using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>
    /// How a held item follows its holder: rigidly with the holder's body, weighted only against the
    /// hand (mouse) moving it, and held at its grip rather than its base.
    /// See docs/plans/staging-playtest-2-2026-09-24.md, part 1.
    /// </summary>
    public class CarryFeelTests
    {
        private readonly System.Collections.Generic.List<GameObject> _made = new System.Collections.Generic.List<GameObject>();

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

        private Rigidbody MakeHolder(Vector3 position)
        {
            var holder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            holder.transform.position = position;
            var body = holder.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            // As the player's body is (PlayerStateMachine): the camera, and so the hand's point,
            // follow its interpolated pose, the same one the held item is drawn at.
            body.interpolation = RigidbodyInterpolation.Interpolate;
            _made.Add(holder);
            return body;
        }

        private Item MakeItem(Vector3 position, float mass, Vector3? gripLocal = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = position;
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            var item = go.AddComponent<Item>();
            if (gripLocal.HasValue)
            {
                var grip = new GameObject("GripPoint").transform;
                grip.SetParent(go.transform, false);
                grip.localPosition = gripLocal.Value;
                item.SetGripPoint(grip);
            }
            _made.Add(go);
            return item;
        }

        [UnityTest]
        public IEnumerator Test_WalkingDoesNotMakeAHeldItemLag()
        {
            Rigidbody holder = MakeHolder(new Vector3(0f, 1f, 0f));
            Vector3 offset = new Vector3(0f, 0.3f, 1.5f);
            Item item = MakeItem(holder.position + offset, mass: 8f);
            item.StartDragging(holder.gameObject);

            float worst = 0f;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                // Strafe at walking speed; the hand's point rides along with the body, as the
                // camera's crosshair point does.
                holder.linearVelocity = new Vector3(4f, 0f, 0f);
                item.UpdateTargetPosition(holder.transform.position + offset);
                yield return null;
                if (t > 0.3f)
                    worst = Mathf.Max(worst, Vector3.Distance(item.GripWorldPosition, holder.transform.position + offset));
            }

            Assert.That(worst, Is.LessThan(0.05f),
                $"An 8 kg item strafed at 4 m/s trailed its hand point by {worst:F3} m; walking should carry it along.");
        }

        [UnityTest]
        public IEnumerator Test_AMouseSwingStillLagsMoreForAHeavyItem()
        {
            Rigidbody holderA = MakeHolder(new Vector3(-20f, 1f, 0f));
            Rigidbody holderB = MakeHolder(new Vector3(20f, 1f, 0f));
            Vector3 offset = new Vector3(0f, 0.3f, 1.5f);
            Item light = MakeItem(holderA.position + offset, mass: 1f);
            Item heavy = MakeItem(holderB.position + offset, mass: 14f);
            light.StartDragging(holderA.gameObject);
            heavy.StartDragging(holderB.gameObject);
            light.UpdateTargetPosition(holderA.position + offset);
            heavy.UpdateTargetPosition(holderB.position + offset);
            for (int i = 0; i < 30; i++)
                yield return new WaitForFixedUpdate();

            // The hand swings a metre sideways in one go.
            Vector3 lightStart = light.GripWorldPosition, heavyStart = heavy.GripWorldPosition;
            light.UpdateTargetPosition(holderA.position + offset + Vector3.right);
            heavy.UpdateTargetPosition(holderB.position + offset + Vector3.right);
            for (int i = 0; i < 6; i++)
                yield return new WaitForFixedUpdate();

            float lightMoved = light.GripWorldPosition.x - lightStart.x;
            float heavyMoved = heavy.GripWorldPosition.x - heavyStart.x;
            Assert.That(heavyMoved, Is.LessThan(lightMoved * 0.8f),
                $"After a mouse swing the 14 kg item moved {heavyMoved:F3} m and the 1 kg item {lightMoved:F3} m; weight should still show.");
        }

        [UnityTest]
        public IEnumerator Test_AHeldItemIsHeldAtItsGripNotItsBase()
        {
            Rigidbody holder = MakeHolder(new Vector3(0f, 1f, 40f));
            Vector3 target = holder.position + new Vector3(0f, 0.3f, 1.5f);
            Item item = MakeItem(target, mass: 2f, gripLocal: new Vector3(0f, 0.4f, 0f));
            item.StartDragging(holder.gameObject);
            item.UpdateTargetPosition(target);
            for (int i = 0; i < 60; i++)
                yield return new WaitForFixedUpdate();

            float gripError = Vector3.Distance(item.GripWorldPosition, target);
            Assert.That(gripError, Is.LessThan(0.03f),
                $"The grip point is {gripError:F3} m from the hand point; the item is being held somewhere else.");
        }
    }
}
