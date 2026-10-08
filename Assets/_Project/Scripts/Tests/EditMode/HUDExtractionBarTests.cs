using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.UI.Screens;
using UnityEngine;

namespace Plunderspell.Tests.EditMode
{
    /// <summary>The "LEAVING IN" bar follows the countdown, even one that ended while the HUD was hidden.</summary>
    public class HUDExtractionBarTests
    {
        private HUDScreen _hud;

        [SetUp]
        public void SetUp()
        {
            TestEventBus.Create();
            GameServices.Initialize();
            _hud = new GameObject("HUDScreen", typeof(RectTransform)).AddComponent<HUDScreen>();
            _hud.Build();
        }

        [TearDown]
        public void TearDown()
        {
            GameServices.Extraction.CancelExtraction();
            Object.DestroyImmediate(_hud.gameObject);
            TestEventBus.Destroy();
        }

        private bool BarShown => _hud.transform.Find("ExtractionGroup").gameObject.activeSelf;

        [Test]
        public void ACountdownEndedWhileHiddenIsNotShownStuckOnReturn()
        {
            _hud.SetVisible(false);
            _hud.SetVisible(true);
            GameServices.Extraction.StartExtraction();
            GameServices.Extraction.Tick(GameServices.Extraction.DurationSeconds - 1f);
            Assert.IsTrue(BarShown, "the bar shows while the countdown runs");

            _hud.SetVisible(false); // died, or the raid ended: the HUD hides first
            GameServices.Extraction.CancelExtraction();
            _hud.SetVisible(true); // the next raid

            Assert.IsFalse(BarShown, "the bar was left at its last reading");
        }
    }
}
