using System.Collections;
using System.Collections.Generic;
using Code.Scripts.EventSystems;
using NUnit.Framework;
using Plunderspell.Voice;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The Settings microphone meter's source (#303): with the meter mode on, the voice service opens the real
    /// microphone and publishes its loudness without recognising anything. Needs a microphone, so it ignores itself
    /// on a machine with none.
    /// </summary>
    public class MicMeterTests
    {
        [UnityTest]
        public IEnumerator Test_TheMeterModePublishesTheRealMicrophoneLevelAndStopsWhenSwitchedOff()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
                Assert.Ignore("No microphone on this machine.");

            var service = new VoskVoiceInputService();
            var levels = new List<float>();
            var phrases = 0;
            EventManager.Instance.Subscribe(this, (MicLevelChanged e) => levels.Add(e.Level));
            EventManager.Instance.Subscribe(this, (PhraseRecognized e) => phrases++);
            try
            {
                service.MeterEnabled = true;
                Assert.IsTrue(service.MeterEnabled, "The meter must find a microphone to open.");
                Assert.IsFalse(service.IsListening, "Metering is not listening for a spell.");

                yield return new WaitForSeconds(1.5f);
                Assert.Greater(levels.Count, 0, "The open microphone must publish its level as it changes.");
                Assert.AreEqual(0, phrases, "Metering recognises nothing.");

                service.MeterEnabled = false;
                int after = levels.Count;
                yield return new WaitForSeconds(0.5f);
                Assert.LessOrEqual(levels.Count - after, 1, "Switched off, it stops publishing (one last reset to zero at most).");
            }
            finally
            {
                EventManager.Instance.UnsubscribeFromAllEvents(this);
                service.MeterEnabled = false;
                service.CloseMicrophone();
            }
        }
    }
}
