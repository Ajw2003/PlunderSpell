using System.Collections.Generic;
using RogueAi.Alarm;
using RogueAi.Castle;
using UnityEngine;

namespace RogueAi.Atmosphere
{
    /// <summary>
    /// One fire: a stylised flame, embers, a flickering point light and a <see cref="LightSource"/>
    /// tag. It lights, flares and reddens with the alarm (<see cref="FireRules"/>), easing rather than
    /// snapping, and takes whatever light the budget grants it (<see cref="CastleAtmosphere"/>).
    /// The fog halo round it is drawn by the fog pass from <see cref="All"/>.
    /// </summary>
    public class FireSource : MonoBehaviour
    {
        private static readonly List<FireSource> s_all = new List<FireSource>();

        /// <summary>Every fire in the world, lit or not.</summary>
        public static IReadOnlyList<FireSource> All => s_all;

        [Header("Kind")]
        [SerializeField] private FireKind _kind = FireKind.Brazier;
        [Tooltip("The alarm state that first lights it: 0 Calm, 1 Stirred, 2 Roused, 3 HueAndCry.")]
        [SerializeField] private int _litFrom;

        [Header("Parts")]
        [SerializeField] private Light _light;
        [Tooltip("The flame quads. Scaled with the fire and tinted through a property block.")]
        [SerializeField] private Renderer[] _flames = new Renderer[0];
        [SerializeField] private Transform _flameRoot;
        [SerializeField] private ParticleSystem _embers;
        [SerializeField] private LightSource _lightSource;

        [Header("Burn")]
        [Tooltip("Light intensity at calm size, before the atmosphere's fire multiplier.")]
        [SerializeField] private float _intensity = 6f;
        [Tooltip("Light range at calm size, metres.")]
        [SerializeField] private float _range = 8f;
        [Tooltip("Where the light and the glow sit, relative to the fire's base.")]
        [SerializeField] private Vector3 _flameOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float _flickerAmount = 0.18f;
        [SerializeField] private float _flickerSpeed = 2.2f;
        [Tooltip("Seconds to catch light, flare or die down.")]
        [SerializeField] private float _easeSeconds = 1.5f;

        [Header("Own colour")]
        [Tooltip("Burn this colour at a steady strength instead of the night's flame colour. The portal: " +
                 "magic is lapis, never fire-coloured, and it does not answer the alarm.")]
        [SerializeField] private bool _useOwnColor;
        [SerializeField] private Color _ownColor = new Color(0.48f, 0.42f, 0.63f);

        private static readonly int s_flameColor = Shader.PropertyToID("_FlameColor");
        private static readonly int s_flameSeed = Shader.PropertyToID("_FlameSeed");
        private MaterialPropertyBlock _block;
        private float _seed;
        private float _lit;
        private float _flare = 1f;
        private float _targetLit;
        private float _targetFlare = 1f;
        private Vector3 _flameRootScale = Vector3.one;
        private float _emberRate;

        /// <summary>What kind of fire this is.</summary>
        public FireKind Kind => _kind;

        /// <summary>How lit it is right now, 0 (out) to 1 (burning), mid-ease included.</summary>
        public float Lit => _lit;

        /// <summary>Whether it is burning or catching; false once fully out.</summary>
        public bool IsBurning => _lit > 0.01f;

        /// <summary>Where the light and glow come from.</summary>
        public Vector3 GlowPosition => transform.position + transform.rotation * _flameOffset;

        /// <summary>Light range right now, grown by flaring.</summary>
        public float CurrentRange => _range * Mathf.Lerp(1f, _flare, 0.6f);

        /// <summary>The light's brightness right now, flicker included, before the budget.</summary>
        public float CurrentIntensity { get; private set; }

        /// <summary>The point light, for the budget.</summary>
        public Light Light => _light;

        /// <summary>Scales the whole fire, light and flame. The portal falters through this.</summary>
        public float Strength { get; set; } = 1f;

        /// <summary>Sets the kind and first-lit state; used when a fire is spawned from an anchor.</summary>
        public void Configure(FireKind kind, int litFrom)
        {
            _kind = kind;
            _litFrom = litFrom;
        }

        /// <summary>
        /// For a fire whose room already models its iron (a forge, a brazier in the mesh): removes
        /// this prefab's own holder and moves the fire so its flame's base sits where it stood.
        /// </summary>
        public void DropHolder()
        {
            Transform prop = transform.Find("Prop");
            if (prop != null)
            {
                prop.gameObject.SetActive(false);
                Destroy(prop.gameObject);
            }
            if (_flameRoot != null)
                transform.position -= transform.rotation * _flameRoot.localPosition;
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _seed = Random.value * 100f;
            if (_flameRoot != null)
                _flameRootScale = _flameRoot.localScale;
            if (_embers != null)
                _emberRate = _embers.emission.rateOverTimeMultiplier;
            if (_lightSource == null)
                _lightSource = GetComponent<LightSource>();
        }

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);

        /// <summary>
        /// Aims the fire at an alarm state. <paramref name="immediate"/> skips the ease, for fires
        /// spawned into a castle already in that state.
        /// </summary>
        public void SetAlarmState(AlarmState state, bool immediate)
        {
            _targetLit = FireRules.IsLit(_kind, _litFrom, state) ? 1f : 0f;
            _targetFlare = FireRules.Flare(_kind, state);
            if (immediate)
            {
                _lit = _targetLit;
                _flare = _targetFlare;
            }
        }

        /// <summary>Called by the atmosphere every frame with the night's fire colour and multiplier.</summary>
        public void Burn(Color flameColor, float intensityScale, float deltaTime)
        {
            float step = _easeSeconds > 0f ? deltaTime / _easeSeconds : 1f;
            _lit = Mathf.MoveTowards(_lit, _targetLit, step);
            _flare = Mathf.MoveTowards(_flare, _targetFlare, step);

            float t = Time.time * _flickerSpeed + _seed;
            float flicker = 1f + _flickerAmount * (Mathf.PerlinNoise(t, _seed) * 2f - 1f)
                               + _flickerAmount * 0.5f * (Mathf.PerlinNoise(t * 3.1f, _seed + 7f) * 2f - 1f);

            if (_useOwnColor)
            {
                flameColor = _ownColor;
                intensityScale = 1f;
            }
            CurrentIntensity = _intensity * intensityScale * _lit * _flare * flicker * Strength;

            if (_light != null)
            {
                _light.color = flameColor;
                _light.intensity = CurrentIntensity;
                _light.range = CurrentRange;
            }

            if (_flameRoot != null)
            {
                // The flame breathes a little with the light's flicker, so the two read as one fire.
                float size = _lit * Strength * Mathf.Lerp(1f, _flare, 0.8f) * (1f + 0.35f * (flicker - 1f));
                _flameRoot.localScale = _flameRootScale * size;
                _flameRoot.gameObject.SetActive(_lit > 0.01f);
            }

            if (_flames.Length > 0)
            {
                _block.SetColor(s_flameColor, flameColor * Mathf.Lerp(1f, 1.4f, _flare - 1f) * Mathf.Clamp01(Strength));
                _block.SetFloat(s_flameSeed, _seed);
                foreach (Renderer flame in _flames)
                {
                    if (flame != null)
                        flame.SetPropertyBlock(_block);
                }
            }

            if (_embers != null)
            {
                ParticleSystem.EmissionModule emission = _embers.emission;
                emission.rateOverTimeMultiplier = _emberRate * _lit * _flare * _flare;
            }

            if (_lightSource != null)
                _lightSource.Set(_lit > 0.5f, CurrentRange, Mathf.Clamp01(_lit * _flare / FireRules.FullAlertFlare));
        }

        /// <summary>Applies the budget's grant: whether the light is on, and whether it casts shadows.</summary>
        public void ApplyGrant(FireRules.LightGrant grant)
        {
            if (_light == null)
                return;
            bool on = grant != FireRules.LightGrant.None && _lit > 0.01f;
            if (_light.enabled != on)
                _light.enabled = on;
            LightShadows shadows = grant == FireRules.LightGrant.Shadowed ? LightShadows.Soft : LightShadows.None;
            if (_light.shadows != shadows)
                _light.shadows = shadows;
        }
    }
}
