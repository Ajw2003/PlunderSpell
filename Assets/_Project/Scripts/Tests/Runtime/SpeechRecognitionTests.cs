#if VOSK_PRESENT && !HEADLESS && UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Plunderspell.Spells;
using Plunderspell.Voice;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Runs synthesized speech (Tools/VoiceFixtures/clips, two voices) through the real Vosk model
    /// with the shipped lexicon's vocabulary, then through MisfireEngine, exactly as a held cast key
    /// would. This is what caught every correct pronunciation misfiring: the English model heard
    /// "FRANGO" as "franco", the misfire word.
    /// </summary>
    public class SpeechRecognitionTests
    {
        private const string LexiconPath = "Assets/_Project/Data/Spells/SpellLexicon.asset";

        private static readonly Dictionary<string, SpellId> s_correct = new Dictionary<string, SpellId>
        {
            { "IGNIS", SpellId.Ignis }, { "FRANGO", SpellId.Frango }, { "LEVO", SpellId.Levo },
            { "AURUMVOCO", SpellId.AurumVoco }, { "TONITRUS", SpellId.Tonitrus },
            { "SOMNUS", SpellId.Somnus }, { "CADAVERSURGE", SpellId.CadaverSurge },
            { "PORTA", SpellId.Porta },
        };

        // Pairs the small English model can tell apart. FRANGO/FRANCO (g vs k), and one voice each
        // of LEVO and AURUM VOCO, cannot be separated by it and fall back to the correct spell.
        private static readonly string[] s_separableMisfires =
            { "IGNIS", "TONITRUS", "SOMNUS", "CADAVERSURGE", "PORTA" };

        private static VoskVoiceInputService s_speech;
        private static SpellLexicon s_lexicon;

        [OneTimeSetUp]
        public void LoadModel()
        {
            if (!VoskVoiceInputService.IsModelInstalled)
                Assert.Ignore("No Vosk model installed under StreamingAssets; run Plunderspell/Voice/Download Vosk Small Model.");

            s_lexicon = AssetDatabase.LoadAssetAtPath<SpellLexicon>(LexiconPath);
            Assert.IsNotNull(s_lexicon, $"No lexicon at {LexiconPath}.");
            s_speech = new VoskVoiceInputService();
            s_speech.SetVocabulary(s_lexicon.BuildHeardVocabulary());
        }

        private static SpellId Recognise(string clipName)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "VoiceFixtures", "clips", clipName + ".wav"));
            Assert.IsTrue(File.Exists(path), $"Missing fixture {path}; run Tools/VoiceFixtures/generate.ps1.");

            byte[] wav = File.ReadAllBytes(path);
            const int headerBytes = 44;
            var samples = new short[(wav.Length - headerBytes) / 2];
            System.Buffer.BlockCopy(wav, headerBytes, samples, 0, samples.Length * 2);

            string heard = s_speech.RecognizeSamples(samples);
            return heard == null ? SpellId.None : MisfireEngine.Resolve(heard, s_lexicon);
        }

        [Test]
        public void Test_EverySpellSaidCorrectlyCastsThatSpell(
            [Values("David", "Zira")] string voice,
            [Values("IGNIS", "FRANGO", "LEVO", "AURUMVOCO", "TONITRUS", "SOMNUS", "CADAVERSURGE", "PORTA")] string word)
        {
            SpellId cast = Recognise($"OK_{word}_{voice}");
            Assert.AreEqual(s_correct[word], cast,
                $"Saying {word} correctly ({voice}) cast {cast}. Heard: \"{s_speech.LastHeardText}\". " +
                "Tune that word's HeardAs in the SpellWord asset.");
        }

        [Test]
        public void Test_ASeparableMispronunciationMisfires([Values("David", "Zira")] string voice)
        {
            foreach (string word in s_separableMisfires)
            {
                SpellId cast = Recognise($"MF_{word}_{voice}");
                Assert.IsTrue(SpellCatalogue.IsMisfire(cast),
                    $"Mispronouncing {word} ({voice}) cast {cast}, not a misfire. Heard: \"{s_speech.LastHeardText}\".");
            }
        }

        [Test]
        public void Test_AMispronunciationNeverFizzles([Values("David", "Zira")] string voice)
        {
            foreach (string word in s_correct.Keys)
            {
                SpellId cast = Recognise($"MF_{word}_{voice}");
                Assert.AreNotEqual(SpellId.None, cast,
                    $"Mispronouncing {word} ({voice}) did nothing at all. Heard: \"{s_speech.LastHeardText}\".");
            }
        }
    }
}
#endif
