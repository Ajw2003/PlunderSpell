using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Loot;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Issue #158: loot put down inside the extraction portal stops moving and cannot break, and
    /// picking it up, or it leaving the portal, frees it. See docs/4-systems/raid.md, "Loot in the portal".
    /// </summary>
    public class PortalRestTests
    {
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
                if (o != null)
                    Object.Destroy(o);
            _made.Clear();
        }

        private Item MakeItem(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = position;
            go.AddComponent<Rigidbody>();
            _made.Add(go);
            return go.AddComponent<Item>();
        }

        private LootPickup MakeLoot(float fragility)
        {
            var go = new GameObject("Loot");
            go.AddComponent<BoxCollider>();
            var pickup = go.AddComponent<LootPickup>();
            var data = ScriptableObject.CreateInstance<LootItem>();
            data.Worth = 50f;
            data.Fragility = fragility;
            data.WeightKg = 2f;
            pickup.SetData(data);
            _made.Add(go);
            _made.Add(data);
            return pickup;
        }

        [UnityTest]
        public IEnumerator Test_LootRestingInThePortalIsHeldStill()
        {
            Item item = MakeItem(new Vector3(0f, 50f, 0f));
            var body = item.GetComponent<Rigidbody>();
            body.useGravity = false;
            item.SetInPortal(true);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(body.isKinematic, "A settled piece in the portal must be held still.");
            Assert.IsTrue(item.IsFrozenByPortal);

            item.SetInPortal(false);
            Assert.IsFalse(body.isKinematic, "Out of the portal, it is a free body again.");
        }

        [UnityTest]
        public IEnumerator Test_AFallingPieceLandsBeforeItIsHeld()
        {
            Item item = MakeItem(new Vector3(0f, 60f, 0f));
            var body = item.GetComponent<Rigidbody>();
            body.useGravity = false;
            body.linearVelocity = Vector3.down * 3f;
            item.SetInPortal(true);
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(body.isKinematic, "A piece still moving must not freeze in mid-air.");
        }

        [UnityTest]
        public IEnumerator Test_PickingUpAHeldPieceFreesIt()
        {
            Item item = MakeItem(new Vector3(0f, 70f, 0f));
            var body = item.GetComponent<Rigidbody>();
            body.useGravity = false;
            item.SetInPortal(true);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assume.That(body.isKinematic);

            item.StartDragging(null, item.transform.position);
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(body.isKinematic, "A piece being carried must move with the beam.");
            Assert.IsFalse(item.IsFrozenByPortal);
        }

        [Test]
        public void Test_NothingBreaksLootInThePortal()
        {
            LootPickup pickup = MakeLoot(fragility: 2f);
            Assume.That(pickup.WouldBreak(10f), "Test premise: a 10 m/s knock breaks it outside.");

            pickup.SetInPortal(true);
            Assert.IsFalse(pickup.WouldBreak(10f), "A fall or a knock must not break it in the portal.");
            pickup.Break();
            Assert.IsFalse(pickup.IsBroken, "A spell must not break it in the portal either.");

            pickup.SetInPortal(false);
            Assert.IsTrue(pickup.WouldBreak(10f), "Out of the portal it is fragile again.");
        }
    }
}
