using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Plunderspell.Audio.VoiceBank;

namespace Plunderspell.Tests.Editor
{
    public class WavCodecTests
    {
        [Test]
        public void RoundTripKeepsLengthRateAndPeak()
        {
            var samples = new float[4000];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = 0.8f * (float)Math.Sin(2 * Math.PI * 220 * i / 16000);

            float[] decoded = WavCodec.Decode(WavCodec.Encode(samples, 16000), out int rate);

            Assert.AreEqual(16000, rate);
            Assert.AreEqual(samples.Length, decoded.Length);
            for (int i = 0; i < samples.Length; i++)
                Assert.AreEqual(samples[i], decoded[i], 2f / 32768f);
        }

        [Test]
        public void DecodesFloatStereoAt48kToMono()
        {
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(0);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((ushort)3);
                writer.Write((ushort)2);
                writer.Write(48000);
                writer.Write(48000 * 8);
                writer.Write((ushort)8);
                writer.Write((ushort)32);
                writer.Write(Encoding.ASCII.GetBytes("LIST"));
                writer.Write(3);
                writer.Write(new byte[] { 1, 2, 3, 0 });
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(32);
                writer.Write(0.5f);
                writer.Write(-0.5f);
                writer.Write(1f);
                writer.Write(0.5f);
                writer.Write(0.25f);
                writer.Write(0.25f);
                writer.Write(-1f);
                writer.Write(0f);
            }

            float[] decoded = WavCodec.Decode(stream.ToArray(), out int rate);

            Assert.AreEqual(48000, rate);
            CollectionAssert.AreEqual(new[] { 0f, 0.75f, 0.25f, -0.5f }, decoded);
        }

        [Test]
        public void GarbageBytesThrow()
        {
            Assert.Throws<InvalidDataException>(() => WavCodec.Decode(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }, out _));
            Assert.Throws<InvalidDataException>(() => WavCodec.Decode(new byte[3], out _));
        }

        [Test]
        public void UnsupportedBitDepthThrows()
        {
            byte[] wav = WavCodec.Encode(new float[10], 16000);
            wav[34] = 24;

            Assert.Throws<InvalidDataException>(() => WavCodec.Decode(wav, out _));
        }
    }
}
