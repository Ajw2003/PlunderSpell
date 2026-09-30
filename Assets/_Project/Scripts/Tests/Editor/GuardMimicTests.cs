using NUnit.Framework;
using Plunderspell.Audio.Mimic;
using Plunderspell.Voice;

namespace Plunderspell.Tests.Editor
{
    /// <summary>Guard mimic prototype: cutting words out of a recording, and stitching them back together.</summary>
    public class GuardMimicTests
    {
        private const int Rate = ChatterReport.SampleRate;

        // A recording of silence with a constant level where each word is, so a cut can be checked.
        private static float[] Recording(float seconds, params (float start, float end, float level)[] words)
        {
            var samples = new float[(int)(seconds * Rate)];
            foreach (var (start, end, level) in words)
                for (int i = (int)(start * Rate); i < (int)(end * Rate); i++)
                    samples[i] = level;
            return samples;
        }

        [Test]
        public void Test_EachConfidentWordIsCutFromTheRecording()
        {
            float[] samples = Recording(2f, (0.2f, 0.6f, 0.5f), (0.8f, 1.2f, 0.25f), (1.4f, 1.5f, 0.1f));
            var report = new ChatterReport("where is gold", 0.2f, CastVolume.Normal,
                new[]
                {
                    new WordTiming("Where", 0.2f, 0.6f, 1f),
                    new WordTiming("is", 0.8f, 1.2f, 0.3f), // not sure enough
                    new WordTiming("gold", 1.4f, 1.5f, 1f), // too short once padded? 0.16 s: kept
                },
                samples);

            var bank = new MimicWordBank();
            Assert.AreEqual(2, bank.Add(report));
            Assert.IsTrue(bank.Has("where"), "Words are kept in lower case.");
            Assert.IsFalse(bank.Has("is"), "A word heard with confidence under 0.5 is not kept.");

            float[] clip = bank.Pick("where", new System.Random(1));
            Assert.AreEqual(0.46f * Rate, clip.Length, 2f, "The clip is the word plus 30 ms either side.");
            Assert.AreEqual(0.5f, clip[clip.Length / 2], 1e-5f, "The middle of the clip is the word itself.");
            Assert.AreEqual(0f, clip[0], 1e-5f, "The clip fades in from silence.");
        }

        [Test]
        public void Test_ABankKeepsTheNewestFourClipsOfAWord()
        {
            var bank = new MimicWordBank();
            for (int i = 0; i < 6; i++)
                bank.AddClip("gold", new float[] { i });
            var random = new System.Random(3);
            for (int n = 0; n < 50; n++)
                Assert.GreaterOrEqual(bank.Pick("gold", random)[0], 2f, "The two oldest clips were dropped.");
        }

        [Test]
        public void Test_StitchingJoinsTheWordsAtRandomPitches()
        {
            var bank = new MimicWordBank();
            bank.AddClip("where", new float[Rate / 2]);
            bank.AddClip("gold", new float[Rate / 2]);

            float[] line = MimicStitcher.Stitch(new[] { "where", "gold", "nothing" }, bank, Rate, new System.Random(7));
            float shortest = 2 * (Rate / 2) / MimicStitcher.MaxPitch;
            float longest = 2 * (Rate / 2) / MimicStitcher.MinPitch + 0.12f * Rate;
            Assert.That(line.Length, Is.InRange(shortest - 0.03f * Rate, longest), "Two words, re-pitched, one short gap; the unbanked word is skipped.");
        }

        [Test]
        public void Test_ResamplingChangesLengthByThePitch()
        {
            Assert.AreEqual(800, MimicStitcher.Resample(new float[1000], 1.25f).Length);
            Assert.AreEqual(2000, MimicStitcher.Resample(new float[1000], 0.5f).Length);
        }
    }
}
