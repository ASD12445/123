using UnityEngine;

namespace Game.World
{
    public static class WorldGenerator
    {
        public const int ChunkSize = 64;
        public const int Resolution = 32;
        public const float WaterLevel = -2f;

        public static int Seed { get; set; } = 1337;

        public static float GetHeight(float x, float z)
        {
            float s = Seed * 0.0173f;
            float hills = Mathf.PerlinNoise(1000f + x * 0.004f + s, 1000f + z * 0.004f + s);
            float detail = Mathf.PerlinNoise(5000f + x * 0.02f + s, 5000f + z * 0.02f + s);
            float mountains = Mathf.PerlinNoise(9000f + x * 0.0012f + s, 9000f + z * 0.0012f + s);
            float ridge = Mathf.Pow(Mathf.PerlinNoise(9000f + x * 0.0012f + s, 9000f + z * 0.0012f + s), 3f);
            return hills * 16f + detail * 3.5f + mountains * 10f + ridge * 14f - 10f;
        }

        public static float GetForestNoise(float x, float z)
        {
            float s = Seed * 0.0311f;
            return Mathf.PerlinNoise(20000f + x * 0.006f + s, 20000f + z * 0.006f + s);
        }

        public static float GetRockNoise(float x, float z)
        {
            float s = Seed * 0.0437f;
            return Mathf.PerlinNoise(30000f + x * 0.005f + s, 30000f + z * 0.005f + s);
        }

        public static int GetChunkSeed(int chunkX, int chunkZ)
        {
            unchecked
            {
                int h = Seed;
                h = h * 31 + chunkX;
                h = h * 31 + chunkZ;
                return h;
            }
        }

        public static int WorldToChunkCoord(float worldCoord)
        {
            return Mathf.FloorToInt(worldCoord / ChunkSize);
        }

        public static Vector2Int WorldToChunk(Vector3 worldPos)
        {
            return new Vector2Int(WorldToChunkCoord(worldPos.x), WorldToChunkCoord(worldPos.z));
        }
    }
}
