using System;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Audio.VoiceBank;

namespace Plunderspell.Tests.Editor
{
    public class VoiceDisguiseTests
    {
        private const int Rate = 16000;

        private static float[] Sine(float hz, float seconds)
        {
            var samples = new float[(int)(seconds * Rate)];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = 0.6f * (float)Math.Sin(2 * Math.PI * hz * i / Rate);
            return samples;
        }

        private static float DominantHz(float[] samples)
        {
            int from = samples.Length / 4;
            int to = samples.Length * 3 / 4;
            int crossings = 0;
            for (int i = from + 1; i < to; i++)
            {
                if ((samples[i - 1] < 0f) != (samples[i] < 0f))
                    crossings++;
            }
            return crossings / 2f / ((to - from) / (float)Rate);
        }

        private static DisguiseProfile Plain(float pitch, float speed)
        {
            return new DisguiseProfile(pitch, speed, 5000f, 0f);
        }

        [TestCase(0.7f, 140f)]
        [TestCase(1.4f, 280f)]
        public void PitchMovesTheDominantFrequencyAndKeepsTheDuration(float ratio, float expectedHz)
        {
            float[] output = VoiceDisguise.Render(Sine(200f, 1f), Rate, Plain(ratio, 1f));

            Assert.AreEqual(expectedHz, DominantHz(output), expectedHz * 0.04f);
            Assert.AreEqual(1f, output.Length / (float)Rate, 0.05f);
        }

        [Test]
        public void SpeedShortensTheClip()
        {
            float[] output = VoiceDisguise.Render(Sine(200f, 1f), Rate, Plain(1.3f, 1.1f));

            Assert.AreEqual(1f / 1.1f, output.Length / (float)Rate, 0.05f);
        }

        [Test]
        public void OutputIsFinitePeakLimitedAndDeterministic()
        {
            float[] input = Sine(180f, 0.8f);
            var profile = new DisguiseProfile(0.8f, 0.95f, 2500f, 0.35f);

            float[] first = VoiceDisguise.Render(input, Rate, profile);
            float[] second = VoiceDisguise.Render(input, Rate, profile);

            CollectionAssert.AreEqual(first, second);
            foreach (float sample in first)
            {
                Assert.IsFalse(float.IsNaN(sample) || float.IsInfinity(sample));
                Assert.LessOrEqual(Math.Abs(sample), 0.95f + 1e-4f);
            }
        }

        [Test]
        public void EmptyAndTinyInputDoNotThrow()
        {
            Assert.AreEqual(0, VoiceDisguise.Render(new float[0], Rate, Plain(0.8f, 1f)).Length);
            Assert.DoesNotThrow(() => VoiceDisguise.Render(new float[] { 0.5f }, Rate, Plain(1.4f, 1.1f)));
            Assert.DoesNotThrow(() => VoiceDisguise.Render(new float[50], Rate, Plain(0.7f, 0.9f)));
        }
    }

    public class DisguiseProfileTests
    {
        [Test]
        public void SameSeedGivesSameProfile()
        {
            DisguiseProfile a = DisguiseProfile.For(42, 100f);
            DisguiseProfile b = DisguiseProfile.For(42, 100f);

            Assert.AreEqual(a.PitchRatio, b.PitchRatio);
            Assert.AreEqual(a.Speed, b.Speed);
            Assert.AreEqual(a.BrightnessHz, b.BrightnessHz);
            Assert.AreEqual(a.Roughness, b.Roughness);
        }

        [Test]
        public void DifferentSeedsGiveDifferentPitches()
        {
            var ratios = new HashSet<float>();
            for (int seed = 0; seed <= 15; seed++)
                ratios.Add(DisguiseProfile.For(seed, 120f).PitchRatio);

            Assert.GreaterOrEqual(ratios.Count, 5);
        }

        [Test]
        public void EveryFieldStaysInsideItsRange()
        {
            for (int seed = 0; seed <= 500; seed++)
            {
                for (float hz = 48f; hz <= 150f; hz += 17f)
                {
                    DisguiseProfile p = DisguiseProfile.For(seed, hz);
                    Assert.IsFalse(p.PitchRatio > 0.85f && p.PitchRatio < 1.15f, $"seed {seed} hz {hz} pitch {p.PitchRatio}");
                    Assert.GreaterOrEqual(p.PitchRatio, 0.6f);
                    Assert.LessOrEqual(p.PitchRatio, 1.6f);
                    Assert.That(p.Speed, Is.InRange(0.88f, 1.12f));
                    Assert.That(p.BrightnessHz, Is.InRange(2400f, 5200f));
                    Assert.That(p.Roughness, Is.InRange(0f, 0.35f));
                }
            }
        }
    }
}
