using Plunderspell.Acoustics;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A guard's cry for help when it first sights an intruder (#259). It is a sound like any other, through
    /// <see cref="NoiseBroadcaster"/>, so walls muffle it: guards within earshot hear it and come over, and
    /// each one that then sees the intruder cries out in turn. The cry scores nothing with the alarm; its
    /// effect is the guards it brings. Server only.
    /// </summary>
    public sealed class GuardCry
    {
        /// <summary>How far a cry carries in the open, in metres.</summary>
        public const float Radius = 18f;

        /// <summary>Loud enough that one wall muffles it and two nearly lose it; three walls (the cap) drop it
        /// below <see cref="AcousticEmitter.MinAudibleStrength"/>.</summary>
        public const float Strength = 0.4f;

        private readonly Guard _guard;

        public GuardCry(Guard guard)
        {
            _guard = guard;
        }

        /// <summary>True only while this guard's own cry is being delivered, so the guard can ignore it: the
        /// broadcast reaches every listener in range, the crier included.</summary>
        public bool IsCrying { get; private set; }

        /// <summary>Cries out from where the guard stands, which is where the guard saw the intruder.</summary>
        public void Raise()
        {
            if (!_guard.IsAuthority || _guard.IsDead)
                return;

            IsCrying = true;
            try
            {
                NoiseBroadcaster.Broadcast(_guard.transform.position, Radius, Strength, NoiseType.GuardCry,
                    ~0, _guard.Tuning.GeometryLayers);
            }
            finally
            {
                IsCrying = false;
            }
        }
    }
}
