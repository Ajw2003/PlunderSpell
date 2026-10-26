using System.Collections.Generic;
using System.Reflection;
using Code.Scripts.EventSystems;
using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.Lair;
using Plunderspell.Raid;
using Plunderspell.Tests.EditMode;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// HaulLanding saves and restores only on events, never from a per-frame check. With no NetworkManager
    /// the "server" is always ready, so each trigger is published on a test bus and its effect read from the
    /// save and the pile's unresolved entries. Edit Mode runs no OnEnable, so the test calls it.
    /// </summary>
    public class HaulLandingTriggerTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private int _slot = SaveSlots.TestSlot;
        private int _previousSlot;
        private GameObject _go;
        private HaulLanding _landing;

        [SetUp]
        public void SetUp()
        {
            _previousSlot = SaveSlots.Active;
            SaveSlots.Active = _slot;
            HaulPileSave.Clear(_slot);
            TestEventBus.Create();
            _go = new GameObject("HaulLandingTest");
            var spawner = _go.AddComponent<LootSpawner>();
            _landing = _go.AddComponent<HaulLanding>();
            typeof(HaulLanding).GetField("_pile", Hidden).SetValue(_landing, spawner);
        }

        [TearDown]
        public void TearDown()
        {
            Enable(false);
            Object.DestroyImmediate(_go);
            TestEventBus.Destroy();
            SaveSlots.Active = _previousSlot;
            HaulPileSave.Clear(_slot);
        }

        private void Enable(bool on) =>
            typeof(HaulLanding).GetMethod(on ? "OnEnable" : "OnDisable", Hidden).Invoke(_landing, null);

        private List<HaulPileEntry> Unresolved() =>
            (List<HaulPileEntry>)typeof(HaulLanding).GetField("_unresolved", Hidden).GetValue(_landing);

        // Enabled and restored with one entry no table resolves, which Save writes back; then the slot is wiped.
        private void RestoredThenWiped()
        {
            HaulPileSave.SaveEntries(_slot, new[] { new HaulPileEntry("Unknown", Vector3.one, Quaternion.identity) });
            Enable(true);
            Assert.AreEqual("Unknown", Unresolved()[0].Name, "Enabling with the server ready restores.");
            HaulPileSave.Clear(_slot);
        }

        [Test]
        public void Test_SettingOutSaves()
        {
            RestoredThenWiped();
            EventManager.Instance.Publish(new RaidPhaseChanged(RaidPhase.Generating));
            Assert.AreEqual(1, HaulPileSave.LoadEntries(_slot).Length);
        }

        [Test]
        public void Test_OtherPhasesDoNotSave()
        {
            RestoredThenWiped();
            EventManager.Instance.Publish(new RaidPhaseChanged(RaidPhase.InLair));
            Assert.AreEqual(0, HaulPileSave.LoadEntries(_slot).Length);
        }

        [Test]
        public void Test_SessionEndingSaves()
        {
            RestoredThenWiped();
            EventManager.Instance.Publish(new SessionEnding());
            Assert.AreEqual(1, HaulPileSave.LoadEntries(_slot).Length);
        }

        [Test]
        public void Test_NothingSavesBeforeARestore()
        {
            HaulPileSave.SaveEntries(_slot, new[] { new HaulPileEntry("Unknown") });
            // Not enabled, so never restored: a save now would wipe the real one.
            typeof(HaulLanding).GetMethod("OnApplicationQuit", Hidden).Invoke(_landing, null);
            Assert.AreEqual(1, HaulPileSave.LoadEntries(_slot).Length);
        }

        [Test]
        public void Test_CoopChangedRestoresAPileThatWasForgotten()
        {
            Enable(true);
            typeof(HaulLanding).GetField("_restored", Hidden).SetValue(_landing, false);
            HaulPileSave.SaveEntries(_slot, new[] { new HaulPileEntry("Later") });
            EventManager.Instance.Publish(new CoopChanged());
            Assert.AreEqual("Later", Unresolved()[0].Name);
        }

        [Test]
        public void Test_SaveSlotLoadedRestoresTheNewSlot()
        {
            Enable(true);
            HaulPileSave.SaveEntries(_slot, new[] { new HaulPileEntry("Other") });
            EventManager.Instance.Publish(new SaveSlotLoaded(_slot));
            Assert.AreEqual("Other", Unresolved()[0].Name);
        }

        [Test]
        public void Test_DisabledStopsListening()
        {
            RestoredThenWiped();
            Enable(false);
            EventManager.Instance.Publish(new RaidPhaseChanged(RaidPhase.Generating));
            Assert.AreEqual(0, HaulPileSave.LoadEntries(_slot).Length);
        }
    }
}
