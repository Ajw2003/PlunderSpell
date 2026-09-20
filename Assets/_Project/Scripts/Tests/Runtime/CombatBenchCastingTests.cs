#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Plunderspell.Core;
using RogueAi.Spells;
using RogueAi.Spells.Vfx;
using RogueAi.Voice;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>The bench mirror of <see cref="RaidSceneCastingTests"/>. See
    /// docs/systems/combat-bench.md, "The bench carries its own spell VFX".</summary>
    public class CombatBenchCastingTests
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/CombatBench.unity";

        private GameState m_stateBeforeTest;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                k_ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            GameServices.Initialize();
            m_stateBeforeTest = GameServices.GameState.CurrentState;
        }

        [UnityTearDown]
        public IEnumerator TearDownScene()
        {
            if (GameServices.GameState != null)
            {
                GameServices.GameState.ChangeState(m_stateBeforeTest);
            }

            VoiceServiceLocator.Clear();

            Scene bench = SceneManager.GetSceneByPath(k_ScenePath);
            if (bench.IsValid() && bench.isLoaded)
            {
                Scene empty = SceneManager.CreateScene($"AfterCombatBenchCastingTest_{Time.frameCount}");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(bench);
            }

            yield return null;
        }

        /// <summary>
        /// The bench puts itself into Playing on Start (see docs/systems/combat-bench.md), so unlike
        /// the raid scene this does not need an explicit state change first — proving that stays true
        /// is part of what this test covers.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_CastingOnTheBenchProducesAVisual()
        {
            yield return null;

            var casting = Object.FindFirstObjectByType<SpellCastingSystem>();
            Assert.IsNotNull(casting, $"{k_ScenePath} has no SpellCastingSystem on its player.");

            var director = Object.FindFirstObjectByType<SpellVfxDirector>();
            Assert.IsNotNull(director, $"{k_ScenePath} has no SpellVfxDirector.");

            Assert.IsTrue(GameServices.IsPlaying,
                "The bench must put itself into play on its own; casting gates on this.");

            IVoiceInputService voice = VoiceServiceLocator.Current;
            Assert.IsNotNull(voice, "No voice provider; nothing can ever be cast.");
            Assert.IsInstanceOf<MockVoiceInputService>(voice,
                "The editor must use the keyboard mock, or casting needs a microphone to test.");

            int before = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;

            // Exactly what holding V and tapping 5 does.
            voice.StartListening();
            ((MockVoiceInputService)voice).SimulateKeyPress(KeyCode.Alpha5);
            voice.StopListening();

            yield return null;

            int after = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;
            Assert.Greater(after, before,
                "Casting Tonitrus on the bench produced no visible effect.");
        }

        /// <summary>
        /// The bench and the raid must resolve every primary word to the same spell: one lexicon,
        /// wired into both players by the same path, not two configurations that could drift apart.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_TheBenchsLexiconResolvesEveryPrimaryWord()
        {
            yield return null;

            var casting = Object.FindFirstObjectByType<SpellCastingSystem>();
            Assert.IsNotNull(casting);

            SpellId lastResolved = SpellId.None;
            void Record(SpellCastingSystem.CastReport report) => lastResolved = report.Spell;

            SpellCastingSystem.CastResolved += Record;
            try
            {
                IVoiceInputService voice = VoiceServiceLocator.Current;
                var mock = (MockVoiceInputService)voice;

                foreach (KeyCode key in new[]
                         {
                             KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
                             KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8,
                         })
                {
                    lastResolved = SpellId.None;

                    voice.StartListening();
                    mock.SimulateKeyPress(key);
                    voice.StopListening();

                    yield return null;

                    Assert.AreNotEqual(SpellId.None, lastResolved,
                        $"{key} fizzled: the bench's lexicon does not resolve that word.");
                }
            }
            finally
            {
                SpellCastingSystem.CastResolved -= Record;
            }
        }
    }
}
#endif
