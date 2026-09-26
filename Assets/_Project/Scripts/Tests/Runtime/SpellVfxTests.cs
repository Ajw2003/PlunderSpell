using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Spells;
using Plunderspell.Spells.Vfx;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// A cast has to be visible. See docs/4-systems/spells.md, "Seeing a cast".
    /// </summary>
    public class SpellVfxTests
    {
        private readonly List<Object> m_tracked = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in m_tracked)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            m_tracked.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            m_tracked.Add(o);
            return o;
        }

        [Test]
        public void Test_EveryPrimarySpellHasItsOwnLook()
        {
            SpellId[] primaries =
            {
                SpellId.Ignis, SpellId.Frango, SpellId.Levo, SpellId.AurumVoco,
                SpellId.Tonitrus, SpellId.Somnus, SpellId.CadaverSurge, SpellId.Porta,
            };

            var seen = new List<Color>();
            foreach (SpellId spell in primaries)
            {
                SpellLook look = SpellLookbook.For(spell);

                Assert.AreNotEqual(Color.white, look.Colour,
                    $"{spell} falls through to the default look, so it is indistinguishable.");
                Assert.Greater(look.Radius, 0f, $"{spell} has no visible size.");

                foreach (Color other in seen)
                {
                    Assert.AreNotEqual(other, look.Colour,
                        $"{spell} shares a colour with another spell; they read as the same thing.");
                }

                seen.Add(look.Colour);
            }
        }

        /// <summary>
        /// Whatever a misfire was meant to be, it has to read as gone wrong before the player has
        /// read the caption.
        /// </summary>
        [Test]
        public void Test_EveryMisfireLooksLikeAMisfire()
        {
            SpellId[] misfires =
            {
                SpellId.MisfireIgnis, SpellId.MisFireFrango, SpellId.MisfireLevo,
                SpellId.MisfireTonitrus, SpellId.MisFireSomnus, SpellId.MisFireCadaverSurge,
                SpellId.MisfireAurumVoco, SpellId.MisfirePorta,
            };

            foreach (SpellId spell in misfires)
            {
                Assert.AreEqual(SpellLookbook.Misfire.Colour, SpellLookbook.For(spell).Colour,
                    $"{spell} must read as a misfire, not as the spell it was aimed at.");
            }
        }

        [Test]
        public void Test_IgnisIsTheProjectileSpell()
        {
            Assert.AreEqual(SpellVisualStyle.Bolt, SpellLookbook.For(SpellId.Ignis).Style,
                "Ignis is the fire bolt; it should fly rather than bloom at the hands.");
            Assert.AreEqual(SpellVisualStyle.Burst, SpellLookbook.For(SpellId.Tonitrus).Style,
                "A thunderclap happens where you are standing.");
        }

        [UnityTest]
        public IEnumerator Test_ABurstAppearsAndThenCleansItselfUp()
        {
            SpellBurst burst = SpellBurst.Spawn(Vector3.zero, Color.red, 2f, 0.1f);
            Track(burst.gameObject);

            Assert.IsNotNull(burst, "The burst must exist the moment it is spawned.");
            // Destroy is deferred to the end of the frame, so the honest assertion is that nothing
            // on it can collide with anything, not that the component has already gone.
            var collider = burst.GetComponent<Collider>();
            Assert.IsTrue(collider == null || !collider.enabled,
                "A burst is light, not matter; a live collider would shove the loot around.");

            float waited = 0f;
            while (burst != null && waited < 2f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(burst == null,
                "A burst must destroy itself, or every cast leaks a GameObject.");
        }

        [UnityTest]
        public IEnumerator Test_ACastPutsSomethingOnScreen()
        {
            var go = Track(new GameObject("SpellVfx"));
            go.AddComponent<SpellVfxDirector>();
            yield return null;

            int before = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;

            SpellCastingSystem.AnnounceForTesting(new SpellCastingSystem.CastReport(
                SpellId.Tonitrus, Plunderspell.Voice.CastVolume.Normal, 1, "Tester",
                Vector3.zero, Vector3.forward));

            int after = Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None).Length;

            Assert.Greater(after, before,
                "Resolving a cast must spawn something visible; that is the whole point.");

            foreach (SpellBurst burst in Object.FindObjectsByType<SpellBurst>(FindObjectsSortMode.None))
            {
                Track(burst.gameObject);
            }
        }
    }
}
