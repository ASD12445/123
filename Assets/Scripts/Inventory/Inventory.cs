using System;
using System.Collections.Generic;
using Game.Items;
using UnityEngine;

namespace Game.InventorySystem
{
    [Serializable]
    public class ItemStack
    {
        public string ItemId;
        public int Count;

        public ItemStack(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

        public ItemDefinition Item => ItemRegistry.Get(ItemId);

        public void Clear()
        {
            ItemId = null;
            Count = 0;
        }
    }

    public class Inventory
    {
        public const int HotbarSize = 8;
        public const int MainSize = 24;
        public const int TotalSize = HotbarSize + MainSize;

        private readonly ItemStack[] _slots;

        public event Action Changed;
        public event Action<int> SlotChanged;
        public event Action<int> HotbarSelectionChanged;

        public int SelectedHotbarIndex { get; private set; }

        public Inventory()
        {
            _slots = new ItemStack[TotalSize];
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = new ItemStack(null, 0);
        }

        public ItemStack GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length)
                return null;
            return _slots[index];
        }

        public ItemStack GetSelectedHotbarStack() => GetSlot(SelectedHotbarIndex);

        public ItemDefinition GetSelectedItem() => GetSelectedHotbarStack()?.Item;

        public void SetSelectedHotbarIndex(int index)
        {
            index = Mathf.Clamp(index, 0, HotbarSize - 1);
            if (SelectedHotbarIndex == index)
                return;
            SelectedHotbarIndex = index;
            HotbarSelectionChanged?.Invoke(index);
        }

        public void ScrollHotbar(int direction)
        {
            int next = SelectedHotbarIndex + (direction > 0 ? 1 : direction < 0 ? -1 : 0);
            if (next < 0)
                next = HotbarSize - 1;
            else if (next >= HotbarSize)
                next = 0;
            SetSelectedHotbarIndex(next);
        }

        public int CountOf(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;
            int total = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].ItemId == itemId)
                    total += _slots[i].Count;
            }
            return total;
        }

        public bool Add(string itemId, int amount)
        {
            var item = ItemRegistry.Get(itemId);
            if (item == null || amount <= 0)
                return false;

            int remaining = amount;
            int maxStack = item.MaxStack;

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (slot.IsEmpty || slot.ItemId != itemId || slot.Count >= maxStack)
                    continue;

                int space = maxStack - slot.Count;
                int put = Mathf.Min(space, remaining);
                slot.Count += put;
                remaining -= put;
                NotifySlot(i);
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty)
                    continue;

                int put = Mathf.Min(maxStack, remaining);
                slot.ItemId = itemId;
                slot.Count = put;
                remaining -= put;
                NotifySlot(i);
            }

            if (remaining != amount)
                Changed?.Invoke();

            return remaining == 0;
        }

        public int Remove(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return 0;

            int toRemove = amount;
            for (int i = _slots.Length - 1; i >= 0 && toRemove > 0; i--)
            {
                var slot = _slots[i];
                if (slot.ItemId != itemId)
                    continue;

                int take = Mathf.Min(slot.Count, toRemove);
                slot.Count -= take;
                toRemove -= take;
                if (slot.Count <= 0)
                    slot.Clear();
                NotifySlot(i);
            }

            int removed = amount - toRemove;
            if (removed > 0)
                Changed?.Invoke();
            return removed;
        }

        public bool RemoveFromSlot(int index, int amount)
        {
            var slot = GetSlot(index);
            if (slot == null || slot.IsEmpty || amount <= 0)
                return false;

            slot.Count -= amount;
            if (slot.Count <= 0)
                slot.Clear();
            NotifySlot(index);
            Changed?.Invoke();
            return true;
        }

        public bool UseSelectedTool()
        {
            var slot = GetSelectedHotbarStack();
            if (slot == null || slot.IsEmpty)
                return false;

            var item = slot.Item;
            if (item == null || !item.IsTool)
                return false;

            return RemoveFromSlot(SelectedHotbarIndex, 1);
        }

        public bool SwapSlots(int a, int b)
        {
            if (a < 0 || b < 0 || a >= _slots.Length || b >= _slots.Length || a == b)
                return false;

            (_slots[a].ItemId, _slots[b].ItemId) = (_slots[b].ItemId, _slots[a].ItemId);
            (_slots[a].Count, _slots[b].Count) = (_slots[b].Count, _slots[a].Count);
            NotifySlot(a);
            NotifySlot(b);
            Changed?.Invoke();
            return true;
        }

        public List<ItemStack> GetAllStacks()
        {
            var result = new List<ItemStack>();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                    result.Add(_slots[i]);
            }
            return result;
        }

        private void NotifySlot(int index) => SlotChanged?.Invoke(index);
    }
}
