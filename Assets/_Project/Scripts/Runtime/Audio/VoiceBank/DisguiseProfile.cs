namespace Plunderspell.Audio.VoiceBank
{
    /// <summary>A guard's fixed voice colouring, derived only from its seed so it sounds the same every run.</summary>
    public readonly struct DisguiseProfile
    {
        private const float ReferenceHz = 120f;
        private const float DeadZoneLow = 0.85f;
        private const float DeadZoneHigh = 1.15f;
        private const float MinPitchRatio = 0.6f;
        private const float MaxPitchRatio = 1.6f;

        public DisguiseProfile(float pitchRatio, float speed, float brightnessHz, float roughness)
        {
            PitchRatio = pitchRatio;
            Speed = speed;
            BrightnessHz = brightnessHz;
            Roughness = roughness;
        }

        public float PitchRatio { get; }
        public float Speed { get; }
        public float BrightnessHz { get; }
        public float Roughness { get; }

        // Pitch shifts close to 1 sound like the player's own voice, so the dead zone is skipped on purpose.
        public static DisguiseProfile For(int seed, float archetypeHz)
        {
            float pitch = archetypeHz / ReferenceHz * Lerp(0.90f, 1.10f, Unit(seed, 1));
            if (pitch > DeadZoneLow && pitch < DeadZoneHigh)
            {
                float extra = Unit(seed, 2) * 0.06f;
                pitch = pitch < 1f ? DeadZoneLow - extra : DeadZoneHigh + extra;
            }
            pitch = pitch < MinPitchRatio ? MinPitchRatio : pitch > MaxPitchRatio ? MaxPitchRatio : pitch;

            return new DisguiseProfile(
                pitch,
                Lerp(0.88f, 1.12f, Unit(seed, 3)),
                Lerp(2400f, 5200f, Unit(seed, 4)),
                Lerp(0f, 0.35f, Unit(seed, 5)));
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }

        private static float Unit(int seed, int salt)
        {
            unchecked
            {
                uint x = (uint)seed * 0x9E3779B1u + (uint)salt * 0x85EBCA6Bu;
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return (x >> 8) / 16777216f;
            }
        }
    }
}
