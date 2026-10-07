using System;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Audio.VoiceBank;

namespace Plunderspell.Tests.Editor
{
    /// <summary>Cutting a mic stream into clips at pauses.</summary>
    public class PauseSegmenterTests
    {
        private const int Rate = PauseSegmenter.SampleRate;

        private static float[] Silence(float seconds)
        {
            return new float[(int)(seconds * Rate)];
        }

        private static float[] Tone(float seconds, float amplitude = 0.5f)
        {
            var samples = new float[(int)(seconds * Rate)];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = amplitude * (float)Math.Sin(2 * Math.PI * 300 * i / Rate);
            return samples;
        }

        private static float[] Join(params float[][] parts)
        {
            var all = new List<float>();
            foreach (float[] part in parts)
                all.AddRange(part);
            return all.ToArray();
        }

        private static List<Chunk> Run(float[] signal, int block)
        {
            var chunks = new List<Chunk>();
            var segmenter = new PauseSegmenter();
            segmenter.ChunkReady += chunks.Add;
            var buffer = new float[block];
            for (int at = 0; at < signal.Length; at += block)
            {
                int count = Math.Min(block, signal.Length - at);
                Array.Copy(signal, at, buffer, 0, count);
                segmenter.Feed(buffer, count);
            }
            return chunks;
        }

        [Test]
        public void ToneBurstsBetweenSilenceGiveOneChunkEach()
        {
            float[] signal = Join(Silence(0.5f), Tone(0.5f), Silence(0.5f), Tone(0.8f), Silence(0.5f));
            List<Chunk> chunks = Run(signal, 320);

            Assert.AreEqual(2, chunks.Count);
            Assert.AreEqual(0.58f, chunks[0].Seconds, 0.04f);
            Assert.AreEqual(0.88f, chunks[1].Seconds, 0.04f);
        }

        [Test]
        public void ShortBlipIsDiscarded()
        {
            List<Chunk> chunks = Run(Join(Silence(0.5f), Tone(0.1f), Silence(0.5f)), 320);

            Assert.AreEqual(0, chunks.Count);
        }

        [Test]
        public void ContinuousToneIsCutIntoChunksNoLongerThanTheLimit()
        {
            List<Chunk> chunks = Run(Join(Silence(0.3f), Tone(4f), Silence(0.5f)), 320);

            int total = 0;
            foreach (Chunk chunk in chunks)
            {
                Assert.LessOrEqual(chunk.Samples.Length, (int)(1.5f * Rate));
                total += chunk.Samples.Length;
            }
            Assert.GreaterOrEqual(chunks.Count, 3);
            Assert.GreaterOrEqual(total, 4 * Rate - Rate / 10);
        }

        [Test]
        public void BurstsAreFoundOverConstantHiss()
        {
            var random = new Random(7);
            float[] hiss = new float[(int)(3f * Rate)];
            for (int i = 0; i < hiss.Length; i++)
                hiss[i] = (float)(random.NextDouble() * 2 - 1) * 0.0346f;

            float[] burst = Tone(0.5f, 0.3f);
            foreach (int start in new[] { (int)(0.5f * Rate), (int)(1.8f * Rate) })
            {
                for (int i = 0; i < burst.Length; i++)
                    hiss[start + i] += burst[i];
            }

            Assert.AreEqual(2, Run(hiss, 320).Count);
        }

        [Test]
        public void ResetDropsAudioInProgress()
        {
            var chunks = new List<Chunk>();
            var segmenter = new PauseSegmenter();
            segmenter.ChunkReady += chunks.Add;
            float[] start = Join(Silence(0.3f), Tone(0.5f));
            segmenter.Feed(start, start.Length);

            segmenter.Reset();
            float[] quiet = Silence(1f);
            segmenter.Feed(quiet, quiet.Length);
            segmenter.Flush();

            Assert.AreEqual(0, chunks.Count);
        }

        [Test]
        public void FlushEmitsAudioInProgress()
        {
            var chunks = new List<Chunk>();
            var segmenter = new PauseSegmenter();
            segmenter.ChunkReady += chunks.Add;
            float[] start = Join(Silence(0.3f), Tone(0.5f));
            segmenter.Feed(start, start.Length);

            segmenter.Flush();

            Assert.AreEqual(1, chunks.Count);
        }

        [Test]
        public void BlockSizeDoesNotChangeTheChunks()
        {
            float[] signal = Join(Silence(0.4f), Tone(0.6f), Silence(0.4f), Tone(2.2f), Silence(0.5f));

            List<Chunk> small = Run(signal, 160);
            List<Chunk> large = Run(signal, 1000);

            Assert.AreEqual(small.Count, large.Count);
            for (int i = 0; i < small.Count; i++)
                CollectionAssert.AreEqual(small[i].Samples, large[i].Samples);
        }
    }
}
