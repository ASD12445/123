using System.Collections.Generic;
using Game.Items;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.World
{
    public class ChunkManager : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] private int seed = 1337;
        [SerializeField] private int loadRadius = 3;

        [Header("Materials")]
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material rockMaterial;
        [SerializeField] private Material trunkMaterial;
        [SerializeField] private Material leavesMaterial;
        [SerializeField] private Material oreCrystalMaterial;

        [Header("Refs")]
        [SerializeField] private Transform player;

        [Header("Respawn")]
        [SerializeField] private float nodeRespawnSeconds = 180f;

        private readonly Dictionary<Vector2Int, ChunkData> _chunks = new Dictionary<Vector2Int, ChunkData>();
        private readonly Dictionary<int, Dictionary<int, float>> _harvested = new Dictionary<int, Dictionary<int, float>>();
        private readonly Queue<Vector2Int> _buildQueue = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> _queued = new HashSet<Vector2Int>();
        private Vector2Int _lastPlayerChunk;
        private bool _playerChunkInitialized;
        private Material[] _terrainMaterials;

        private class ChunkData
        {
            public GameObject Root;
            public readonly List<ResourceNode> Nodes = new List<ResourceNode>();
        }

        private struct NodeSpec
        {
            public int Index;
            public Vector3 Position;
            public float Scale;
            public float Rotation;
        }

        public void SetPlayer(Transform target) => player = target;

        public void SetMaterials(Material grass, Material rock, Material trunk, Material leaves, Material ore)
        {
            grassMaterial = grass;
            rockMaterial = rock;
            trunkMaterial = trunk;
            leavesMaterial = leaves;
            oreCrystalMaterial = ore;
            _terrainMaterials = null;
        }

        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            WorldGenerator.Seed = newSeed;
        }

        public void SetLoadRadius(int radius) => loadRadius = Mathf.Clamp(radius, 1, 8);

        private void Awake()
        {
            WorldGenerator.Seed = seed;
            if (player == null)
            {
                var controller = FindFirstObjectByType<Player.PlayerController>();
                if (controller != null)
                    player = controller.transform;
            }
        }

        private void Update()
        {
            if (player == null)
                return;

            Vector2Int playerChunk = WorldGenerator.WorldToChunk(player.position);
            if (!_playerChunkInitialized || playerChunk != _lastPlayerChunk)
            {
                _playerChunkInitialized = true;
                _lastPlayerChunk = playerChunk;
                RefreshQueue(playerChunk);
            }

            int built = 0;
            while (_buildQueue.Count > 0 && built < 1)
            {
                Vector2Int coord = _buildQueue.Dequeue();
                _queued.Remove(coord);
                if (_chunks.ContainsKey(coord))
                    continue;
                BuildChunk(coord);
                built++;
            }

            UnloadFarChunks(playerChunk);
        }

        private void RefreshQueue(Vector2Int center)
        {
            _buildQueue.Clear();
            _queued.Clear();

            var wanted = new List<Vector2Int>();
            for (int dz = -loadRadius; dz <= loadRadius; dz++)
            {
                for (int dx = -loadRadius; dx <= loadRadius; dx++)
                {
                    Vector2Int coord = new Vector2Int(center.x + dx, center.y + dz);
                    if (_chunks.ContainsKey(coord))
                        continue;
                    wanted.Add(coord);
                }
            }

            wanted.Sort((a, b) =>
            {
                float da = (a - center).sqrMagnitude;
                float db = (b - center).sqrMagnitude;
                return da.CompareTo(db);
            });

            foreach (var coord in wanted)
            {
                if (_queued.Add(coord))
                    _buildQueue.Enqueue(coord);
            }
        }

        private void UnloadFarChunks(Vector2Int center)
        {
            List<Vector2Int> toRemove = null;
            foreach (var pair in _chunks)
            {
                int dist = Mathf.Max(Mathf.Abs(pair.Key.x - center.x), Mathf.Abs(pair.Key.y - center.y));
                if (dist > loadRadius + 1)
                {
                    toRemove ??= new List<Vector2Int>();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove == null)
                return;

            foreach (var coord in toRemove)
            {
                var data = _chunks[coord];
                if (data.Root != null)
                    Destroy(data.Root);
                _chunks.Remove(coord);
            }
        }

        public void OnNodeHarvested(ResourceNode node)
        {
            int chunkKey = node.ChunkX * 73856093 ^ node.ChunkZ * 19349663;
            if (!_harvested.TryGetValue(chunkKey, out var map))
            {
                map = new Dictionary<int, float>();
                _harvested[chunkKey] = map;
            }
            map[node.NodeIndex] = Time.time;
        }

        private bool IsHarvested(int chunkKey, int nodeIndex, out bool respawned)
        {
            respawned = false;
            if (!_harvested.TryGetValue(chunkKey, out var map))
                return false;
            if (!map.TryGetValue(nodeIndex, out float time))
                return false;

            if (Time.time - time >= nodeRespawnSeconds)
            {
                map.Remove(nodeIndex);
                respawned = true;
                return false;
            }
            return true;
        }

        private void BuildChunk(Vector2Int coord)
        {
            if (grassMaterial == null || rockMaterial == null)
                return;

            var root = new GameObject($"Chunk_{coord.x}_{coord.y}");
            root.transform.SetParent(transform, false);

            var data = new ChunkData { Root = root };
            _chunks[coord] = data;

            BuildTerrain(root, coord);
            SpawnNodes(root, coord, data);
        }

        private void BuildTerrain(GameObject root, Vector2Int coord)
        {
            int res = WorldGenerator.Resolution;
            int vertsPerSide = res + 1;
            float size = WorldGenerator.ChunkSize;
            float step = size / res;
            float originX = coord.x * size;
            float originZ = coord.y * size;

            var vertices = new Vector3[vertsPerSide * vertsPerSide];
            var uvs = new Vector2[vertices.Length];
            var heights = new float[vertices.Length];

            for (int z = 0; z < vertsPerSide; z++)
            {
                for (int x = 0; x < vertsPerSide; x++)
                {
                    int i = z * vertsPerSide + x;
                    float wx = originX + x * step;
                    float wz = originZ + z * step;
                    float h = WorldGenerator.GetHeight(wx, wz);
                    vertices[i] = new Vector3(wx - originX, h, wz - originZ);
                    heights[i] = h;
                    uvs[i] = new Vector2(wx / 16f, wz / 16f);
                }
            }

            var trisFlat = new List<int>(res * res * 6);
            var trisSteep = new List<int>(res * res * 6);

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i0 = z * vertsPerSide + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + vertsPerSide;
                    int i3 = i2 + 1;

                    float slopeA = Mathf.Abs(heights[i0] - heights[i3]);
                    float slopeB = Mathf.Abs(heights[i1] - heights[i2]);
                    float slope = Mathf.Max(slopeA, slopeB) / step;
                    bool steep = slope > 0.55f || Mathf.Max(heights[i0], heights[i3]) > 26f;

                    var target = steep ? trisSteep : trisFlat;
                    target.Add(i0); target.Add(i2); target.Add(i1);
                    target.Add(i1); target.Add(i2); target.Add(i3);
                }
            }

            var mesh = new Mesh { name = $"ChunkMesh_{coord.x}_{coord.y}" };
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisFlat, 0);
            mesh.SetTriangles(trisSteep, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            _terrainMaterials ??= new Material[] { grassMaterial, rockMaterial };
            renderer.sharedMaterials = _terrainMaterials;

            var collider = root.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        private void SpawnNodes(GameObject root, Vector2Int coord, ChunkData data)
        {
            int chunkSeed = WorldGenerator.GetChunkSeed(coord.x, coord.y);
            var rng = new System.Random(chunkSeed);
            int chunkKey = coord.x * 73856093 ^ coord.y * 19349663;

            float originX = coord.x * WorldGenerator.ChunkSize;
            float originZ = coord.y * WorldGenerator.ChunkSize;

            int attempts = 34;
            int nodeIndex = 0;

            for (int a = 0; a < attempts; a++)
            {
                float px = (float)rng.NextDouble() * WorldGenerator.ChunkSize;
                float pz = (float)rng.NextDouble() * WorldGenerator.ChunkSize;
                float roll = (float)rng.NextDouble();
                float scale = 0.8f + (float)rng.NextDouble() * 0.6f;
                float rotation = (float)rng.NextDouble() * 360f;

                float wx = originX + px;
                float wz = originZ + pz;
                float h = WorldGenerator.GetHeight(wx, wz);
                nodeIndex++;

                if (h < WorldGenerator.WaterLevel + 1f)
                    continue;

                bool harvested = IsHarvested(chunkKey, nodeIndex, out _);
                if (harvested)
                    continue;

                float forest = WorldGenerator.GetForestNoise(wx, wz);
                float rockNoise = WorldGenerator.GetRockNoise(wx, wz);
                var pos = new Vector3(wx, h, wz);

                string oreId = DecideOre(h, forest, rockNoise, roll, out _);

                if (oreId != null)
                {
                    SpawnOreNode(root, data, coord, nodeIndex, pos, scale, rotation, oreId);
                }
                else if (forest > 0.52f && roll < 0.55f)
                {
                    SpawnTree(root, data, coord, nodeIndex, pos, scale, rotation);
                }
                else if (rockNoise > 0.5f && roll < 0.5f)
                {
                    SpawnRock(root, data, coord, nodeIndex, pos, scale, rotation, "stone", 1);
                }
                else if (roll > 0.96f)
                {
                    SpawnBush(root, data, coord, nodeIndex, pos, rotation);
                }
            }
        }

        private string DecideOre(float height, float forest, float rockNoise, float roll, out float oreRoll)
        {
            oreRoll = roll;
            if (rockNoise < 0.45f && height < 20f)
                return null;
            if (height < 12f)
                return roll > 0.93f ? "copper" : null;
            if (height < 18f)
                return roll > 0.9f ? "coal" : roll > 0.85f ? "iron" : null;
            if (height < 25f)
                return roll > 0.88f ? "iron" : roll > 0.8f ? "silver" : roll > 0.74f ? "coal" : null;
            if (height < 32f)
                return roll > 0.85f ? "silver" : roll > 0.78f ? "gold" : roll > 0.7f ? "obsidian" : null;
            return roll > 0.82f ? "obsidian" : roll > 0.72f ? "diamond" : roll > 0.62f ? "emerald" : null;
        }

        private ResourceNode CreateNodeBase(GameObject root, ChunkData data, Vector2Int coord, int index,
            Vector3 pos, string itemId, string displayName, ToolClass tool, int tier, float health,
            int minDrop, int maxDrop)
        {
            var go = new GameObject($"Node_{itemId}_{index}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;

            var node = go.AddComponent<ResourceNode>();
            node.ItemId = itemId;
            node.DisplayName = displayName;
            node.ChunkX = coord.x;
            node.ChunkZ = coord.y;
            node.NodeIndex = index;
            node.RequiredTool = tool;
            node.RequiredTier = tier;
            node.MaxHealth = health;
            node.Health = health;
            node.MinDrop = minDrop;
            node.MaxDrop = maxDrop;

            data.Nodes.Add(node);
            return node;
        }

        private void SpawnTree(GameObject root, ChunkData data, Vector2Int coord, int index,
            Vector3 pos, float scale, float rotation)
        {
            var node = CreateNodeBase(root, data, coord, index, pos, "wood", "Дерево",
                ToolClass.Axe, 1, 100f, 3, 6);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(node.transform, false);
            trunk.transform.localScale = new Vector3(0.45f, 2.4f, 0.45f) * scale;
            trunk.transform.localPosition = new Vector3(0f, 2.4f * scale, 0f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

            int crowns = 2 + (index % 2);
            for (int c = 0; c < crowns; c++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "Crown";
                leaf.transform.SetParent(node.transform, false);
                float y = (4.4f + c * 1.1f) * scale;
                float radius = (2.1f - c * 0.35f) * scale;
                leaf.transform.localScale = Vector3.one * radius * 2f;
                leaf.transform.localPosition = new Vector3(Random.Range(-0.3f, 0.3f), y, Random.Range(-0.3f, 0.3f));
                leaf.GetComponent<Renderer>().sharedMaterial = leavesMaterial;
            }

            node.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            node.transform.localScale = Vector3.one;
        }

        private void SpawnRock(GameObject root, ChunkData data, Vector2Int coord, int index,
            Vector3 pos, float scale, float rotation, string itemId, int tier)
        {
            var node = CreateNodeBase(root, data, coord, index, pos, itemId, "Камень",
                ToolClass.Pickaxe, tier, 80f, 2, 4);

            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.SetParent(node.transform, false);
            rock.transform.localScale = new Vector3(1.7f, 1.1f, 1.5f) * scale;
            rock.transform.localPosition = new Vector3(0f, 0.45f * scale, 0f);
            rock.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), rotation, Random.Range(-15f, 15f));
            rock.GetComponent<Renderer>().sharedMaterial = rockMaterial;

            node.transform.localScale = Vector3.one;
        }

        private void SpawnOreNode(GameObject root, ChunkData data, Vector2Int coord, int index,
            Vector3 pos, float scale, float rotation, string oreId)
        {
            (string name, ToolClass tool, int tier, float health, int minD, int maxD, Color color) = oreId switch
            {
                "copper" => ("Медь", ToolClass.Pickaxe, 1, 90f, 2, 4, new Color(0.75f, 0.45f, 0.25f)),
                "coal" => ("Уголь", ToolClass.Pickaxe, 1, 80f, 2, 4, new Color(0.12f, 0.12f, 0.13f)),
                "iron" => ("Железо", ToolClass.Pickaxe, 2, 110f, 2, 4, new Color(0.72f, 0.72f, 0.75f)),
                "silver" => ("Серебро", ToolClass.Pickaxe, 2, 120f, 1, 3, new Color(0.85f, 0.88f, 0.92f)),
                "gold" => ("Золото", ToolClass.Pickaxe, 3, 130f, 1, 3, new Color(0.95f, 0.78f, 0.2f)),
                "obsidian" => ("Обсидиан", ToolClass.Pickaxe, 3, 160f, 1, 3, new Color(0.18f, 0.1f, 0.28f)),
                "diamond" => ("Алмаз", ToolClass.Pickaxe, 4, 180f, 1, 2, new Color(0.55f, 0.9f, 0.95f)),
                "emerald" => ("Изумруд", ToolClass.Pickaxe, 4, 180f, 1, 2, new Color(0.2f, 0.8f, 0.4f)),
                _ => ("Руда", ToolClass.Pickaxe, 1, 90f, 1, 2, Color.magenta)
            };

            var node = CreateNodeBase(root, data, coord, index, pos, oreId, name, tool, tier, health, minD, maxD);

            var baseRock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            baseRock.name = "OreBase";
            baseRock.transform.SetParent(node.transform, false);
            baseRock.transform.localScale = new Vector3(1.6f, 1.2f, 1.5f) * scale;
            baseRock.transform.localPosition = new Vector3(0f, 0.5f * scale, 0f);
            baseRock.GetComponent<Renderer>().sharedMaterial = rockMaterial;

            var crystalMat = oreCrystalMaterial != null ? oreCrystalMaterial : rockMaterial;
            int crystals = 2 + (index % 3);
            for (int c = 0; c < crystals; c++)
            {
                var crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crystal.name = "Crystal";
                crystal.transform.SetParent(node.transform, false);
                float crScale = 0.35f * scale + Random.Range(0f, 0.2f) * scale;
                crystal.transform.localScale = Vector3.one * crScale;
                crystal.transform.localPosition = new Vector3(
                    Random.Range(-0.6f, 0.6f) * scale,
                    (0.7f + Random.Range(0f, 0.6f)) * scale,
                    Random.Range(-0.6f, 0.6f) * scale);
                var renderer = crystal.GetComponent<Renderer>();
                renderer.sharedMaterial = crystalMat;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
            }

            node.transform.localScale = Vector3.one;
        }

        private void SpawnBush(GameObject root, ChunkData data, Vector2Int coord, int index,
            Vector3 pos, float rotation)
        {
            var node = CreateNodeBase(root, data, coord, index, pos, "berries", "Ягодный куст",
                ToolClass.None, 0, 40f, 1, 2);

            var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush";
            bush.transform.SetParent(node.transform, false);
            bush.transform.localScale = new Vector3(1.2f, 0.9f, 1.2f);
            bush.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            bush.GetComponent<Renderer>().sharedMaterial = leavesMaterial;

            for (int b = 0; b < 4; b++)
            {
                var berry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                berry.name = "Berry";
                berry.transform.SetParent(node.transform, false);
                berry.transform.localScale = Vector3.one * 0.18f;
                berry.transform.localPosition = new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0.4f, 0.85f), Random.Range(-0.45f, 0.45f));
                var renderer = berry.GetComponent<Renderer>();
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(0.85f, 0.15f, 0.3f));
                renderer.SetPropertyBlock(block);
            }

            node.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        public void SpawnPickup(Vector3 position, string itemId, int count)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Pickup_{itemId}";
            var item = ItemRegistry.Get(itemId);
            go.transform.position = position + Vector3.up * 0.6f;
            go.transform.localScale = Vector3.one * 0.35f;

            var renderer = go.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", item != null ? item.IconColor : Color.white);
            renderer.SetPropertyBlock(block);

            var pickup = go.AddComponent<WorldPickup>();
            pickup.ItemId = itemId;
            pickup.Count = count;
        }
    }
}
