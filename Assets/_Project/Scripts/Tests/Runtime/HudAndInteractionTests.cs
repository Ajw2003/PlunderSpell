using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Lair;
using Plunderspell.Loot;
using Plunderspell.Raid;
using Plunderspell.UI;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Tests for the two things standing between working systems and a playable game: being able to
    /// pick loot up, and being told what is going on.
    ///
    /// The HUD is tested through <see cref="RaidHudModel"/> — what the player is TOLD is logic and
    /// worth asserting; how it is drawn is not.
    /// </summary>
    public class HudAndInteractionTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("TotalDebt");
            PlayerPrefs.DeleteKey("AccumulatedGold");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        /// <param name="withItem">Adds an <see cref="Item"/> before the <see cref="LootPickup"/> so
        /// <c>LootPickup.IsBeingCarried</c> (which reads the piece's own Item) has one to read —
        /// LootPickup's Awake caches <c>GetComponent&lt;Item&gt;()</c>, so the Item must exist first.</param>
        private LootPickup MakeLoot(string name, float bulk, Vector3 position, bool withItem = false)
        {
            var go = Track(new GameObject($"Loot_{name}"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            if (withItem)
                go.AddComponent<Item>();

            var pickup = go.AddComponent<LootPickup>();
            var data = Track(ScriptableObject.CreateInstance<LootItem>());
            data.DisplayName = name;
            data.WeightKg = bulk;
            data.Worth = 100f;
            data.Fragility = 8f;
            pickup.SetData(data);
            return pickup;
        }

        /// <summary>A player looking down +Z from the origin.</summary>
        private LootInteractor MakePlayer()
        {
            var go = Track(new GameObject("Player"));
            go.transform.position = Vector3.zero;
            return go.AddComponent<LootInteractor>();
        }

        /// <summary>An <see cref="ItemManager"/> singleton reporting <paramref name="carried"/> as
        /// what it is dragging — driven through the private field it exposes as
        /// <see cref="ItemManager.CarriedItem"/>, since driving the real hover/click input needs a
        /// live camera and raycast the HUD tests do not set up.</summary>
        private ItemManager MakeItemManager(Item carried = null)
        {
            var go = Track(new GameObject("ItemManager"));
            var manager = go.AddComponent<ItemManager>();
            if (carried != null)
            {
                typeof(ItemManager)
                    .GetField("_draggedItem", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(manager, carried);
            }
            return manager;
        }

        // --- Interaction ---------------------------------------------------------------------

        [Test]
        public void Test_LookingAtLootFocusesIt()
        {
            LootInteractor player = MakePlayer();
            LootPickup vase = MakeLoot("Vase", 2f, new Vector3(0f, 0f, 2f));

            player.UpdateFocus();

            Assert.AreSame(vase, player.Focus, "Loot within reach and in front must be focused.");
        }

        [Test]
        public void Test_LootOutOfReachIsNotFocused()
        {
            LootInteractor player = MakePlayer();
            MakeLoot("Vase", 2f, new Vector3(0f, 0f, 30f));

            player.UpdateFocus();

            Assert.IsNull(player.Focus, "You cannot grab across a room.");
        }

        [Test]
        public void Test_TakingAndDroppingLoot()
        {
            // Grabbing is left-click through ItemManager now, not the interact key: drive the piece's
            // own Item, the way ItemManager's beam does (see CarryFeelTests).
            LootPickup vase = MakeLoot("Vase", 2f, new Vector3(0f, 0f, 2f), withItem: true);
            var item = vase.GetComponent<Item>();
            var holder = Track(new GameObject("Holder"));

            item.StartDragging(holder);

            Assert.IsTrue(vase.IsBeingCarried, "Dragging the item must read back as carried.");

            item.StopDragging();

            Assert.IsFalse(vase.IsBeingCarried, "Letting go must clear the carried state.");
        }

        [Test]
        public void Test_HeavyLootIsFlaggedAsNeedingTwoPeople()
        {
            LootInteractor player = MakePlayer();
            LootPickup chest = MakeLoot("Gilded Chest", LootItem.DualCarryBulkThreshold + 4f,
                new Vector3(0f, 0f, 2f));

            player.UpdateFocus();

            Assert.IsTrue(player.FocusRequiresHelp,
                "A player must be able to tell a two-person lift before they try it.");
            Assert.IsTrue(chest.Data.RequiresDualCarry);
        }

        [Test]
        public void Test_ADoorInReachIsOpenedByTheInteractKey()
        {
            LootInteractor player = MakePlayer();

            var doorGo = Track(new GameObject("Door"));
            doorGo.transform.position = new Vector3(0f, 0f, 2f);
            doorGo.AddComponent<BoxCollider>();
            CastleDoor door = doorGo.AddComponent<CastleDoor>();
            doorGo.AddComponent<CastleDoorHandle>().SetDoor(door);

            player.UpdateFocus();
            Assert.IsNotNull(player.FocusDoor);

            player.Interact();
            Assert.IsTrue(door.IsOpen, "An unlocked door opens by hand.");
        }

        [Test]
        public void Test_ALockedDoorDoesNotOpenByHandButPortaOpensIt()
        {
            var doorGo = Track(new GameObject("Door"));
            CastleDoor door = doorGo.AddComponent<CastleDoor>();
            door.Lock();

            Assert.IsFalse(door.TryOpenByHand(), "A locked door must not open by hand.");
            Assert.IsFalse(door.IsOpen);

            door.Open();   // this is what Porta calls
            Assert.IsTrue(door.IsOpen, "Porta ignores the lock — that is what the word is for.");
        }

        // --- HUD ------------------------------------------------------------------------------

        private RaidHudPresenter MakeHud(out ExtractionZone zone, out AlarmFSMManager alarm,
            out LootInteractor interactor)
        {
            var zoneGo = Track(new GameObject("Zone"));
            // Well away from the player, so the zone's own collider cannot sit on the interaction ray.
            zoneGo.transform.position = new Vector3(50f, 0f, 0f);
            zoneGo.AddComponent<BoxCollider>().isTrigger = true;
            zone = zoneGo.AddComponent<ExtractionZone>();

            var alarmGo = Track(new GameObject("Alarm"));
            alarm = alarmGo.AddComponent<AlarmFSMManager>();

            var lairGo = Track(new GameObject("Lair"));
            LairHubManager lair = lairGo.AddComponent<LairHubManager>();

            interactor = MakePlayer();

            var hudGo = Track(new GameObject("Hud"));
            var presenter = hudGo.AddComponent<RaidHudPresenter>();
            presenter.Configure(null, zone, alarm, lair, interactor);
            return presenter;
        }

        [Test]
        public void Test_TheHudShowsTheClockAndTheAlarm()
        {
            RaidHudPresenter hud = MakeHud(out ExtractionZone zone, out AlarmFSMManager alarm, out _);
            zone.SetRaidDuration(125f);
            alarm.SetAlarmLevel(60f);

            RaidHudModel model = hud.Build();

            Assert.AreEqual("02:05", model.TimerText);
            Assert.AreEqual(AlarmState.Roused, model.Alarm);
            Assert.AreEqual(0.6f, model.AlarmFill, 0.001f);
        }

        [Test]
        public void Test_TheClockIsMarkedCriticalInsideTheLastMinute()
        {
            RaidHudPresenter hud = MakeHud(out ExtractionZone zone, out _, out _);

            zone.SetRaidDuration(90f);
            Assert.IsFalse(hud.Build().TimerIsCritical);

            zone.SetRaidDuration(45f);
            Assert.IsTrue(hud.Build().TimerIsCritical,
                "A raid is lost by not noticing the clock; the last minute must be shouted about.");
        }

        [Test]
        public void Test_TimerNeverReadsNegative()
        {
            Assert.AreEqual("00:00", RaidHudModel.FormatTime(-30f));
            Assert.AreEqual("00:00", RaidHudModel.FormatTime(0f));
            Assert.AreEqual("10:00", RaidHudModel.FormatTime(600f));
        }

        [Test]
        public void Test_TheHudPromptsForATwoPersonLift()
        {
            RaidHudPresenter hud = MakeHud(out _, out _, out LootInteractor interactor);
            MakeLoot("Gilded Chest", LootItem.DualCarryBulkThreshold + 4f, new Vector3(0f, 0f, 2f));

            interactor.UpdateFocus();
            RaidHudModel model = hud.Build();

            StringAssert.Contains("grab with a friend", model.InteractPrompt,
                "A player who does not know an item is a two-person lift will just stand there.");
        }

        [Test]
        public void Test_TheHudNamesWhatYouAreCarrying()
        {
            RaidHudPresenter hud = MakeHud(out _, out _, out _);
            LootPickup plate = MakeLoot("Silver Plate", 2f, new Vector3(0f, 0f, 2f), withItem: true);
            var item = plate.GetComponent<Item>();
            Track(plate.gameObject.AddComponent<LootValue>()).SetItem(plate.Data);
            MakeItemManager(item);

            RaidHudModel model = hud.Build();
            Assert.AreEqual("Silver Plate", model.CarriedLootName);
            Assert.IsFalse(model.CarriedNeedsTwo);
        }

        [Test]
        public void Test_TheAlarmTextEscalatesWithTheAlarm()
        {
            RaidHudPresenter hud = MakeHud(out _, out AlarmFSMManager alarm, out _);

            alarm.SetAlarmLevel(0f);
            StringAssert.Contains("Calm", hud.Build().AlarmText);

            alarm.SetAlarmLevel(30f);
            StringAssert.Contains("STIRRED", hud.Build().AlarmText);

            alarm.SetAlarmLevel(90f);
            StringAssert.Contains("HUE AND CRY", hud.Build().AlarmText);
        }
    }
}
