using System.Collections.Generic;
using NUnit.Framework;
using RogueAi.Spells;
using RogueAi.Voice;
using UnityEngine;

namespace RogueAi.Tests
{
    /// <summary>
    /// EditMode tests for the Milestone 1 voice-casting pipeline: mock provider,
    /// misfire resolution, amplitude classification and Levenshtein distance.
    /// </summary>
    public class VoiceCastingTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            VoiceServiceLocator.Clear();
            foreach (var o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        // --- Helpers -------------------------------------------------------------

        private SpellWord MakeWord(string word, SpellId id, SpellId misfire, params string[] alts)
        {
            var sw = ScriptableObject.CreateInstance<SpellWord>();
            sw.Word = word;
            sw.spellId = id;
            sw.misfireId = misfire;
            sw.AltPronunciations = alts;
            _spawned.Add(sw);
            return sw;
        }

        private SpellLexicon BuildTestLexicon()
        {
            var lex = ScriptableObject.CreateInstance<SpellLexicon>();
            lex.MaxNearMatchDistance = 2;
            lex.Spells = new List<SpellWord>
            {
                MakeWord("IGNIS", SpellId.Ignis, SpellId.MisfireIgnis, "AGNIS", "IGNISH"),
                MakeWord("FRANGO", SpellId.Frango, SpellId.MisFireFrango, "FRANCO", "FRANGGO"),
                MakeWord("LEVO", SpellId.Levo, SpellId.MisfireLevo, "LAVO"),
                MakeWord("TONITRUS", SpellId.Tonitrus, SpellId.MisfireTonitrus, "TONITUS", "TONITRIS"),
                MakeWord("PORTA", SpellId.Porta, SpellId.MisfirePorta, "PROTA", "PORTA A"),
            };
            _spawned.Add(lex);
            return lex;
        }

        private MockVoiceInputService MakeMock()
        {
            var go = new GameObject("MockVoiceTest");
            _spawned.Add(go);
            return go.AddComponent<MockVoiceInputService>();
        }

        // --- Tests ---------------------------------------------------------------

        [Test]
        public void Test_MockServiceFiresOnKeyPress()
        {
            var mock = MakeMock();
            var lexicon = BuildTestLexicon();

            VoiceRecognitionResult? captured = null;
            mock.OnPhraseRecognized += r => captured = r;

            mock.StartListening();
            mock.SimulateKeyPress(KeyCode.Alpha1); // -> "IGNIS"

            Assert.IsTrue(captured.HasValue, "OnPhraseRecognized did not fire.");
            Assert.AreEqual("IGNIS", captured.Value.NormalizedText);

            SpellId resolved = MisfireEngine.Resolve(captured.Value, lexicon);
            Assert.AreEqual(SpellId.Ignis, resolved);
        }

        [Test]
        public void Test_MockServiceDoesNotFireWhenNotListening()
        {
            var mock = MakeMock();
            bool fired = false;
            mock.OnPhraseRecognized += _ => fired = true;

            // Not calling StartListening()
            mock.SimulateKeyPress(KeyCode.Alpha1);

            Assert.IsFalse(fired, "Phrase fired while not listening.");
        }

        [Test]
        public void Test_MisfireEngineExactMatch()
        {
            var lexicon = BuildTestLexicon();
            Assert.AreEqual(SpellId.Ignis, MisfireEngine.Resolve("IGNIS", lexicon));
        }

        [Test]
        public void Test_MisfireEngineNearMatch()
        {
            var lexicon = BuildTestLexicon();
            Assert.AreEqual(SpellId.MisfireIgnis, MisfireEngine.Resolve("AGNIS", lexicon));
        }

        [Test]
        public void Test_MisfireEngineNoMatch()
        {
            var lexicon = BuildTestLexicon();
            Assert.AreEqual(SpellId.None, MisfireEngine.Resolve("XYZABC", lexicon));
        }

        [Test]
        public void Test_AmplitudeClassification()
        {
            Assert.AreEqual(CastVolume.Whisper, VoiceUtility.ClassifyVolume(0.05f));
            Assert.AreEqual(CastVolume.Normal, VoiceUtility.ClassifyVolume(0.2f));
            Assert.AreEqual(CastVolume.Shout, VoiceUtility.ClassifyVolume(0.6f));
        }

        [Test]
        public void Test_LevenshteinDistance()
        {
            Assert.AreEqual(1, MisfireEngine.LevenshteinDistance("IGNIS", "AGNIS"));
            Assert.AreEqual(1, MisfireEngine.LevenshteinDistance("FRANGO", "FRANCO"));
            Assert.AreEqual(0, MisfireEngine.LevenshteinDistance("PORTA", "PORTA"));
            Assert.AreEqual(5, MisfireEngine.LevenshteinDistance("", "PORTA"));
        }

        [Test]
        public void Test_NormalizeStripsPunctuationAndCase()
        {
            Assert.AreEqual("AURUM VOCO", VoiceUtility.Normalize("  aurum, voco!  "));
            Assert.AreEqual("IGNIS", VoiceUtility.Normalize("ignis."));
        }
    }
}
