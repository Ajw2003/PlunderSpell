using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Plunderspell.Spells;
using Plunderspell.Voice;
using StateMachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Velox and Saltus move a real player body: the dash, the launch, the slam and both misfires,
    /// on a floor, through the same calls a cast makes on the caster's machine
    /// (docs/4-systems/spells.md, "Velox and Saltus").
    /// </summary>
    public class MovementSpellTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            SpellEffectRegistry.Reset();
            foreach (Object o in _spawned)
                if (o != null)
                    Object.Destroy(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        /// <summary>A floor, and a player standing on it with a camera looking along +Z.</summary>
        private PlayerStateMachine MakePlayerOnFloor(Vector3 at)
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = at + new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);

            var go = Track(new GameObject("Player"));
            go.transform.position = at + Vector3.up * 1f;
            go.AddComponent<CapsuleCollider>();
            go.AddComponent<Rigidbody>();
            var player = go.AddComponent<PlayerStateMachine>();
            var eye = new GameObject("Eye");
            eye.transform.SetParent(go.transform, false);
            eye.transform.localPosition = Vector3.up * 0.6f;
            player.CameraTransform = eye.transform;
            player.WalkSpeed = 5f;
            player.JumpForce = 25f;
            player.DodgeForce = 5f;
            typeof(PlayerStateMachine).GetField("_groundLayer", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(player, (LayerMask)~0);
            go.AddComponent<SpellCastingSystem>();
            return player;
        }

        private static SpellEffectContext Context(SpellId spell, PlayerStateMachine player) =>
            new SpellEffectContext(spell, CastVolume.Normal, player.transform.position, Vector3.forward,
                player.GetComponent<SpellCastingSystem>());

        private static ICasterMovementSpell Mover(SpellId id) => (ICasterMovementSpell)SpellEffectRegistry.Find(id);

        private static IEnumerator Settle(PlayerStateMachine player)
        {
            for (int i = 0; i < 20 && !player.IsGrounded; i++)
                yield return new WaitForFixedUpdate();
            Assert.IsTrue(player.IsGrounded, "The test player never stood on the floor.");
        }

        [UnityTest]
        public IEnumerator Test_VeloxDashesTheCasterTheWayTheyAreSteering()
        {
            PlayerStateMachine player = MakePlayerOnFloor(new Vector3(0f, 0f, 400f));
            yield return Settle(player);

            player.MovementDirection = new Vector2(1f, 0f);
            Vector3 before = player.transform.position;
            ICasterMovementSpell velox = Mover(SpellId.Velox);
            Assert.IsTrue(velox.CanMove(Context(SpellId.Velox, player)));
            velox.MoveCaster(Context(SpellId.Velox, player));

            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            // 10 steps is 0.2 s: walking covers 1 m of it, the dash nearly 3.
            Vector3 moved = player.transform.position - before;
            Assert.Greater(moved.x, 2f, $"Velox steered right must dash right, faster than walking; moved {moved}.");
            Assert.Less(Mathf.Abs(moved.z), Mathf.Abs(moved.x), "…and mostly that way.");
        }

        [UnityTest]
        public IEnumerator Test_VeloxWithNoSteeringDashesWhereYouLook()
        {
            PlayerStateMachine player = MakePlayerOnFloor(new Vector3(0f, 0f, 500f));
            yield return Settle(player);

            Vector3 before = player.transform.position;
            Mover(SpellId.Velox).MoveCaster(Context(SpellId.Velox, player));
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Assert.Greater(player.transform.position.z - before.z, 2f,
                "A dash with no steering goes the way the camera looks, never nowhere.");
        }

        [UnityTest]
        public IEnumerator Test_SaltusLaunchesAGroundedCasterAndCannotBeCastInTheAir()
        {
            PlayerStateMachine player = MakePlayerOnFloor(new Vector3(0f, 0f, 600f));
            yield return Settle(player);

            float startY = player.transform.position.y;
            ICasterMovementSpell saltus = Mover(SpellId.Saltus);
            Assert.IsTrue(saltus.CanMove(Context(SpellId.Saltus, player)), "Saltus works from the ground.");
            saltus.MoveCaster(Context(SpellId.Saltus, player));

            float peak = startY;
            bool refusedInAir = false;
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.transform.position.y);
                if (i == 10)
                    refusedInAir = !saltus.CanMove(Context(SpellId.Saltus, player));
            }

            Assert.Greater(peak - startY, 3f, $"Saltus must be a high jump; peaked {peak - startY:0.00} m up.");
            Assert.IsTrue(refusedInAir, "Cast in mid-air, Saltus must fizzle instead of costing mana.");
        }

        [UnityTest]
        public IEnumerator Test_JumpInTheAirAfterSaltusSlamsAndHurtsWhatIsAround()
        {
            PlayerStateMachine player = MakePlayerOnFloor(new Vector3(0f, 0f, 700f));
            yield return Settle(player);

            var guard = Track(new GameObject("Guard"));
            guard.transform.position = player.transform.position + new Vector3(1.5f, 0f, 0f);
            guard.AddComponent<BoxCollider>();
            SlamTarget target = guard.AddComponent<SlamTarget>();

            Mover(SpellId.Saltus).MoveCaster(Context(SpellId.Saltus, player));
            for (int i = 0; i < 15; i++)
                yield return new WaitForFixedUpdate();

            Assert.IsTrue(player.IsSlamArmed, "A Saltus launch must arm the slam.");
            player.Jump();
            Assert.IsTrue(player.IsSlamming, "Jump in the air after Saltus is the slam.");

            for (int i = 0; i < 150 && player.IsSlamming; i++)
                yield return new WaitForFixedUpdate();

            Assert.IsFalse(player.IsSlamming, "The slam must reach the ground.");
            Assert.Less(target.Health, 100f, "The slam's landing must hurt what is beside it.");
        }

        [UnityTest]
        public IEnumerator Test_JumpInTheAirWithoutSaltusIsNotASlam()
        {
            PlayerStateMachine player = MakePlayerOnFloor(new Vector3(0f, 0f, 800f));
            yield return Settle(player);

            player.Jump();
            for (int i = 0; i < 8; i++)
                yield return new WaitForFixedUpdate();
            player.Jump();

            Assert.IsFalse(player.IsSlamming, "Only a Saltus launch arms the slam.");
        }

        [UnityTest]
        public IEnumerator Test_AMisfiredSaltusIsAFeebleHopThatLocksTheLegs()
        {
            PlayerStateMachine player = MakePlayerOnFloor(new Vector3(0f, 0f, 900f));
            yield return Settle(player);

            float startY = player.transform.position.y;
            Mover(SpellId.MisfireSaltus).MoveCaster(Context(SpellId.MisfireSaltus, player));
            Assert.IsTrue(player.IsStaggered, "A misfired Saltus must lock the legs.");
            Assert.IsFalse(player.IsSlamArmed, "…and never arms the slam.");

            float peak = startY;
            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.transform.position.y);
            }
            Assert.Less(peak - startY, 1f, $"A misfire is a feeble hop, not a leap; peaked {peak - startY:0.00} m.");
        }

        [UnityTest]
        public IEnumerator Test_AMisfiredVeloxStillDashes()
        {
            PlayerStateMachine player = MakePlayerOnFloor(new Vector3(0f, 0f, 1000f));
            yield return Settle(player);

            Vector3 before = player.transform.position;
            Mover(SpellId.MisfireVelox).MoveCaster(Context(SpellId.MisfireVelox, player));
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Vector3 moved = player.transform.position - before;
            Assert.Greater(new Vector2(moved.x, moved.z).magnitude, 2f,
                "A misfired Velox is a full dash, just not where you meant.");
        }

        private class SlamTarget : MonoBehaviour, Interfaces.IHealth
        {
            public float Health = 100f;
            public float CurrentHealth => Health;
            public float MaxHealth => 100f;
            public void TakeDamage(float damage) => Health = Mathf.Max(0f, Health - damage);
            public void TakeDamage(float damage, float impactVelocity) => TakeDamage(damage);
        }
    }
}
