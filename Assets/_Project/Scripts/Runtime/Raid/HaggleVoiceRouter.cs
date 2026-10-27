using Code.Scripts.EventSystems;
using Plunderspell.Market;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Sends a spoken Plus, Satis or Vale to the nearest counter with an open haggle within reach of the local player,
    /// by the same call the 1/2/3 keys make (<see cref="SellCounter.Speak"/>). With no such counter the word is ignored.
    /// SpellCastingSystem skips these words, so they never become a spell, a misfire or a fizzle.
    /// </summary>
    public class HaggleVoiceRouter : MonoBehaviour
    {
        private static HaggleVoiceRouter s_instance;

        /// <summary>One router for the scene; the first counter to enable makes it.</summary>
        public static void Ensure()
        {
            if (s_instance != null)
                return;
            s_instance = new GameObject("HaggleVoiceRouter").AddComponent<HaggleVoiceRouter>();
        }

        private void OnEnable() =>
            EventManager.Instance?.Subscribe(this, (PhraseRecognized e) => Hear(e.Result.NormalizedText));

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        /// <summary>The recogniser's text for one phrase. True when it was a haggling word and a counter took it.</summary>
        public static bool Hear(string normalizedText)
        {
            if (!HaggleWords.TryParse(normalizedText, out HaggleWord word))
                return false;

            SellCounter nearest = null;
            float best = float.PositiveInfinity;
            foreach (SellCounter counter in SellCounter.All)
            {
                float distance = counter.ListeningDistance();
                if (distance < best)
                {
                    best = distance;
                    nearest = counter;
                }
            }
            if (nearest == null)
                return false;
            nearest.Speak(word);
            return true;
        }
    }
}
