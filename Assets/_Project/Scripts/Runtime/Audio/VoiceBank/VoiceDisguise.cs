using System;

namespace Plunderspell.Audio.VoiceBank
{
    /// <summary>
    /// Re-voices a clip so a guard never sounds like the player: pitch and formants move together, the
    /// clip keeps its length (up to the profile's speed), and a soft low-pass and saturation add character.
    /// Plain C#; always returns a new array.
    /// </summary>
    public static class VoiceDisguise
    {
        private const float MaxPeak = 0.95f;
        private const float MinRatio = 0.25f;
        private const float MaxRatio = 4f;
        private const float WindowSeconds = 0.025f;
        private const float HopSeconds = 0.0125f;
        private const float SearchSeconds = 0.010f;
        private const float FadeSeconds = 0.005f;
        private const int CoarseStep = 4;

        public static float[] Render(float[] input, int sampleRate, DisguiseProfile profile)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (input.Length == 0) return new float[0];

            float pitch = Clamp(profile.PitchRatio, MinRatio, MaxRatio);
            float speed = Clamp(profile.Speed, MinRatio, MaxRatio);

            float[] clean = Sanitise(input);
            float inputPeak = Peak(clean);

            float[] shifted = Resample(clean, pitch);
            int targetLength = Math.Max(1, (int)Math.Round(input.Length / speed));
            float[] stretched = StretchToLength(shifted, targetLength, sampleRate);

            LowPass(stretched, profile.BrightnessHz, sampleRate);
            LowPass(stretched, profile.BrightnessHz, sampleRate);
            Saturate(stretched, 1f + 4f * Clamp(profile.Roughness, 0f, 1f));
            NormaliseTo(stretched, Math.Min(inputPeak, MaxPeak));
            Fade(stretched, Math.Max(1, (int)(FadeSeconds * sampleRate)));
            return stretched;
        }

        private static float[] Sanitise(float[] input)
        {
            var clean = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
                clean[i] = float.IsNaN(input[i]) || float.IsInfinity(input[i]) ? 0f : input[i];
            return clean;
        }

        private static float[] Resample(float[] input, float step)
        {
            int length = Math.Max(1, (int)(input.Length / step));
            var output = new float[length];
            for (int i = 0; i < length; i++)
            {
                double at = i * (double)step;
                int index = (int)at;
                float frac = (float)(at - index);
                float a = input[Math.Min(index, input.Length - 1)];
                float b = input[Math.Min(index + 1, input.Length - 1)];
                output[i] = a + (b - a) * frac;
            }
            return output;
        }

        // WSOLA: each output window is taken from the input spot that best continues the previous one, so
        // pitch survives the change of length.
        private static float[] StretchToLength(float[] input, int targetLength, int sampleRate)
        {
            int hop = Math.Max(1, (int)(HopSeconds * sampleRate));
            int window = hop * 2;
            int search = (int)(SearchSeconds * sampleRate);

            if (input.Length < window * 2)
                return Resample(input, (float)input.Length / targetLength);

            var output = new float[targetLength];
            var weight = new float[targetLength];
            var shape = new float[window];
            for (int i = 0; i < window; i++)
                shape[i] = 0.5f - 0.5f * (float)Math.Cos(2.0 * Math.PI * i / window);

            double analysisHop = hop * (double)input.Length / targetLength;
            int lastStart = input.Length - window;
            int previous = 0;

            for (int frame = 0; frame * hop < targetLength; frame++)
            {
                int start;
                if (frame == 0)
                {
                    start = 0;
                }
                else
                {
                    int nominal = (int)Math.Round(frame * analysisHop);
                    start = BestMatch(input, previous + hop, nominal, search, window, lastStart);
                }

                int outStart = frame * hop;
                int copy = Math.Min(window, targetLength - outStart);
                for (int i = 0; i < copy; i++)
                {
                    output[outStart + i] += input[start + i] * shape[i];
                    weight[outStart + i] += shape[i];
                }
                previous = start;
            }

            for (int i = 0; i < targetLength; i++)
                output[i] /= Math.Max(weight[i], 0.1f);
            return output;
        }

        // The match is found in two passes: a coarse one over every few candidates using every few samples of
        // the window, then a fine one around the winner. Scanning every candidate at full resolution cost
        // about 14 ms per second of audio, enough to drop frames when a guard spoke a long line.
        private static int BestMatch(float[] input, int referenceStart, int nominal, int search, int window, int lastStart)
        {
            int reference = Math.Min(referenceStart, input.Length - window);
            int low = Math.Max(0, nominal - search);
            int high = Math.Min(lastStart, nominal + search);
            if (low > high) return Math.Min(Math.Max(nominal, 0), lastStart);

            int best = low;
            double bestScore = double.NegativeInfinity;
            for (int candidate = low; candidate <= high; candidate += CoarseStep)
            {
                double score = MatchScore(input, candidate, reference, window, CoarseStep);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            int fineLow = Math.Max(low, best - CoarseStep);
            int fineHigh = Math.Min(high, best + CoarseStep);
            bestScore = double.NegativeInfinity;
            for (int candidate = fineLow; candidate <= fineHigh; candidate++)
            {
                double score = MatchScore(input, candidate, reference, window, 1);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        private static double MatchScore(float[] input, int candidate, int reference, int window, int stride)
        {
            double dot = 0;
            double energy = 0;
            for (int i = 0; i < window; i += stride)
            {
                float value = input[candidate + i];
                dot += value * input[reference + i];
                energy += value * value;
            }
            return dot / Math.Sqrt(energy + 1e-9);
        }

        private static void LowPass(float[] samples, float cutoffHz, int sampleRate)
        {
            float cutoff = Clamp(cutoffHz, 50f, sampleRate * 0.45f);
            float alpha = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoff / sampleRate);
            float state = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                state += alpha * (samples[i] - state);
                samples[i] = state;
            }
        }

        private static void Saturate(float[] samples, float drive)
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] = (float)Math.Tanh(samples[i] * drive);
        }

        private static void NormaliseTo(float[] samples, float targetPeak)
        {
            float peak = Peak(samples);
            float gain = peak > 1e-9f ? targetPeak / peak : 0f;
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= gain;
        }

        private static void Fade(float[] samples, int fadeSamples)
        {
            int fade = Math.Min(fadeSamples, samples.Length / 2);
            for (int i = 0; i < fade; i++)
            {
                float gain = (float)i / fade;
                samples[i] *= gain;
                samples[samples.Length - 1 - i] *= gain;
            }
        }

        private static float Peak(float[] samples)
        {
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float magnitude = Math.Abs(samples[i]);
                if (magnitude > peak) peak = magnitude;
            }
            return peak;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
