using System;
using System.IO;
using System.Text;

namespace Plunderspell.Audio.VoiceBank
{
    /// <summary>Reads and writes the small subset of WAV the clip bank needs, and refuses anything else loudly.</summary>
    public static class WavCodec
    {
        public const int HeaderBytes = 44;

        private const ushort FormatPcm = 1;
        private const ushort FormatFloat = 3;
        private const ushort FormatExtensible = 0xFFFE;

        public static byte[] Encode(float[] samples, int sampleRate)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));

            int dataBytes = samples.Length * 2;
            var stream = new MemoryStream(HeaderBytes + dataBytes);
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write(FormatPcm);
                writer.Write((ushort)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((ushort)2);
                writer.Write((ushort)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);
                for (int i = 0; i < samples.Length; i++)
                {
                    float clamped = Math.Max(-1f, Math.Min(1f, samples[i]));
                    writer.Write((short)Math.Round(clamped * 32767f));
                }
            }
            return stream.ToArray();
        }

        public static float[] Decode(byte[] wav, out int sampleRate)
        {
            if (wav == null) throw new ArgumentNullException(nameof(wav));
            if (wav.Length < 12 || Tag(wav, 0) != "RIFF" || Tag(wav, 8) != "WAVE")
                throw new InvalidDataException("Not a WAV file: missing RIFF/WAVE header.");

            ushort format = 0;
            int channels = 0;
            int rate = 0;
            int bits = 0;
            bool haveFormat = false;

            int pos = 12;
            while (pos + 8 <= wav.Length)
            {
                string id = Tag(wav, pos);
                long size = BitConverter.ToUInt32(wav, pos + 4);
                int body = pos + 8;

                if (id == "fmt ")
                {
                    if (size < 16 || body + 16 > wav.Length)
                        throw new InvalidDataException("WAV fmt chunk is truncated.");
                    format = BitConverter.ToUInt16(wav, body);
                    channels = BitConverter.ToUInt16(wav, body + 2);
                    rate = BitConverter.ToInt32(wav, body + 4);
                    bits = BitConverter.ToUInt16(wav, body + 14);
                    if (format == FormatExtensible)
                    {
                        if (size < 26 || body + 26 > wav.Length)
                            throw new InvalidDataException("WAV extensible fmt chunk is truncated.");
                        format = BitConverter.ToUInt16(wav, body + 24);
                    }
                    haveFormat = true;
                }
                else if (id == "data")
                {
                    if (!haveFormat)
                        throw new InvalidDataException("WAV data chunk appears before the fmt chunk.");
                    if (body + size > wav.Length)
                        throw new InvalidDataException("WAV data chunk is longer than the file.");
                    ValidateFormat(format, channels, rate, bits);
                    sampleRate = rate;
                    return ReadSamples(wav, body, (int)size, format, channels, bits);
                }

                pos = (int)Math.Min(int.MaxValue, body + size + (size & 1));
            }

            throw new InvalidDataException("WAV file has no data chunk.");
        }

        private static void ValidateFormat(ushort format, int channels, int rate, int bits)
        {
            bool supported = (format == FormatPcm && bits == 16) || (format == FormatFloat && bits == 32);
            if (!supported)
                throw new InvalidDataException($"Unsupported WAV encoding (format {format}, {bits}-bit); only 16-bit PCM and 32-bit float are read.");
            if (channels != 1 && channels != 2)
                throw new InvalidDataException($"Unsupported WAV channel count {channels}; only mono and stereo are read.");
            if (rate <= 0)
                throw new InvalidDataException($"Invalid WAV sample rate {rate}.");
        }

        private static float[] ReadSamples(byte[] wav, int start, int size, ushort format, int channels, int bits)
        {
            int bytesPerSample = bits / 8;
            int frames = size / (bytesPerSample * channels);
            var result = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                {
                    int at = start + (i * channels + c) * bytesPerSample;
                    sum += format == FormatFloat ? BitConverter.ToSingle(wav, at) : BitConverter.ToInt16(wav, at) / 32768f;
                }
                result[i] = sum / channels;
            }
            return result;
        }

        private static string Tag(byte[] bytes, int at)
        {
            return Encoding.ASCII.GetString(bytes, at, 4);
        }
    }
}
