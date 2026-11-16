using Game.Player;
using Game.World;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    public static class SceneSetupStage3
    {
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("Game/Setup Stage3 World")]
        public static void Setup()
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Player not found. Run 'Game/Setup Stage1 Scene' first.");
                return;
            }

            EnsureFolders();
            var materials = LoadOrCreateMaterials();

            var oldGround = GameObject.Find("Ground");
            if (oldGround != null)
                Object.DestroyImmediate(oldGround);

            var systems = GameObject.Find("GameSystems");
            if (systems == null)
                systems = new GameObject("GameSystems");

            var chunkManager = systems.GetComponent<ChunkManager>();
            if (chunkManager == null)
                chunkManager = systems.AddComponent<ChunkManager>();

            chunkManager.SetSeed(1337);
            chunkManager.SetLoadRadius(3);
            chunkManager.SetPlayer(player.transform);
            chunkManager.SetMaterials(materials.grass, materials.rock, materials.trunk, materials.leaves, materials.ore);
            EditorUtility.SetDirty(chunkManager);

            var camera = player.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                var interactor = camera.GetComponent<PlayerInteractor>();
                if (interactor == null)
                    interactor = camera.gameObject.AddComponent<PlayerInteractor>();
                EditorUtility.SetDirty(interactor);
            }

            float groundHeight = WorldGenerator.GetHeight(2f, 2f);
            player.transform.position = new Vector3(2f, groundHeight + 2f, 2f);

            EditorSceneManagerMarkDirty();
            Debug.Log("Stage3: world streaming + resources wired. Press Play.");
        }

        private static void EditorSceneManagerMarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets", "Materials");
        }

        private struct MaterialSet
        {
            public Material grass;
            public Material rock;
            public Material trunk;
            public Material leaves;
            public Material ore;
        }

        private static MaterialSet LoadOrCreateMaterials()
        {
            return new MaterialSet
            {
                grass = LoadMat("TerrainGrass", new Color(0.25f, 0.42f, 0.18f)),
                rock = LoadMat("TerrainRock", new Color(0.44f, 0.42f, 0.4f)),
                trunk = LoadMat("NodeTrunk", new Color(0.36f, 0.23f, 0.11f)),
                leaves = LoadMat("NodeLeaves", new Color(0.14f, 0.34f, 0.12f)),
                ore = LoadMat("NodeOreCrystal", new Color(0.8f, 0.8f, 0.85f))
            };
        }

        private static Material LoadMat(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                ApplyColor(existing, color);
                return existing;
            }

            var shader = Shader.Find("HDRP/Lit");
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
            ApplyColor(material, color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
        }
    }
}
