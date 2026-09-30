using System;
using System.Collections.Generic;

namespace Plunderspell.Audio.Mimic
{
    /// <summary>
    /// Guard mimic prototype: joins word clips into one line, each word at its own random pitch, with
    /// short crossfades and small gaps. The choppy "ransom note" sound is intended. Plain C#.
    /// </summary>
    public static class MimicStitcher
    {
        public const float MinPitch = 0.75f;
        public const float MaxPitch = 1.3f;
        private const float CrossfadeSeconds = 0.03f;
        private const float MinGapSeconds = 0.04f;
        private const float MaxGapSeconds = 0.12f;

        /// <summary>The words' clips, re-pitched and joined; empty when no word has a clip.</summary>
        public static float[] Stitch(IReadOnlyList<string> words, MimicWordBank bank, int rate, Random random)
        {
            var output = new List<float>();
            int crossfade = (int)(CrossfadeSeconds * rate);
            foreach (string word in words)
            {
                float[] clip = bank.Pick(word, random);
                if (clip == null)
                    continue;
                float pitch = MinPitch + (float)random.NextDouble() * (MaxPitch - MinPitch);
                float[] shifted = Resample(clip, pitch);

                if (output.Count > 0)
                {
                    int gap = (int)((MinGapSeconds + random.NextDouble() * (MaxGapSeconds - MinGapSeconds)) * rate);
                    for (int i = 0; i < gap; i++)
                        output.Add(0f);
                }

                // Equal-power crossfade into whatever tail the previous word left.
                int overlap = Math.Min(crossfade, Math.Min(output.Count, shifted.Length));
                int start = output.Count - overlap;
                for (int i = 0; i < overlap; i++)
                {
                    double t = (i + 1) / (double)(overlap + 1) * Math.PI / 2;
                    output[start + i] = (float)(output[start + i] * Math.Cos(t) + shifted[i] * Math.Sin(t));
                }
                for (int i = overlap; i < shifted.Length; i++)
                    output.Add(shifted[i]);
            }
            return output.ToArray();
        }

        /// <summary>Plays the clip <paramref name="pitch"/> times faster: higher and shorter above 1, lower and longer below.</summary>
        public static float[] Resample(float[] clip, float pitch)
        {
            int length = Math.Max(1, (int)(clip.Length / pitch));
            var output = new float[length];
            for (int i = 0; i < length; i++)
            {
                double position = i * (double)pitch;
                int index = (int)position;
                double frac = position - index;
                float a = clip[Math.Min(index, clip.Length - 1)];
                float b = clip[Math.Min(index + 1, clip.Length - 1)];
                output[i] = (float)(a + (b - a) * frac);
            }
            return output;
        }
    }
}
