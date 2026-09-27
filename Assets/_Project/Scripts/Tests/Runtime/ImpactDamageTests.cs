using System.Collections;
using System.Collections.Generic;
using Interfaces;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// An item hurts what it hits only by its own motion (#146): walking into a cauldron standing on
    /// the floor is not a hit, but the same cauldron thrown at someone still is.
    /// </summary>
    public class ImpactDamageTests
    {
        private sealed class Body : MonoBehaviour, IHealth
        {
            public float CurrentHealth { get; private set; } = 100f;
            public float MaxHealth => 100f;
            public void TakeDamage(float damage) => CurrentHealth -= damage;
            public void TakeDamage(float damage, float impactVelocity) => CurrentHealth -= damage;
        }

        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made)
            {
                if (go != null)
                    Object.Destroy(go);
            }
            _made.Clear();
        }

        private GameObject Make(PrimitiveType shape, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            go.transform.position = position;
            _made.Add(go);
            return go;
        }

        /// <summary>A player-like body: a capsule on a rigidbody that is not knocked over.</summary>
        private (Rigidbody body, Body health) MakeWalker(Vector3 position)
        {
            GameObject go = Make(PrimitiveType.Capsule, position);
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 70f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            return (body, go.AddComponent<Body>());
        }

        /// <summary>A 12 kg item, as heavy as the tripod cauldron.</summary>
        private Rigidbody MakeCauldron(Vector3 position)
        {
            GameObject go = Make(PrimitiveType.Cube, position);
            var body = go.AddComponent<Rigidbody>();
            body.mass = 12f;
            body.useGravity = false;
            go.AddComponent<Item>();
            return body;
        }

        [UnityTest]
        public IEnumerator Test_WalkingIntoStandingLootDoesNotHurt()
        {
            (Rigidbody walker, Body health) = MakeWalker(new Vector3(0f, 1f, 0f));
            MakeCauldron(new Vector3(0f, 1f, 2f));

            // Walk straight into it at the player's walking speed (RaidPlayer.prefab walkSpeed 5),
            // setting the velocity every step as PlayerWalkState does.
            for (float t = 0f; t < 1f; t += Time.fixedDeltaTime)
            {
                walker.linearVelocity = new Vector3(0f, 0f, 5f);
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(100f, health.CurrentHealth, "Walking into a cauldron on the floor hurt the walker.");
        }

        /// <summary>Fragile loot, as the faience hippopotamus (Fragility 2 m/s), with a box collider.</summary>
        private Plunderspell.Loot.LootPickup MakeFragileLoot(Vector3 position)
        {
            GameObject go = Make(PrimitiveType.Cube, position);
            go.transform.localScale = Vector3.one * 0.4f;
            var pickup = go.AddComponent<Plunderspell.Loot.LootPickup>();
            var data = ScriptableObject.CreateInstance<Plunderspell.Loot.LootItem>();
            data.Worth = 420f;
            data.WeightKg = 0.5f;
            data.Fragility = 2f;
            pickup.SetData(data);
            go.GetComponent<Rigidbody>().mass = 0.5f;
            return pickup;
        }

        [UnityTest]
        public IEnumerator Test_WalkingIntoFragileLootDoesNotBreakIt()
        {
            GameObject floor = Make(PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f));
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            (Rigidbody walker, Body _) = MakeWalker(new Vector3(0f, 1f, 0f));
            Plunderspell.Loot.LootPickup loot = MakeFragileLoot(new Vector3(0f, 0.2f, 2f));

            for (float t = 0f; t < 1f; t += Time.fixedDeltaTime)
            {
                walker.linearVelocity = new Vector3(0f, 0f, 5f);
                yield return new WaitForFixedUpdate();
            }

            Assert.IsFalse(loot.IsBroken, "Walking into fragile loot on the floor shattered it.");
        }

        [UnityTest]
        public IEnumerator Test_FragileLootDroppedFromHighStillBreaks()
        {
            GameObject floor = Make(PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f));
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            Plunderspell.Loot.LootPickup loot = MakeFragileLoot(new Vector3(0f, 3f, 0f));

            for (float t = 0f; t < 1.5f; t += Time.fixedDeltaTime)
                yield return new WaitForFixedUpdate();

            Assert.IsTrue(loot.IsBroken, "Fragile loot dropped from 3 m should shatter.");
        }

        [UnityTest]
        public IEnumerator Test_AThrownCauldronStillHurts()
        {
            (Rigidbody standing, Body health) = MakeWalker(new Vector3(0f, 1f, 0f));
            Rigidbody cauldron = MakeCauldron(new Vector3(0f, 1f, 3f));
            cauldron.linearVelocity = new Vector3(0f, 0f, -8f);

            for (float t = 0f; t < 1f; t += Time.fixedDeltaTime)
                yield return new WaitForFixedUpdate();

            Assert.Less(health.CurrentHealth, 100f, "A cauldron thrown at 8 m/s did no damage.");
        }
    }
}
