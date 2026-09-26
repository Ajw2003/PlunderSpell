using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
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
        public IEnumerator Test_AnOffCentreGrabKeepsTheItemsOrientation()
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
            Assert.That(turned, Is.LessThan(10f),
                $"Held by one side, the item flopped round {turned:F1} degrees; it should keep the orientation it was picked up in.");
        }

        [UnityTest]
        public IEnumerator Test_AHeldItemSettlesInsteadOfShaking()
        {
            var report = new System.Text.StringBuilder();
            float worstTurn = 0f, worstMove = 0f;
            float x = -6f;
            foreach (float mass in new[] { 0.5f, 1f, 2f, 5f, 9f })
            {
                Rigidbody holder = MakeHolder(new Vector3(x, 1f, 200f));
                x += 3f;
                Vector3 target = holder.position + new Vector3(0f, 0.3f, 1.5f);
                Item item = MakeItem(target, mass);
                item.StartDragging(holder.gameObject, target + new Vector3(0.45f, 0.2f, -0.3f)); // a corner
                item.UpdateTarget(target + new Vector3(0.45f, 0.2f, -0.3f), Vector3.zero);
                for (int i = 0; i < 75; i++)
                    yield return new WaitForFixedUpdate();

                float turn = 0f, move = 0f;
                Quaternion lastRotation = item.transform.rotation;
                Vector3 lastPosition = item.transform.position;
                for (int i = 0; i < 50; i++)
                {
                    yield return new WaitForFixedUpdate();
                    turn = Mathf.Max(turn, Quaternion.Angle(lastRotation, item.transform.rotation));
                    move = Mathf.Max(move, Vector3.Distance(lastPosition, item.transform.position));
                    lastRotation = item.transform.rotation;
                    lastPosition = item.transform.position;
                }
                report.Append($"{mass} kg: worst step {turn:F2} deg, {move * 1000f:F1} mm\n");
                worstTurn = Mathf.Max(worstTurn, turn);
                worstMove = Mathf.Max(worstMove, move);
            }
#if UNITY_EDITOR
            // The light loot that shook in play, as forged: grabbed by a corner of its collider.
            foreach (string path in new[]
            {
                "Assets/_Project/Prefabs/Loot/BronzeAge/FaienceHippopotamus.prefab",
                "Assets/_Project/Prefabs/Loot/GoldenGoblet.prefab",
                "Assets/_Project/Prefabs/Loot/AgeOfPowder/NautilusCup.prefab",
                "Assets/_Project/Prefabs/Loot/LateMedieval/JewelledHatBadge.prefab",
            })
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Rigidbody holder = MakeHolder(new Vector3(x, 1f, 200f));
                x += 3f;
                Vector3 target = holder.position + new Vector3(0f, 0.3f, 1.5f);
                GameObject piece = Object.Instantiate(prefab, target, prefab.transform.rotation);
                _made.Add(piece);
                var item = piece.GetComponent<Item>();
                Bounds box = piece.GetComponent<Collider>().bounds;
                Vector3 corner = box.center + Vector3.Scale(box.extents, new Vector3(0.9f, 0.9f, -0.9f));
                item.StartDragging(holder.gameObject, corner);
                item.UpdateTarget(corner, Vector3.zero);
                for (int i = 0; i < 75; i++)
                    yield return new WaitForFixedUpdate();

                float turn = 0f, move = 0f;
                Quaternion lastRotation = piece.transform.rotation;
                Vector3 lastPosition = piece.transform.position;
                for (int i = 0; i < 50; i++)
                {
                    yield return new WaitForFixedUpdate();
                    turn = Mathf.Max(turn, Quaternion.Angle(lastRotation, piece.transform.rotation));
                    move = Mathf.Max(move, Vector3.Distance(lastPosition, piece.transform.position));
                    lastRotation = piece.transform.rotation;
                    lastPosition = piece.transform.position;
                }
                report.Append($"{prefab.name} ({item.Mass} kg): worst step {turn:F2} deg, {move * 1000f:F1} mm\n");
                worstTurn = Mathf.Max(worstTurn, turn);
                worstMove = Mathf.Max(worstMove, move);
            }
#endif
            Debug.Log("[CarryFeel] Settling:\n" + report);
            Assert.That(worstTurn, Is.LessThan(0.2f), "A held item still shakes after settling.\n" + report);
            Assert.That(worstMove, Is.LessThan(0.002f), "A held item still shakes after settling.\n" + report);
        }

        [UnityTest]
        public IEnumerator Test_AHeldItemTurnsWithTheHolder()
        {
            Rigidbody holder = MakeHolder(new Vector3(0f, 1f, 90f));
            Vector3 target = holder.position + new Vector3(0f, 0.3f, 1.5f);
            Item item = MakeItem(target, mass: 2f);
            item.SetViewYaw(0f);
            item.StartDragging(holder.gameObject);
            item.UpdateTarget(target, Vector3.zero);
            item.SetViewYaw(90f);
            for (int i = 0; i < 150; i++)
                yield return new WaitForFixedUpdate();

            float yaw = item.transform.eulerAngles.y;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)), Is.LessThan(10f),
                $"The holder turned 90 degrees and the item is at {yaw:F1}; it should turn with them.");
        }

        /// <summary>A crossbow-shaped item: its mesh reaches 0.97 m along local +Y from its origin,
        /// as the forged Crossbow and Matchlock do.</summary>
        private Item MakeCrossbow(Vector3 position)
        {
            var go = new GameObject("Crossbow");
            go.transform.position = position;
            go.AddComponent<Rigidbody>().mass = 3f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.49f, 0.12f);
            body.transform.localScale = new Vector3(0.06f, 0.97f, 0.23f);
            var item = go.AddComponent<Item>();
            _made.Add(go);
            return item;
        }

        [UnityTest]
        public IEnumerator Test_AWeaponInTheHandPointsWhereYouLookAndDoesNotStopItsOwnShot()
        {
            Rigidbody holder = MakeHolder(new Vector3(0f, 1f, 150f));
            Item crossbow = MakeCrossbow(holder.position + Vector3.forward * 2f);
            crossbow.StartDragging(holder.gameObject);
            crossbow.HoldInHand(true);

            Quaternion view = Quaternion.LookRotation(new Vector3(1f, 0f, 1f).normalized);
            Vector3 hand = holder.position + new Vector3(0f, 0.6f, 0f);
            crossbow.SetHandPose(hand, view);
            Physics.SyncTransforms();

            Vector3 barrel = crossbow.transform.TransformDirection(Vector3.up);
            Assert.That(Vector3.Angle(barrel, view * Vector3.forward), Is.LessThan(1f),
                "The crossbow's barrel should point along the view.");
            Assert.That(Vector3.Distance(crossbow.transform.position, hand), Is.LessThan(0.01f),
                "It is held by its origin (the butt of the stock), at the hand.");

            // A shot fired down the barrel from behind the hand passes straight through the weapon.
            var shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shot.transform.localScale = Vector3.one * 0.05f;
            shot.transform.position = hand + barrel * 0.2f + crossbow.transform.TransformDirection(new Vector3(0f, 0f, 0.12f));
            var shotBody = shot.AddComponent<Rigidbody>();
            shotBody.useGravity = false;
            shotBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            shotBody.linearVelocity = barrel * 30f;
            _made.Add(shot);
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Dot(shot.transform.position - hand, barrel), Is.GreaterThan(2f),
                "The weapon in the hand stopped a shot fired along it.");

            crossbow.StopDragging();
            Assert.IsFalse(crossbow.GetComponent<Rigidbody>().isKinematic, "Let go, it is a physics body again.");
            Assert.IsFalse(crossbow.GetComponentInChildren<Collider>().isTrigger, "Let go, it collides again.");
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

        [Test]
        public void Test_AHeavyPieceIsTowedBehindOnARope()
        {
            Vector3 feet = Vector3.zero;
            Vector3 walking = new Vector3(0f, 0f, 2f); // walking away from the piece at 2 m/s

            Vector3 taut = Item.TowVelocity(feet, new Vector3(0f, 0.4f, -2.001f), walking, 2f);
            Assert.That(Vector3.Distance(taut, walking), Is.LessThan(0.01f),
                "At the rope's length the piece is driven at the holder's own pace, no faster.");

            Vector3 stretched = Item.TowVelocity(feet, new Vector3(0f, 0.4f, -3f), walking, 2f);
            Assert.That(stretched.z, Is.GreaterThan(2f).And.LessThan(4f), "A stretched rope catches up gently.");

            Assert.AreEqual(Vector3.zero, Item.TowVelocity(feet, new Vector3(1f, 0.4f, -1f), walking, 2f),
                "A slack rope pulls nothing.");
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator Test_OnePlayerCanDragEveryTwoPersonPieceBehindThem()
        {
            // Two-person pieces (LootItem.RequiresDualCarry) are heavy, not immovable: one player
            // drags them, slowly (the owner's call, 2026-09-26).
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.5f, 300f);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);
            _made.Add(floor);

            var report = new System.Text.StringBuilder();
            var failures = new System.Text.StringBuilder();
            int checkedCount = 0;
            float x = -20f;
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Loot" }))
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                var pickup = prefab.GetComponent<Plunderspell.Loot.LootPickup>();
                if (pickup == null || pickup.Data == null || !pickup.Data.RequiresDualCarry)
                    continue;

                GameObject piece = Object.Instantiate(prefab, new Vector3(x, 0.05f, 300f), prefab.transform.rotation);
                _made.Add(piece);
                x += 8f;
                for (int i = 0; i < 30; i++)
                    yield return new WaitForFixedUpdate();

                Rigidbody holder = MakeHolder(piece.transform.position + new Vector3(0f, 1f, -3f));
                var item = piece.GetComponent<Item>();
                Vector3 start = piece.transform.position;
                item.StartDragging(holder.gameObject);
                // The holder walks away at the pace towing allows them (PlayerWalkState: the
                // RaidPlayer's 5 m/s walk times the piece's TowSpeedMultiplier), pulling it behind.
                float pace = 5f * item.TowSpeedMultiplier;
                Assert.That(pace, Is.LessThanOrEqualTo(3f), $"Towing {prefab.name} should slow the holder to at most 60 %.");
                float topSpeed = 0f;
                for (float t = 0f; t < 3f; t += Time.fixedDeltaTime)
                {
                    // As PlayerWalkState does: a lagging piece holds the holder back.
                    float now = 5f * item.TowSpeedMultiplier;
                    holder.MovePosition(holder.position + new Vector3(0f, 0f, -now) * Time.fixedDeltaTime);
                    if (t > 0.5f)
                        topSpeed = Mathf.Max(topSpeed, Vector3.ProjectOnPlane(piece.GetComponent<Rigidbody>().linearVelocity, Vector3.up).magnitude);
                    item.SetTow(holder.position + Vector3.down, new Vector3(0f, 0f, -now), 3f); // the rope is as long as the reach it was grabbed at
                    yield return new WaitForFixedUpdate();
                }
                item.StopDragging();

                float moved = start.z - piece.transform.position.z;
                float behind = piece.transform.position.z - holder.position.z;
                report.Append($"{prefab.name} ({piece.GetComponent<Rigidbody>().mass} kg): holder {pace:F1} m/s, piece moved {moved:F2} m, {behind:F2} m behind, top speed {topSpeed:F1} m/s\n");
                if (behind < 1.5f || behind > 4.2f)
                    failures.Append($"{prefab.name} ended {behind:F2} m behind the holder; on a 3 m rope it should trail about 3 m behind.\n");
                if (topSpeed > pace * 1.5f)
                    failures.Append($"{prefab.name} surged to {topSpeed:F1} m/s behind a holder walking {pace:F1} m/s; a towed piece should plod.\n");
                checkedCount++;
            }
            Debug.Log("[CarryFeel] Dragged alone:\n" + report);
            Assert.Greater(checkedCount, 3, "Sanity: the two-person pieces were found.");
            Assert.IsEmpty(failures.ToString(), "\n" + report);
        }
#endif

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
