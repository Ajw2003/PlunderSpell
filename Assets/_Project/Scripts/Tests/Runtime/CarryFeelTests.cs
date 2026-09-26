using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>
    /// How a held item follows the beam (#144, docs/plans/carry-like-repo.md): it trails a moving
    /// target without jolting, heavy things lag more, it hangs from the point it was grabbed by, and
    /// anything past the beam's strength drags on the floor instead of lifting.
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

        /// <summary>Strafes a holder at walking speed and turns round; returns the worst trail
        /// and the biggest one-frame change in it (a jolt).</summary>
        private IEnumerator WalkAndTurn(float mass, float x, float[] worst)
        {
            Rigidbody holder = MakeHolder(new Vector3(x, 1f, 0f));
            Vector3 offset = new Vector3(0f, 0.3f, 1.5f);
            Item item = MakeItem(holder.position + offset, mass);
            item.StartDragging(holder.gameObject);

            float previous = 0f;
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                holder.linearVelocity = new Vector3(t < 1f ? 4f : -4f, 0f, 0f);
                item.UpdateTargetPosition(holder.transform.position + offset);
                yield return null;
                float trail = Vector3.Distance(item.GripWorldPosition, holder.transform.position + offset);
                if (t > 0.3f)
                {
                    worst[0] = Mathf.Max(worst[0], trail);
                    worst[1] = Mathf.Max(worst[1], Mathf.Abs(trail - previous));
                }
                previous = trail;
            }
        }

        [UnityTest]
        public IEnumerator Test_WalkingDoesNotJoltAHeldItem()
        {
            var light = new float[2];
            var heavy = new float[2];
            yield return WalkAndTurn(2f, -60f, light);
            yield return WalkAndTurn(8f, 60f, heavy);

            Assert.That(light[0], Is.LessThan(0.5f),
                $"A 2 kg item trailed its target by {light[0]:F3} m at walking pace; light loot should keep up.");
            Assert.That(heavy[0], Is.GreaterThan(light[0]),
                "An 8 kg item should trail further than a 2 kg one when you turn round: weight shows.");
            Assert.That(Mathf.Max(light[1], heavy[1]), Is.LessThan(0.05f),
                $"The trail jumped {Mathf.Max(light[1], heavy[1]):F3} m in one frame; turning round should not jolt it.");
        }

        [UnityTest]
        public IEnumerator Test_AMouseSwingStillLagsMoreForAHeavyItem()
        {
            Rigidbody holderA = MakeHolder(new Vector3(-20f, 1f, 0f));
            Rigidbody holderB = MakeHolder(new Vector3(20f, 1f, 0f));
            Vector3 offset = new Vector3(0f, 0.3f, 1.5f);
            Item light = MakeItem(holderA.position + offset, mass: 1f);
            Item heavy = MakeItem(holderB.position + offset, mass: 8f);
            light.StartDragging(holderA.gameObject);
            heavy.StartDragging(holderB.gameObject);
            light.UpdateTargetPosition(holderA.position + offset);
            heavy.UpdateTargetPosition(holderB.position + offset);
            for (int i = 0; i < 30; i++)
                yield return new WaitForFixedUpdate();

            // The hand swings a metre sideways in one go.
            Vector3 lightStart = light.GripWorldPosition, heavyStart = heavy.GripWorldPosition;
            light.UpdateTarget(holderA.position + offset + Vector3.right, Vector3.zero);
            heavy.UpdateTarget(holderB.position + offset + Vector3.right, Vector3.zero);
            for (int i = 0; i < 6; i++)
                yield return new WaitForFixedUpdate();

            float lightMoved = light.GripWorldPosition.x - lightStart.x;
            float heavyMoved = heavy.GripWorldPosition.x - heavyStart.x;
            Assert.That(heavyMoved, Is.LessThan(lightMoved * 0.8f),
                $"After a mouse swing the 8 kg item moved {heavyMoved:F3} m and the 1 kg item {lightMoved:F3} m; weight should show.");
        }

        [UnityTest]
        public IEnumerator Test_AnItemIsHeldByThePointItWasGrabbedBy()
        {
            Rigidbody holder = MakeHolder(new Vector3(0f, 1f, 40f));
            Vector3 target = holder.position + new Vector3(0f, 0.3f, 1.5f);
            Item item = MakeItem(target, mass: 2f);
            Vector3 grabbed = target + new Vector3(0.3f, 0.5f, 0f); // the top edge of the cube
            item.StartDragging(holder.gameObject, grabbed);
            item.UpdateTarget(target, Vector3.zero);
            for (int i = 0; i < 120; i++)
                yield return new WaitForFixedUpdate();

            float error = Vector3.Distance(item.GripWorldPosition, target);
            Assert.That(error, Is.LessThan(0.05f),
                $"The grabbed point is {error:F3} m from where it is held; the item is held somewhere else.");
        }

        [UnityTest]
        public IEnumerator Test_AnOffCentreGrabLetsTheItemHangAndTurn()
        {
            Rigidbody holder = MakeHolder(new Vector3(0f, 1f, 80f));
            Vector3 target = holder.position + new Vector3(0f, 0.3f, 1.5f);
            Item item = MakeItem(target, mass: 2f);
            Quaternion before = item.transform.rotation;
            item.StartDragging(holder.gameObject, target + new Vector3(0.5f, 0f, 0f)); // a side face
            item.UpdateTarget(target + new Vector3(0.5f, 0f, 0f), Vector3.zero);
            for (int i = 0; i < 120; i++)
                yield return new WaitForFixedUpdate();

            float turned = Quaternion.Angle(before, item.transform.rotation);
            Assert.That(turned, Is.GreaterThan(30f),
                $"Held by one side, the item turned {turned:F1} degrees; it should swing down to hang below the grab.");
        }

        [Test]
        public void Test_TheBeamWarmsWithLoadAndTurnsRedPastTheLimit()
        {
            Color easy = GrabBeam.ColourFor(0.1f);
            Color straining = GrabBeam.ColourFor(0.9f);
            Color dragging = GrabBeam.ColourFor(1.2f);

            Assert.Greater(easy.b, straining.b, "A light load reads cool (violet), a heavy one warm.");
            Assert.Greater(dragging.r, dragging.g * 3f, "Past the lift limit the beam is red.");
        }

        [UnityTest]
        public IEnumerator Test_AnItemTooHeavyToLiftIsDraggedAlongTheFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position = new Vector3(0f, 0f, 120f);
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            _made.Add(floor);

            Rigidbody holder = MakeHolder(new Vector3(0f, 1f, 117f));
            Item chest = MakeItem(new Vector3(0f, 0.5f, 120f), mass: 15f);
            Assert.IsTrue(chest.IsTooHeavyToLift);
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Vector3 start = chest.transform.position;
            chest.StartDragging(holder.gameObject, start + Vector3.up * 0.5f);
            Vector3 wanted = start + new Vector3(0f, 1.5f, -2f); // up and toward the holder
            for (int i = 0; i < 90; i++)
            {
                chest.UpdateTarget(wanted, Vector3.zero);
                yield return new WaitForFixedUpdate();
            }

            Vector3 end = chest.transform.position;
            Assert.That(end.y, Is.LessThan(start.y + 0.3f),
                $"The 15 kg chest rose {end.y - start.y:F2} m; past the beam's strength it should stay on the floor.");
            Assert.That(start.z - end.z, Is.GreaterThan(0.3f),
                $"The chest slid {start.z - end.z:F2} m toward the holder; it should still be dragged.");
        }
    }
}
