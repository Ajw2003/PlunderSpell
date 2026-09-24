using System.Collections;
using NUnit.Framework;
using Plunderspell.Core;
using RogueAi.Spells;
using RogueAi.Spells.Vfx;
using RogueAi.Voice;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>
    /// Presses the actual keys. Every other casting test drives the pipeline from the middle, which
    /// proves the spell system works and says nothing about whether the keyboard reaches it — and
    /// the keyboard is where casting was failing.
    ///
    /// See docs/systems/spells.md, "Two ways to cast".
    /// </summary>
    public class CastingInputTests : InputTestFixture
    {
        private Keyboard m_keyboard;
        private GameObject m_player;
        private GameObject m_vfx;
        private GameState m_stateBeforeTest;

        public override void Setup()
        {
            base.Setup();
            m_keyboard = InputSystem.AddDevice<Keyboard>();

            GameServices.Initialize();
            m_stateBeforeTest = GameServices.GameState.CurrentState;
            GameServices.GameState.ChangeState(GameState.Playing);

            m_player = new GameObject("Caster");
            m_player.AddComponent<PushToCastController>();

            SpellCastingSystem casting = m_player.AddComponent<SpellCastingSystem>();
            casting.SetLexicon(LoadLexicon());

            m_vfx = new GameObject("SpellVfx");
            m_vfx.AddComponent<SpellVfxDirector>();
        }

        public override void TearDown()
        {
            if (GameServices.GameState != null)
            {
                GameServices.GameState.ChangeState(m_stateBeforeTest);
            }

            if (m_player != null) Object.DestroyImmediate(m_player);
            if (m_vfx != null) Object.DestroyImmediate(m_vfx);

            foreach (SpellBurst burst in Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(burst.gameObject);
            }

            VoiceServiceLocator.Clear();
            base.TearDown();
        }

        /// <summary>The lexicon the raid ships, not one built here — a test lexicon would pass while
        /// the authored one fizzled.</summary>
        private static SpellLexicon LoadLexicon()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<SpellLexicon>(
                "Assets/_Project/Data/Spells/SpellLexicon.asset");
#else
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator Test_HoldingVOpensTheMic()
        {
            var pushToCast = m_player.GetComponent<PushToCastController>();
            yield return null;

            Assert.IsFalse(pushToCast.IsCasting, "The mic must start closed.");

            Press(m_keyboard.vKey);
            yield return null;

            Assert.IsTrue(pushToCast.IsCasting,
                "Holding V must open the mic. If this fails the key never reaches the controller.");

            Release(m_keyboard.vKey);
            yield return null;

            Assert.IsFalse(pushToCast.IsCasting, "Releasing V must close the mic.");
        }

        /// <summary>
        /// The whole thing, from keystroke to something on screen: hold V, tap 5, release.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_HoldVAndPressFiveCastsTonitrusAndShowsIt()
        {
            SpellId resolved = SpellId.None;
            void Record(SpellCastingSystem.CastReport report) => resolved = report.Spell;
            SpellCastingSystem.CastResolved += Record;

            try
            {
                int before = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;

                Press(m_keyboard.vKey);
                yield return null;

                Press(m_keyboard.digit5Key);
                yield return null;

                Release(m_keyboard.digit5Key);
                Release(m_keyboard.vKey);
                yield return null;

                Assert.AreEqual(SpellId.Tonitrus, resolved,
                    "Holding V and pressing 5 must resolve to Tonitrus.");

                int after = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;
                Assert.Greater(after, before, "…and it must put something on screen.");
            }
            finally
            {
                SpellCastingSystem.CastResolved -= Record;
            }
        }

        /// <summary>
        /// The trap that makes casting look broken: the number keys do nothing unless the mic is
        /// open, so a player who taps 5 without holding V sees no spell, no error and no log line.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_PressingFiveWithoutHoldingVDoesNothing()
        {
            SpellId resolved = SpellId.None;
            void Record(SpellCastingSystem.CastReport report) => resolved = report.Spell;
            SpellCastingSystem.CastResolved += Record;

            try
            {
                Press(m_keyboard.digit5Key);
                yield return null;
                Release(m_keyboard.digit5Key);
                yield return null;

                Assert.AreEqual(SpellId.None, resolved,
                    "Push-to-cast means the mic has to be open first.");
            }
            finally
            {
                SpellCastingSystem.CastResolved -= Record;
            }
        }

        /// <summary>Casting must not fire while a menu is up, matching the input gate on movement.</summary>
        [UnityTest]
        public IEnumerator Test_TheMicStaysShutWhileAMenuIsOpen()
        {
            GameServices.GameState.ChangeState(GameState.Paused);
            var pushToCast = m_player.GetComponent<PushToCastController>();
            yield return null;

            Press(m_keyboard.vKey);
            yield return null;

            Assert.IsFalse(pushToCast.IsCasting,
                "Holding V behind a pause menu must not open the mic.");

            Release(m_keyboard.vKey);
        }
    }
}
