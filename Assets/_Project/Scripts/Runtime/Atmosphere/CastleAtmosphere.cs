using System.Collections.Generic;
using RogueAi.Alarm;
using RogueAi.Raid;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RogueAi.Atmosphere
{
    /// <summary>
    /// The castle's night: fog, moon, sky, ambient, grade and every fire, blended between the four
    /// alarm states over about two seconds whenever <see cref="AlarmFSMManager.AlarmStateChanged"/>
    /// fires. That event already fires on every peer, so each machine blends its own visuals and
    /// nothing here is networked. See docs/systems/atmosphere.md.
    /// </summary>
    public class CastleAtmosphere : MonoBehaviour
    {
        [Tooltip("The four states' looks.")]
        [SerializeField] private NightAtmosphereProfile _profile;

        [Tooltip("The scene's directional light, used as the moon.")]
        [SerializeField] private Light _moon;

        [Tooltip("The castle's alarm. Found in the scene when left empty.")]
        [SerializeField] private AlarmFSMManager _alarm;

        [Tooltip("Plunderspell/NightSky. The fog pass paints over most of it.")]
        [SerializeField] private Material _skyMaterial;

        [Tooltip("Whose Age sets the stone and flame tints. Found in the scene when left empty.")]
        [SerializeField] private RaidDirector _director;

        private static readonly int s_fogColor = Shader.PropertyToID("_NF_FogColor");
        private static readonly int s_fogParams = Shader.PropertyToID("_NF_Params");
        private static readonly int s_moonDir = Shader.PropertyToID("_NF_MoonDir");
        private static readonly int s_moonColor = Shader.PropertyToID("_NF_MoonColor");
        private static readonly int s_scatter = Shader.PropertyToID("_NF_Scatter");
        private static readonly int s_lightPos = Shader.PropertyToID("_NF_LightPos");
        private static readonly int s_lightColor = Shader.PropertyToID("_NF_LightColor");
        private static readonly int s_skyZenith = Shader.PropertyToID("_Zenith");
        private static readonly int s_skyHorizon = Shader.PropertyToID("_Horizon");
        private static readonly int s_skyMoonDir = Shader.PropertyToID("_MoonDir");
        private static readonly int s_skyMoonColor = Shader.PropertyToID("_MoonColor");
        private static readonly int s_stoneTint = Shader.PropertyToID("_PlunderStoneTint");

        private const int k_MaxScatterLights = 32;
        private const float k_ScatterReach = 70f;
        private const float k_BudgetInterval = 0.2f;
        private const float k_VisibilityInterval = 0.1f;
        private const float k_VisibilityFade = 6f;
        private const float k_OccludedGlow = 0.2f;
        // Closest a view ray counts as passing to a flame, metres. Keeps the glow's centre soft
        // rather than a pinpoint that bloom turns into a white disc.
        private const float k_HaloCore = 0.6f;
        private const string k_TriplanarKeyword = "_PLUNDER_TRIPLANAR";

        private readonly float[] _weights = new float[4];
        private readonly Volume[] _volumes = new Volume[4];
        private readonly VolumeProfile[] _volumeProfiles = new VolumeProfile[4];
        private readonly Vector4[] _lightPos = new Vector4[k_MaxScatterLights];
        private readonly Vector4[] _lightColor = new Vector4[k_MaxScatterLights];
        private readonly Dictionary<FireSource, float> _visibility = new Dictionary<FireSource, float>();
        private readonly List<FireSource> _scatterOrder = new List<FireSource>();
        private readonly List<float> _sqrDistances = new List<float>();
        private readonly List<bool> _isLit = new List<bool>();
        private readonly List<FireRules.LightGrant> _grants = new List<FireRules.LightGrant>();
        private readonly List<int> _order = new List<int>();

        private AlarmState _state = AlarmState.Calm;
        private AtmosphereLook _current;
        private Material _skyInstance;
        private float _nextBudget;
        private float _nextVisibility;
        private QualityTier _tier;
        private Camera _camera;
        private Vector3 _eye;
        private System.Comparison<FireSource> _byDistanceFromEye;

        // Restored on disable, so a scene without the atmosphere renders as it did before.
        private AmbientMode _prevAmbientMode;
        private Color _prevAmbientSky, _prevAmbientEquator, _prevAmbientGround;
        private bool _prevFog;
        private Material _prevSkybox;
        private Color _prevMoonColor;
        private float _prevMoonIntensity;

        /// <summary>The alarm state the night is heading for (or in).</summary>
        public static AlarmState CurrentState { get; private set; } = AlarmState.Calm;

        /// <summary>The night's current blended look.</summary>
        public AtmosphereLook Current => _current;

        /// <summary>The active atmosphere, or null. There is at most one per scene.</summary>
        public static CastleAtmosphere Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            if (_alarm == null)
                _alarm = FindFirstObjectByType<AlarmFSMManager>();
            if (_director == null)
                _director = FindFirstObjectByType<RaidDirector>();
            if (_alarm != null)
            {
                _alarm.AlarmStateChanged += OnAlarmStateChanged;
                _state = _alarm.State;
            }
            CurrentState = _state;
            for (int i = 0; i < _weights.Length; i++)
                _weights[i] = i == (int)_state ? 1f : 0f;

            CaptureRenderSettings();
            CreateVolumes();
            if (_skyMaterial != null)
            {
                _skyInstance = new Material(_skyMaterial);
                RenderSettings.skybox = _skyInstance;
            }
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            ApplyQuality();
            NightFogFeature.IsActive = true;
            Apply(0f);
        }

        private void OnDisable()
        {
            if (_alarm != null)
                _alarm.AlarmStateChanged -= OnAlarmStateChanged;
            NightFogFeature.IsActive = false;
            Shader.SetGlobalVector(s_scatter, Vector4.zero);
            Shader.SetGlobalVector(s_stoneTint, Vector4.zero);
            RestoreRenderSettings();
            DestroyVolumes();
            if (_skyInstance != null)
                Destroy(_skyInstance);
            if (Instance == this)
                Instance = null;
        }

        private void OnAlarmStateChanged(AlarmState state)
        {
            _state = state;
            CurrentState = state;
            foreach (FireSource fire in FireSource.All)
                fire.SetAlarmState(state, immediate: false);
        }

        /// <summary>Re-reads the quality level: fire budget, detail keyword, film grain.</summary>
        public void ApplyQuality()
        {
            _tier = AtmosphereQuality.Current;
            TierBudget budget = AtmosphereQuality.BudgetFor(_tier);
            if (budget.TriplanarDetail)
                Shader.EnableKeyword(k_TriplanarKeyword);
            else
                Shader.DisableKeyword(k_TriplanarKeyword);
            foreach (VolumeProfile profile in _volumeProfiles)
            {
                if (profile != null && profile.TryGet(out FilmGrain grain))
                    grain.active = budget.FilmGrain;
            }
            _camera = null; // re-applied to the camera on the next frame
        }

        private void Update()
        {
            float seconds = _profile != null ? Mathf.Max(0.01f, _profile.TransitionSeconds) : 2f;
            Apply(Time.deltaTime / seconds);
        }

        private void Apply(float step)
        {
            if (_profile == null)
                return;

            // Every state's weight walks toward 1 for the target and 0 for the rest at the same rate,
            // so the weights always sum to one, even when the alarm changes again mid-blend.
            for (int i = 0; i < _weights.Length; i++)
                _weights[i] = Mathf.MoveTowards(_weights[i], i == (int)_state ? 1f : 0f, step);
            float total = 0f;
            foreach (float w in _weights)
                total += w;

            _current = default;
            for (int i = 0; i < _weights.Length; i++)
            {
                float w = total > 0f ? _weights[i] / total : 0f;
                float eased = w * w * (3f - 2f * w);
                if (_volumes[i] != null)
                    _volumes[i].weight = eased;
                if (w > 0f)
                    _current.Accumulate(_profile.For((AlarmState)i), w);
            }

            ApplyEraTint();
            ApplyLighting();
            ApplyFog();
            ApplyCamera();
            BurnFires();
        }

        /// <summary>Stone and flame shift with the Age being raided; the rest of the night is shared.</summary>
        private void ApplyEraTint()
        {
            Inventory.HistoricalEra era = _director != null ? _director.Era : Inventory.HistoricalEra.HighMedieval;
            NightAtmosphereProfile.EraTint tint = _profile.ForEra(era);
            Color stone = tint.Stone.maxColorComponent > 0f ? tint.Stone : Color.white;
            Shader.SetGlobalVector(s_stoneTint, new Vector4(stone.r, stone.g, stone.b, 1f));
            if (tint.Flame.maxColorComponent > 0f)
                _current.FlameColor *= tint.Flame;
        }

        private void ApplyLighting()
        {
            RenderSettings.ambientSkyColor = _current.AmbientSky;
            RenderSettings.ambientEquatorColor = _current.AmbientEquator;
            RenderSettings.ambientGroundColor = _current.AmbientGround;
            if (_moon != null)
            {
                _moon.color = _current.MoonColor;
                _moon.intensity = _current.MoonIntensity;
            }
            if (_skyInstance != null)
            {
                _skyInstance.SetColor(s_skyZenith, _current.SkyZenith);
                _skyInstance.SetColor(s_skyHorizon, _current.SkyHorizon);
                _skyInstance.SetVector(s_skyMoonDir, MoonDirection());
                _skyInstance.SetColor(s_skyMoonColor, _current.MoonColor);
            }
        }

        private Vector3 MoonDirection() => _moon != null ? -_moon.transform.forward : Vector3.up;

        private void ApplyFog()
        {
            Shader.SetGlobalColor(s_fogColor, _current.FogColor);
            Shader.SetGlobalVector(s_fogParams, new Vector4(_current.FogDensity, _current.FogBaseHeight,
                Mathf.Max(0.001f, _current.FogHeightFalloff), _current.SkyDistance));
            Vector3 moonDir = MoonDirection();
            Shader.SetGlobalVector(s_moonDir, new Vector4(moonDir.x, moonDir.y, moonDir.z, _current.MoonAnisotropy));
            Shader.SetGlobalColor(s_moonColor, _current.MoonColor * _current.MoonScatter);

            int count = GatherScatterLights();
            Shader.SetGlobalVectorArray(s_lightPos, _lightPos);
            Shader.SetGlobalVectorArray(s_lightColor, _lightColor);
            Shader.SetGlobalVector(s_scatter, new Vector4(_current.FireScatter, _current.FireAnisotropy, k_HaloCore, count));
        }

        /// <summary>
        /// The nearest burning fires within reach, each dimmed while something solid stands between
        /// it and the camera, so a fire in the next room does not glow through the wall.
        /// </summary>
        private int GatherScatterLights()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return 0;
            Vector3 eye = camera.transform.position;
            _eye = eye;
            int limit = Mathf.Min(k_MaxScatterLights, AtmosphereQuality.BudgetFor(_tier).ScatteredFires);

            // Fires come and go with each castle; drop the ones that went.
            if (_visibility.Count > FireSource.All.Count * 2 + 8)
                _visibility.Clear();

            _scatterOrder.Clear();
            foreach (FireSource fire in FireSource.All)
            {
                if (fire.IsBurning && (fire.GlowPosition - eye).sqrMagnitude < k_ScatterReach * k_ScatterReach)
                    _scatterOrder.Add(fire);
            }
            _byDistanceFromEye ??= (a, b) =>
                (a.GlowPosition - _eye).sqrMagnitude.CompareTo((b.GlowPosition - _eye).sqrMagnitude);
            _scatterOrder.Sort(_byDistanceFromEye);

            bool probe = Time.unscaledTime >= _nextVisibility;
            if (probe)
                _nextVisibility = Time.unscaledTime + k_VisibilityInterval;

            int count = Mathf.Min(limit, _scatterOrder.Count);
            for (int i = 0; i < count; i++)
            {
                FireSource fire = _scatterOrder[i];
                Vector3 glow = fire.GlowPosition;
                if (!_visibility.TryGetValue(fire, out float seen))
                    seen = 1f;
                if (probe)
                {
                    bool blocked = Physics.Linecast(eye, glow, ~0, QueryTriggerInteraction.Ignore)
                                   && Physics.Linecast(eye, glow + Vector3.up * 1.5f, ~0, QueryTriggerInteraction.Ignore);
                    float target = blocked ? k_OccludedGlow : 1f;
                    seen = Mathf.MoveTowards(seen, target, k_VisibilityFade * k_VisibilityInterval);
                    _visibility[fire] = seen;
                }

                Color c = fire.Light != null ? fire.Light.color : _current.FlameColor;
                float strength = fire.CurrentIntensity * seen;
                _lightPos[i] = new Vector4(glow.x, glow.y, glow.z, fire.CurrentRange * 1.4f);
                _lightColor[i] = new Vector4(c.r * strength, c.g * strength, c.b * strength, 0f);
            }
            for (int i = count; i < k_MaxScatterLights; i++)
                _lightColor[i] = Vector4.zero;
            return count;
        }

        private void BurnFires()
        {
            float dt = Time.deltaTime;
            IReadOnlyList<FireSource> fires = FireSource.All;
            for (int i = 0; i < fires.Count; i++)
                fires[i].Burn(_current.FlameColor, _current.FireIntensity, dt);

            if (Time.unscaledTime < _nextBudget)
                return;
            _nextBudget = Time.unscaledTime + k_BudgetInterval;

            Camera camera = Camera.main;
            if (camera == null)
                return;
            Vector3 eye = camera.transform.position;
            _sqrDistances.Clear();
            _isLit.Clear();
            for (int i = 0; i < fires.Count; i++)
            {
                _sqrDistances.Add(FireRules.SqrDistance(fires[i].GlowPosition, eye));
                _isLit.Add(fires[i].IsBurning);
            }

            TierBudget budget = AtmosphereQuality.BudgetFor(_tier);
            FireRules.ShareLights(_sqrDistances, _isLit, budget.ShadowedFires, budget.UnshadowedFires, _grants, _order);
            for (int i = 0; i < fires.Count; i++)
                fires[i].ApplyGrant(_grants[i]);
        }

        /// <summary>
        /// The player's camera arrives with the player, after this has woken: post-processing on,
        /// the level's anti-aliasing, and the night sky behind everything.
        /// </summary>
        private void ApplyCamera()
        {
            Camera camera = Camera.main;
            if (camera == null || camera == _camera)
                return;
            _camera = camera;
            camera.clearFlags = CameraClearFlags.Skybox;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            if (data == null)
                return;
            data.renderPostProcessing = true;
            data.antialiasing = _tier == QualityTier.Low
                ? AntialiasingMode.FastApproximateAntialiasing
                : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = _tier == QualityTier.High ? AntialiasingQuality.High : AntialiasingQuality.Medium;
        }

        private void CreateVolumes()
        {
            for (int i = 0; i < _volumes.Length; i++)
            {
                VolumeProfile source = _profile != null ? _profile.For((AlarmState)i).Post : null;
                if (source == null)
                    continue;
                var go = new GameObject($"Grade_{(AlarmState)i}");
                go.transform.SetParent(transform, false);
                Volume volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10f;
                // A copy, so switching film grain per quality level never edits the asset.
                _volumeProfiles[i] = Instantiate(source);
                volume.sharedProfile = _volumeProfiles[i];
                volume.weight = _weights[i];
                _volumes[i] = volume;
            }
        }

        private void DestroyVolumes()
        {
            for (int i = 0; i < _volumes.Length; i++)
            {
                if (_volumes[i] != null)
                    Destroy(_volumes[i].gameObject);
                if (_volumeProfiles[i] != null)
                    Destroy(_volumeProfiles[i]);
                _volumes[i] = null;
                _volumeProfiles[i] = null;
            }
        }

        private void CaptureRenderSettings()
        {
            _prevAmbientMode = RenderSettings.ambientMode;
            _prevAmbientSky = RenderSettings.ambientSkyColor;
            _prevAmbientEquator = RenderSettings.ambientEquatorColor;
            _prevAmbientGround = RenderSettings.ambientGroundColor;
            _prevFog = RenderSettings.fog;
            _prevSkybox = RenderSettings.skybox;
            if (_moon != null)
            {
                _prevMoonColor = _moon.color;
                _prevMoonIntensity = _moon.intensity;
            }
        }

        private void RestoreRenderSettings()
        {
            RenderSettings.ambientMode = _prevAmbientMode;
            RenderSettings.ambientSkyColor = _prevAmbientSky;
            RenderSettings.ambientEquatorColor = _prevAmbientEquator;
            RenderSettings.ambientGroundColor = _prevAmbientGround;
            RenderSettings.fog = _prevFog;
            RenderSettings.skybox = _prevSkybox;
            if (_moon != null)
            {
                _moon.color = _prevMoonColor;
                _moon.intensity = _prevMoonIntensity;
            }
        }
    }
}
