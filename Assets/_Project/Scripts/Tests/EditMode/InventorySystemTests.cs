using NUnit.Framework;
using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.Tests.EditMode
{
    public class InventorySystemTests
    {
        private ItemDefinition _potion;
        private ItemDefinition _sword;

        [SetUp]
        public void SetUp()
        {
            _potion = ScriptableObject.CreateInstance<ItemDefinition>();
            _potion.ItemName = "Potion";
            _potion.MaxStack = 5;

            _sword = ScriptableObject.CreateInstance<ItemDefinition>();
            _sword.ItemName = "Sword";
            _sword.MaxStack = 1;
        }

        [Test]
        public void AddItem_StacksUpToMaxStack()
        {
            var inventory = new InventorySystem(capacity: 10);

            inventory.AddItem(_potion, 3);
            inventory.AddItem(_potion, 4);

            Assert.AreEqual(7, inventory.GetTotalCount(_potion));
            Assert.AreEqual(2, inventory.Items.Count);
        }

        [Test]
        public void AddItem_ReturnsFalse_WhenInventoryIsFull()
        {
            var inventory = new InventorySystem(capacity: 1);
            inventory.AddItem(_sword, 1);

            bool added = inventory.AddItem(_sword, 1);

            Assert.IsFalse(added);
        }

        [Test]
        public void RemoveItem_RemovesAcrossMultipleStacks()
        {
            var inventory = new InventorySystem(capacity: 10);
            inventory.AddItem(_potion, 5);
            inventory.AddItem(_potion, 3);

            bool removed = inventory.RemoveItem(_potion, 6);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, inventory.GetTotalCount(_potion));
        }

        [Test]
        public void RemoveItem_ReturnsFalse_AndLeavesInventoryUnchanged_WhenNotEnoughItems()
        {
            var inventory = new InventorySystem(capacity: 10);
            inventory.AddItem(_potion, 2);

            bool removed = inventory.RemoveItem(_potion, 5);

            Assert.IsFalse(removed);
            Assert.AreEqual(2, inventory.GetTotalCount(_potion));
        }
    }
}
