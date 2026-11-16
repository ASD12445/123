using Game.InventorySystem;
using Game.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactorCamera;
        [SerializeField] private float range = 3.5f;
        [SerializeField] private float hitInterval = 0.45f;

        public static string HintText { get; private set; } = "";
        public static float HintProgress { get; private set; } = -1f;

        private PlayerInventory _inventory;
        private float _cooldown;
        private World.ResourceNode _currentTarget;

        private void Awake()
        {
            if (interactorCamera == null)
                interactorCamera = Camera.main;

            var player = GetComponentInParent<PlayerController>();
            if (player != null)
                _inventory = player.GetComponent<PlayerInventory>();
            if (_inventory == null)
                _inventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void Update()
        {
            _cooldown -= Time.deltaTime;

            var inventory = _inventory;
            bool uiOpen = inventory != null && inventory.UiOpen;
            bool cursorLocked = Core.GameManager.Instance == null || Core.GameManager.Instance.CursorLocked;

            if (uiOpen || !cursorLocked || interactorCamera == null)
            {
                ClearHint();
                return;
            }

            _currentTarget = null;
            var ray = interactorCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                var node = hit.collider.GetComponentInParent<World.ResourceNode>();
                if (node != null && node.gameObject.activeInHierarchy)
                    _currentTarget = node;
            }

            if (_currentTarget == null)
            {
                ClearHint();
                return;
            }

            var selected = inventory.Inventory.GetSelectedItem();
            bool toolOk = HasRequiredTool(selected, _currentTarget);

            if (!toolOk)
            {
                HintText = $"Нужен {ToolName(_currentTarget.RequiredTool)} тир {_currentTarget.RequiredTier}+";
                HintProgress = -1f;
                return;
            }

            HintText = $"{_currentTarget.DisplayName} — держи ЛКМ";
            HintProgress = _currentTarget.Health / _currentTarget.MaxHealth;

            var mouse = Mouse.current;
            bool attacking = mouse != null && mouse.leftButton.isPressed;
            if (!attacking || _cooldown > 0f)
                return;

            _cooldown = hitInterval / Mathf.Max(0.1f, selected.HarvestSpeedMultiplier);
            _currentTarget.TakeDamage(selected, selected.HarvestSpeedMultiplier, out bool destroyed);

            if (!destroyed)
                return;

            HarvestAndDestroy(_currentTarget, inventory);
            ClearHint();
        }

        private static bool HasRequiredTool(ItemDefinition selected, World.ResourceNode node)
        {
            if (node.RequiredTool == ToolClass.None)
                return true;

            if (selected == null || !selected.IsTool)
                return false;
            if (selected.ToolClass != node.RequiredTool)
                return false;
            if (selected.ToolTier < node.RequiredTier)
                return false;
            return true;
        }

        private static string ToolName(ToolClass toolClass)
        {
            return toolClass switch
            {
                ToolClass.Axe => "топор",
                ToolClass.Pickaxe => "кирка",
                _ => "инструмент"
            };
        }

        private void HarvestAndDestroy(World.ResourceNode node, PlayerInventory inventory)
        {
            int drops = node.RollDropCount();
            var world = FindFirstObjectByType<World.ChunkManager>();

            if (world != null)
            {
                world.OnNodeHarvested(node);
                if (drops > 0)
                {
                    Vector3 dropPos = node.transform.position + Random.insideUnitSphere * 0.5f;
                    world.SpawnPickup(dropPos, node.ItemId, drops);
                }
            }
            else if (drops > 0)
            {
                inventory.Inventory.Add(node.ItemId, drops);
            }

            node.gameObject.SetActive(false);
        }

        private static void ClearHint()
        {
            HintText = "";
            HintProgress = -1f;
        }
    }
}
