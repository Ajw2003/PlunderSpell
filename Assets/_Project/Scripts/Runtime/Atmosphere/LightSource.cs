using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// Marks something that lights the castle, with how far and how brightly, so a stealth system
    /// can later ask "is this player standing in light?". Darkness changes nothing yet
    /// (docs/plans/night-atmosphere.md, decisions); this only records the data.
    /// </summary>
    public class LightSource : MonoBehaviour
    {
        private static readonly List<LightSource> s_all = new List<LightSource>();

        /// <summary>Every light source in the world.</summary>
        public static IReadOnlyList<LightSource> All => s_all;

        [Tooltip("Metres the light reaches while burning at its current size.")]
        [SerializeField] private float _radius = 6f;

        [Tooltip("Brightness at the source, 0-1, while burning.")]
        [SerializeField] private float _intensity = 1f;

        /// <summary>Metres the light reaches right now; 0 while unlit.</summary>
        public float Radius => IsLit ? _radius : 0f;

        /// <summary>Brightness at the source right now; 0 while unlit.</summary>
        public float Intensity => IsLit ? _intensity : 0f;

        /// <summary>Whether it is burning.</summary>
        public bool IsLit { get; private set; }

        /// <summary>Updated by whatever owns the light (a fire, the portal).</summary>
        public void Set(bool isLit, float radius, float intensity)
        {
            IsLit = isLit;
            _radius = radius;
            _intensity = intensity;
        }

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);
    }
}
