using Game.InventorySystem;
using Game.Items;
using UnityEngine;

namespace Game.World
{
    public class WorldPickup : MonoBehaviour
    {
        public string ItemId;
        public int Count = 1;

        private Transform _player;
        private float _bobOffset;
        private float _attractRange = 6f;
        private float _collectRange = 1.2f;

        private void Start()
        {
            var controller = FindFirstObjectByType<Player.PlayerController>();
            if (controller != null)
                _player = controller.transform;
            _bobOffset = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (_player == null)
                return;

            float bob = Mathf.Sin(Time.time * 2.5f + _bobOffset) * 0.15f;
            transform.position += Vector3.up * bob * Time.deltaTime;
            transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);

            Vector3 toPlayer = _player.position + Vector3.up - transform.position;
            float distance = toPlayer.magnitude;

            if (distance <= _collectRange)
            {
                Collect();
                return;
            }

            if (distance <= _attractRange)
            {
                transform.position += toPlayer.normalized * 6f * Time.deltaTime;
            }
        }

        private void Collect()
        {
            var inventory = _player.GetComponent<PlayerInventory>();
            if (inventory == null || inventory.Inventory == null)
                return;

            if (inventory.Inventory.Add(ItemId, Count))
                Destroy(gameObject);
        }
    }
}
