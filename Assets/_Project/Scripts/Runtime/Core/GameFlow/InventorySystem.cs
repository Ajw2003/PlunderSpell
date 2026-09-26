using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Core
{
    /// <summary>Fixed-capacity, stack-aware item container. Plain C# so it is testable without a scene.</summary>
    public class InventorySystem
    {
        private readonly List<ItemStack> _items = new List<ItemStack>();

        public int Capacity { get; }
        public IReadOnlyList<ItemStack> Items => _items;
        public bool IsFull => _items.Count >= Capacity;

        public event Action InventoryChanged;

        public InventorySystem(int capacity)
        {
            Capacity = capacity;
        }

        /// <returns>True if every requested unit fit; false if the inventory filled up partway through.</returns>
        public bool AddItem(ItemDefinition definition, int count = 1)
        {
            if (definition == null || count <= 0)
            {
                return false;
            }

            var existing = _items.Find(stack => stack.Definition == definition && stack.Count < definition.MaxStack);
            if (existing != null)
            {
                int space = definition.MaxStack - existing.Count;
                int toAdd = Mathf.Min(space, count);
                existing.Count += toAdd;
                count -= toAdd;
            }

            bool fullyAdded = true;
            while (count > 0)
            {
                if (_items.Count >= Capacity)
                {
                    fullyAdded = false;
                    break;
                }

                int stackAmount = Mathf.Min(count, definition.MaxStack);
                _items.Add(new ItemStack(definition, stackAmount));
                count -= stackAmount;
            }

            InventoryChanged?.Invoke();
            return fullyAdded;
        }

        /// <returns>False (with no change made) if fewer than <paramref name="count"/> units are held.</returns>
        public bool RemoveItem(ItemDefinition definition, int count = 1)
        {
            if (definition == null || count <= 0)
            {
                return false;
            }

            if (GetTotalCount(definition) < count)
            {
                return false;
            }

            int remaining = count;
            for (int i = _items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_items[i].Definition != definition)
                {
                    continue;
                }

                int take = Mathf.Min(_items[i].Count, remaining);
                _items[i].Count -= take;
                remaining -= take;

                if (_items[i].Count <= 0)
                {
                    _items.RemoveAt(i);
                }
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public int GetTotalCount(ItemDefinition definition)
        {
            int total = 0;
            foreach (var stack in _items)
            {
                if (stack.Definition == definition)
                {
                    total += stack.Count;
                }
            }

            return total;
        }

        public void Clear()
        {
            _items.Clear();
            InventoryChanged?.Invoke();
        }
    }
}
