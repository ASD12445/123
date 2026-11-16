using System;
using Game.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.InventorySystem
{
    public class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        public struct StartingStack
        {
            public string ItemId;
            public int Count;
        }

        [SerializeField] private StartingStack[] startingItems;

        public Inventory Inventory { get; private set; }

        public event Action ItemUsed;
        public event Action<int> FoodEaten;

        private InputActionMap _map;
        private InputAction _toggleInventoryAction;
        private InputAction _hotbarScrollAction;
        private InputAction _useAction;
        private InputAction[] _hotbarSelectActions;

        public bool UiOpen { get; private set; }

        public event Action<bool> UiOpenChanged;

        private void Awake()
        {
            ItemRegistry.Initialize();
            Inventory = new Inventory();

            if (startingItems != null)
            {
                foreach (var stack in startingItems)
                {
                    if (!string.IsNullOrEmpty(stack.ItemId) && stack.Count > 0)
                        Inventory.Add(stack.ItemId, stack.Count);
                }
            }

            BuildInput();
        }

        private void OnEnable() => _map?.Enable();

        private void OnDisable() => _map?.Disable();

        private void OnDestroy()
        {
            if (_map == null)
                return;

            _map.Disable();
            foreach (var action in _map.actions)
                action.Dispose();
            _map.Dispose();
        }

        private void Update()
        {
            HandleHotbarHotkeys();
        }

        public void ToggleUi()
        {
            SetUiOpen(!UiOpen);
        }

        public void SetUiOpen(bool open)
        {
            if (UiOpen == open)
                return;

            UiOpen = open;
            var gameManager = Core.GameManager.Instance;
            if (gameManager != null)
                gameManager.SetCursorLocked(!open);

            UiOpenChanged?.Invoke(open);
        }

        public bool TryUseSelectedItem()
        {
            var slot = Inventory.GetSelectedHotbarStack();
            if (slot == null || slot.IsEmpty)
                return false;

            var item = slot.Item;
            if (item == null)
                return false;

            if (item.Type == ItemType.Food)
            {
                Inventory.RemoveFromSlot(Inventory.SelectedHotbarIndex, 1);
                FoodEaten?.Invoke(item.FoodHealAmount);
                ItemUsed?.Invoke();
                return true;
            }

            if (item.IsTool)
            {
                ItemUsed?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryConsumeTool(ToolClass toolClass, int toolTier)
        {
            var item = Inventory.GetSelectedItem();
            if (item == null || !item.IsTool)
                return false;
            if (item.ToolClass != toolClass || item.ToolTier < toolTier)
                return false;

            Inventory.RemoveFromSlot(Inventory.SelectedHotbarIndex, 1);
            return true;
        }

        public ItemDefinition GetTool(ToolClass toolClass, int requiredTier)
        {
            var item = Inventory.GetSelectedItem();
            if (item != null && item.IsTool && item.ToolClass == toolClass && item.ToolTier >= requiredTier)
                return item;
            return null;
        }

        private void HandleHotbarHotkeys()
        {
            bool cursorLocked = Core.GameManager.Instance == null || Core.GameManager.Instance.CursorLocked;
            if (!cursorLocked || UiOpen)
                return;

            for (int i = 0; i < _hotbarSelectActions.Length; i++)
            {
                if (_hotbarSelectActions[i].WasPressedThisFrame())
                {
                    Inventory.SetSelectedHotbarIndex(i);
                    return;
                }
            }

            if (_hotbarScrollAction.WasPerformedThisFrame())
            {
                Vector2 scroll = _hotbarScrollAction.ReadValue<Vector2>();
                if (Mathf.Abs(scroll.y) > 0.01f)
                    Inventory.ScrollHotbar(scroll.y > 0f ? -1 : 1);
            }

            if (_useAction.WasPressedThisFrame())
                TryUseSelectedItem();
        }

        private void BuildInput()
        {
            _map = new InputActionMap("Inventory");

            _toggleInventoryAction = _map.AddAction("ToggleInventory", InputActionType.Button);
            _toggleInventoryAction.AddBinding("<Keyboard>/tab");
            _toggleInventoryAction.AddBinding("<Keyboard>/i");
            _toggleInventoryAction.performed += _ => ToggleUi();

            _hotbarScrollAction = _map.AddAction("HotbarScroll", InputActionType.Value);
            _hotbarScrollAction.AddBinding("<Mouse>/scroll");

            _useAction = _map.AddAction("UseItem", InputActionType.Button);
            _useAction.AddBinding("<Keyboard>/f");
            _useAction.AddBinding("<Gamepad>/buttonWest");

            _hotbarSelectActions = new InputAction[Inventory.HotbarSize];
            for (int i = 0; i < _hotbarSelectActions.Length; i++)
            {
                var action = _map.AddAction($"HotbarSelect{i + 1}", InputActionType.Button);
                action.AddBinding($"<Keyboard>/{i + 1}");
                _hotbarSelectActions[i] = action;
            }
        }
    }
}
