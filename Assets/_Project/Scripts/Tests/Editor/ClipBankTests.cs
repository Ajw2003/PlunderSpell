using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Plunderspell.Audio.VoiceBank;

namespace Plunderspell.Tests.Editor
{
    public class ClipBankTests
    {
        private string _directory;

        [SetUp]
        public void CreateTempDirectory()
        {
            _directory = Path.Combine(Path.GetTempPath(), "clipbank_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void DeleteTempDirectory()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private static Chunk MakeChunk(float peakRms, float seconds)
        {
            var samples = new float[(int)(seconds * PauseSegmenter.SampleRate)];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = 0.3f * (float)Math.Sin(2 * Math.PI * 300 * i / PauseSegmenter.SampleRate);
            return new Chunk(samples, peakRms, peakRms, seconds);
        }

        private static ClipFilter ShoutOnly(float min = 0f, float max = 10f)
        {
            return new ClipFilter(min, max, false, false, true);
        }

        [Test]
        public void AddedClipsPersistWithTheirTags()
        {
            var bank = new ClipBank(_directory);
            bank.Add(MakeChunk(0.05f, 0.3f), 2);
            bank.Add(MakeChunk(0.2f, 0.4f), 5);
            bank.Add(MakeChunk(0.6f, 0.5f), 9);

            var reloaded = new ClipBank(_directory);

            Assert.AreEqual(3, reloaded.Count);
            Assert.AreEqual(0, reloaded.Skipped.Count);
            string[] names = Directory.GetFiles(_directory).Select(Path.GetFileName).OrderBy(n => n).ToArray();
            StringAssert.EndsWith("_w_2.wav", names[0]);
            StringAssert.EndsWith("_n_5.wav", names[1]);
            StringAssert.EndsWith("_s_9.wav", names[2]);
        }

        [Test]
        public void EvictionRemovesTheOldestClips()
        {
            var bank = new ClipBank(_directory, 3);
            foreach (float seconds in new[] { 0.2f, 0.3f, 0.4f, 0.5f, 0.6f })
                bank.Add(MakeChunk(0.2f, seconds), 0);

            Assert.AreEqual(3, bank.Count);
            Assert.AreEqual(3, Directory.GetFiles(_directory).Length);
            var any = new ClipFilter(0f, 0.35f, true, true, true);
            Assert.IsFalse(bank.TryPick(any, new Random(1), out _, out _));
        }

        [Test]
        public void TryPickRespectsTheFilter()
        {
            var bank = new ClipBank(_directory);
            bank.Add(MakeChunk(0.05f, 0.3f), 0);
            bank.Add(MakeChunk(0.2f, 0.4f), 0);

            Assert.IsFalse(bank.TryPick(ShoutOnly(), new Random(1), out _, out _));

            bank.Add(MakeChunk(0.6f, 0.5f), 0);
            Assert.IsTrue(bank.TryPick(ShoutOnly(), new Random(1), out float[] samples, out int rate));
            Assert.AreEqual(PauseSegmenter.SampleRate, rate);
            Assert.AreEqual(8000, samples.Length);
            Assert.IsFalse(bank.TryPick(ShoutOnly(0.6f), new Random(1), out _, out _));
        }

        [Test]
        public void ClearRemovesOnlyClipFiles()
        {
            var bank = new ClipBank(_directory);
            bank.Add(MakeChunk(0.2f, 0.3f), 1);
            string notes = Path.Combine(_directory, "notes.txt");
            string stray = Path.Combine(_directory, "stray.wav");
            File.WriteAllText(notes, "keep");
            File.WriteAllText(stray, "keep");

            bank.Clear();

            Assert.AreEqual(0, bank.Count);
            Assert.IsTrue(File.Exists(notes));
            Assert.IsTrue(File.Exists(stray));
            Assert.AreEqual(2, Directory.GetFiles(_directory).Length);
        }

        [Test]
        public void UnparsableWavNameIsReportedAsSkipped()
        {
            Directory.CreateDirectory(_directory);
            string stray = Path.Combine(_directory, "stray.wav");
            File.WriteAllText(stray, "x");

            var bank = new ClipBank(_directory);

            Assert.AreEqual(0, bank.Count);
            CollectionAssert.AreEqual(new[] { stray }, bank.Skipped.ToArray());
        }
    }
}
