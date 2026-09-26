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
    /// <summary>
    /// Drives the real raid scene the way a player does, rather than a fixture built in a test.
    /// If casting works here it works in the game, which is the only claim worth making.
    ///
    /// See docs/systems/spells.md, "Casting it in the Editor".
    /// </summary>
    public class RaidSceneCastingTests
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/RaidScene.unity";

        private GameState m_stateBeforeTest;
        private string m_sessionReport;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // The real scene asset, loaded in play mode: this is the thing that ships, not a
            // fixture assembled here that could agree with the code while the scene does not.
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                k_ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            GameServices.Initialize();
            m_stateBeforeTest = GameServices.GameState.CurrentState;
            CastKeysInstantly();

            // The player is spawned by the network, solo included, exactly as Play Solo does it.
            Assert.IsNotNull(GameServices.Coop, $"{k_ScenePath} has no co-op session to start.");
            GameServices.Coop.PlaySolo();
            for (int frame = 0; frame < 120 && Object.FindFirstObjectByType<SpellCastingSystem>() == null; frame++)
                yield return null;
            var manager = PurrNet.NetworkManager.main;
            m_sessionReport = $"session: {GameServices.Coop.Status} inSession={GameServices.Coop.IsInSession} " +
                              $"server={(manager != null && manager.isServer)} client={(manager != null && manager.isClient)}";
        }

        /// <summary>
        /// Unloads the raid scene. Without this it stays loaded and active for the rest of the run,
        /// and every later test that asserts "nothing is in range" finds a castle full of guards and
        /// loot instead -- which is how this suite silently broke six spell-effect tests.
        /// </summary>

        /// <summary>These tests are about words resolving and showing, not the keyboard chant or
        /// mana (CastingInputTests covers both), so keyed casts fire at once here.</summary>
        private static void CastKeysInstantly()
        {
            var tuning = ScriptableObject.CreateInstance<SpellTuningProfile>();
            tuning.KeyboardCastSeconds = 0f;
            SpellTuning.Use(tuning);
        }

        [UnityTearDown]
        public IEnumerator TearDownScene()
        {
            SpellTuning.Use(null);
            if (GameServices.GameState != null)
            {
                GameServices.GameState.ChangeState(m_stateBeforeTest);
            }

            VoiceServiceLocator.Clear();
            GameServices.Coop?.Leave();
            yield return null;

            Scene raid = SceneManager.GetSceneByPath(k_ScenePath);
            if (raid.IsValid() && raid.isLoaded)
            {
                Scene empty = SceneManager.CreateScene($"AfterRaidSceneTest_{Time.frameCount}");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(raid);
            }

            yield return null;
        }

        /// <summary>
        /// The whole production chain: the scene's own player, its own casting system and lexicon,
        /// the mock voice provider the editor always uses, and the scene's own VFX director.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_CastingInTheRaidSceneProducesAVisual()
        {
            GameServices.GameState.ChangeState(GameState.Playing);
            yield return null;

            var casting = Object.FindFirstObjectByType<SpellCastingSystem>();
            Assert.IsNotNull(casting, $"{k_ScenePath} has no SpellCastingSystem on its player ({m_sessionReport}).");

            var director = Object.FindFirstObjectByType<SpellVfxDirector>();
            Assert.IsNotNull(director, $"{k_ScenePath} has no SpellVfxDirector.");

            IVoiceInputService voice = VoiceServiceLocator.Current;
            Assert.IsNotNull(voice, "No voice provider; nothing can ever be cast.");
            Assert.IsNotNull(VoiceServiceLocator.Keyboard,
                "Number-key casting must always be available, or casting needs a microphone to test.");

            int before = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;

            // Exactly what holding V and tapping 5 does.
            voice.StartListening();
            VoiceServiceLocator.Keyboard.SimulateKeyPress(KeyCode.Alpha5);
            voice.StopListening();

            // Round trip through the server: allow a few network ticks.
            float deadline = Time.realtimeSinceStartup + 0.5f;
            while (Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length <= before
                   && Time.realtimeSinceStartup < deadline)
                yield return null;

            int after = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;
            Assert.Greater(after, before,
                "Casting Tonitrus in the raid scene produced no visible effect.");
        }

        /// <summary>
        /// The phrase has to resolve to a real spell. A missing or wrong lexicon fizzles everything
        /// to None, which looks exactly like the visuals being broken.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_TheRaidScenesLexiconResolvesEveryPrimaryWord()
        {
            GameServices.GameState.ChangeState(GameState.Playing);
            yield return null;

            var casting = Object.FindFirstObjectByType<SpellCastingSystem>();
            Assert.IsNotNull(casting);

            SpellId lastResolved = SpellId.None;
            void Record(SpellCastingSystem.CastReport report) => lastResolved = report.Spell;

            SpellCastingSystem.CastResolved += Record;
            try
            {
                IVoiceInputService voice = VoiceServiceLocator.Current;
                var mock = VoiceServiceLocator.Keyboard;

                foreach (KeyCode key in new[]
                         {
                             KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
                             KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8,
                         })
                {
                    lastResolved = SpellId.None;

                    GameServices.PlayerStats.RefillMana();
                    voice.StartListening();
                    mock.SimulateKeyPress(key);
                    voice.StopListening();

                    // The cast goes to the server and comes back as a network message, which lands
                    // on the next network tick rather than the next frame.
                    float deadline = Time.realtimeSinceStartup + 0.5f;
                    while (lastResolved == SpellId.None && Time.realtimeSinceStartup < deadline)
                        yield return null;

                    Assert.AreNotEqual(SpellId.None, lastResolved,
                        $"{key} fizzled: the scene's lexicon does not resolve that word.");
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
