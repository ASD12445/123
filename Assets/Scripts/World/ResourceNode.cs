using Game.Items;
using UnityEngine;

namespace Game.World
{
    public class ResourceNode : MonoBehaviour
    {
        [Header("Identity")]
        public string ItemId;
        public string DisplayName;
        public int ChunkX;
        public int ChunkZ;
        public int NodeIndex;

        [Header("Harvest")]
        public ToolClass RequiredTool;
        public int RequiredTier;
        public float MaxHealth = 100f;
        public float Health = 100f;
        public int MinDrop = 1;
        public int MaxDrop = 3;

        private Vector3 _baseScale;
        private float _hitTimer;

        private void Awake()
        {
            _baseScale = transform.localScale;
            Health = MaxHealth;
        }

        public void TakeDamage(ItemDefinition tool, float damageMultiplier, out bool destroyed)
        {
            destroyed = false;
            Health -= 25f * damageMultiplier;
            _hitTimer = 0.12f;

            if (Health <= 0f)
                destroyed = true;
        }

        private void Update()
        {
            if (_hitTimer <= 0f)
                return;

            _hitTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_hitTimer / 0.12f);
            transform.localScale = _baseScale * (1f + t * 0.08f);

            if (_hitTimer <= 0f)
                transform.localScale = _baseScale;
        }

        public int RollDropCount()
        {
            if (MaxDrop <= MinDrop)
                return MinDrop;
            return Random.Range(MinDrop, MaxDrop + 1);
        }
    }
}
