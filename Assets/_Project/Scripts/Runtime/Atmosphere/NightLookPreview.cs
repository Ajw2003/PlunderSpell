using System.Collections.Generic;
using Plunderspell.Castle;
using Plunderspell.Raid;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// Throwaway preview of the "calm" night look chosen in docs/plans/night-atmosphere.md and
    /// rendered as reference at docs/generated/look-samples-2026-09-24/calm.png. Darkens the scene,
    /// lights fire at the curtain wall from primitives, fogs the bailey warm, and drops in stand-in
    /// props so the look can be judged in Play mode.
    ///
    /// This is a stand-in only: no alarm-state blending, no FireSource prefabs, no fire-anchor
    /// pipeline, no quality levels, no shaders. It exists to be replaced by
    /// docs/plans/night-atmosphere.md steps 1-2 once those land — disable or delete this component
    /// then.
    /// </summary>
    public class NightLookPreview : MonoBehaviour
    {
        private const float k_CellSize = 12f;

        [Header("Scene wiring")]
        [Tooltip("Whose generated castle to decorate. Rebuilds whenever Castle changes identity.")]
        [SerializeField] private RaidDirector _director;

        [Tooltip("The scene's DirectionalLight, retinted and dimmed to read as moonlight.")]
        [SerializeField] private Light _moon;

        [Header("Fog")]
        [SerializeField] private Color _fogColor = new Color(0.1f, 0.06f, 0.045f);
        [SerializeField] private float _fogDensity = 0.03f;

        [Header("Ambient")]
        [SerializeField] private Color _ambientColor = new Color(0.035f, 0.026f, 0.02f);

        [Header("Moon")]
        [SerializeField] private Color _moonColor = new Color(0.45f, 0.58f, 0.9f);
        [SerializeField] private float _moonIntensity = 0.12f;
        [SerializeField] private float _moonElevationDegrees = 26f;

        [Header("Post-processing")]
        [SerializeField] private float _postExposure = 0.45f;
        [SerializeField] private float _contrast = 6f;
        [SerializeField] private float _saturation = -2f;
        [SerializeField] private Color _colorFilter = new Color(1.08f, 0.97f, 0.88f);
        [SerializeField] private float _bloomThreshold = 0.75f;
        [SerializeField] private float _bloomIntensity = 1.5f;
        [SerializeField] private float _bloomScatter = 0.85f;
        [SerializeField] private float _vignetteIntensity = 0.35f;

        [Header("Fire")]
        [SerializeField] private Color _fireColor = new Color(1.0f, 0.52f, 0.2f);
        [SerializeField] private float _brazierRange = 9f;
        [SerializeField] private float _brazierIntensity = 12f;
        [SerializeField] private float _torchRange = 6f;
        [SerializeField] private float _torchIntensity = 6f;
        [SerializeField] private float _flickerAmount = 0.15f;
        [SerializeField] private float _flickerSpeed = 1.6f;

        [Header("Materials")]
        [SerializeField] private Color _ironColor = new Color(0.06f, 0.06f, 0.06f);
        [SerializeField] private Color _woodColor = new Color(0.22f, 0.15f, 0.09f);
        [SerializeField] private Color _hayColor = new Color(0.45f, 0.36f, 0.16f);
        [SerializeField] private Color _emberColor = new Color(1.0f, 0.52f, 0.2f);
        [SerializeField] private float _emberEmissionStrength = 8f;

        // Restore targets for OnDisable.
        private bool _prevFog;
        private FogMode _prevFogMode;
        private Color _prevFogColor;
        private float _prevFogDensity;
        private AmbientMode _prevAmbientMode;
        private Color _prevAmbientLight;
        private Material _prevSkybox;
        private Color _prevMoonColor;
        private float _prevMoonIntensity;
        private Quaternion _prevMoonRotation;

        private GameObject _volumeObject;
        private VolumeProfile _volumeProfile;
        private GameObject _propsRoot;
        private ProceduralCastleData _builtFor;

        private Camera _cachedCamera;
        private UniversalAdditionalCameraData _cachedCameraData;
        private CameraClearFlags _prevCameraClearFlags;
        private Color _prevCameraBackground;

        private Material _ironMaterial;
        private Material _woodMaterial;
        private Material _hayMaterial;
        private Material _emberMaterial;

        /// <summary>One flame's point light and the phase its flicker runs at.</summary>
        private struct FlickerLight
        {
            public Light Light;
            public float BaseIntensity;
            public float Seed;
        }

        private readonly List<FlickerLight> _flickerLights = new List<FlickerLight>();

        private void OnEnable()
        {
            CaptureRenderSettings();
            ApplyNightLook();
            CreateVolume();
            CreateMaterials();
        }

        private void OnDisable()
        {
            RestoreRenderSettings();
            DestroyProps();
            DestroyVolume();
            DestroyMaterials();
        }

        private void OnDestroy()
        {
            // OnDisable already ran when the component is disabled or the object destroyed while
            // active; this only guards the case where OnDisable was skipped (e.g. domain reload).
            DestroyProps();
            DestroyVolume();
            DestroyMaterials();
        }

        private void CaptureRenderSettings()
        {
            _prevFog = RenderSettings.fog;
            _prevFogMode = RenderSettings.fogMode;
            _prevFogColor = RenderSettings.fogColor;
            _prevFogDensity = RenderSettings.fogDensity;
            _prevAmbientMode = RenderSettings.ambientMode;
            _prevAmbientLight = RenderSettings.ambientLight;
            _prevSkybox = RenderSettings.skybox;

            if (_moon != null)
            {
                _prevMoonColor = _moon.color;
                _prevMoonIntensity = _moon.intensity;
                _prevMoonRotation = _moon.transform.rotation;
            }
        }

        private void RestoreRenderSettings()
        {
            RenderSettings.fog = _prevFog;
            RenderSettings.fogMode = _prevFogMode;
            RenderSettings.fogColor = _prevFogColor;
            RenderSettings.fogDensity = _prevFogDensity;
            RenderSettings.ambientMode = _prevAmbientMode;
            RenderSettings.ambientLight = _prevAmbientLight;
            RenderSettings.skybox = _prevSkybox;

            if (_moon != null)
            {
                _moon.color = _prevMoonColor;
                _moon.intensity = _prevMoonIntensity;
                _moon.transform.rotation = _prevMoonRotation;
            }

            if (_cachedCamera != null)
            {
                _cachedCamera.clearFlags = _prevCameraClearFlags;
                _cachedCamera.backgroundColor = _prevCameraBackground;
            }
        }

        /// <summary>Darkens the world: fog, ambient, skybox and the moon's colour/strength/angle.</summary>
        private void ApplyNightLook()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = _fogColor;
            RenderSettings.fogDensity = _fogDensity;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _ambientColor;
            RenderSettings.skybox = null;

            if (_moon != null)
            {
                _moon.color = _moonColor;
                _moon.intensity = _moonIntensity;
                _moon.transform.rotation = Quaternion.Euler(_moonElevationDegrees, -35f, 0f);
            }
        }

        /// <summary>Builds a runtime Volume + profile: tonemapping, bloom, colour grade, vignette.</summary>
        private void CreateVolume()
        {
            _volumeObject = new GameObject("NightLookPreview_Volume");
            _volumeObject.transform.SetParent(transform, false);

            _volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tonemapping = _volumeProfile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var bloom = _volumeProfile.Add<Bloom>(true);
            bloom.threshold.Override(_bloomThreshold);
            bloom.intensity.Override(_bloomIntensity);
            bloom.scatter.Override(_bloomScatter);

            var colorAdjustments = _volumeProfile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(_postExposure);
            colorAdjustments.contrast.Override(_contrast);
            colorAdjustments.saturation.Override(_saturation);
            colorAdjustments.colorFilter.Override(_colorFilter);

            var vignette = _volumeProfile.Add<Vignette>(true);
            vignette.intensity.Override(_vignetteIntensity);

            Volume volume = _volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;
            volume.profile = _volumeProfile;
        }

        private void DestroyVolume()
        {
            if (_volumeObject != null)
                Destroy(_volumeObject);
            _volumeObject = null;

            if (_volumeProfile != null)
                Destroy(_volumeProfile);
            _volumeProfile = null;
        }

        private void CreateMaterials()
        {
            _ironMaterial = MakeLitMaterial("NightLookPreview_Iron", _ironColor);
            _woodMaterial = MakeLitMaterial("NightLookPreview_Wood", _woodColor);
            _hayMaterial = MakeLitMaterial("NightLookPreview_Hay", _hayColor);

            _emberMaterial = MakeLitMaterial("NightLookPreview_Ember", new Color(0.1f, 0.05f, 0.02f));
            _emberMaterial.EnableKeyword("_EMISSION");
            _emberMaterial.SetColor("_EmissionColor", _emberColor * _emberEmissionStrength);
            _emberMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private void DestroyMaterials()
        {
            DestroyMaterial(ref _ironMaterial);
            DestroyMaterial(ref _woodMaterial);
            DestroyMaterial(ref _hayMaterial);
            DestroyMaterial(ref _emberMaterial);
        }

        private static void DestroyMaterial(ref Material material)
        {
            if (material != null)
                Destroy(material);
            material = null;
        }

        private static Material MakeLitMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.color = color;
            return material;
        }

        private void LateUpdate()
        {
            EnsureCameraConfigured();

            if (_director == null)
                return;

            ProceduralCastleData castle = _director.Castle;
            if (!ReferenceEquals(castle, _builtFor))
            {
                DestroyProps();
                if (castle != null)
                    BuildProps(castle);
                _builtFor = castle;
            }
        }

        /// <summary>Keeps Camera.main post-processed and cleared to the fog colour instead of a skybox.</summary>
        private void EnsureCameraConfigured()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            if (camera != _cachedCamera)
            {
                _cachedCamera = camera;
                _prevCameraClearFlags = camera.clearFlags;
                _prevCameraBackground = camera.backgroundColor;

                _cachedCameraData = camera.GetUniversalAdditionalCameraData();
            }

            if (_cachedCameraData != null)
                _cachedCameraData.renderPostProcessing = true;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _fogColor;
        }

        private void DestroyProps()
        {
            _flickerLights.Clear();
            if (_propsRoot != null)
                Destroy(_propsRoot);
            _propsRoot = null;
        }

        /// <summary>Walks every curtain-wall cell, dropping torches, braziers and stand-in props.</summary>
        private void BuildProps(ProceduralCastleData castle)
        {
            _propsRoot = new GameObject("NightLookPreview_Props");
            _propsRoot.transform.SetParent(transform, false);

            foreach (ProceduralCastleData.PlacedModule module in castle.PlacedModules)
            {
                if (module.Zone != CastleZone.CurtainWall)
                    continue;

                Vector2Int cell = module.GridPosition;
                bool isCorner = Mathf.Abs(cell.x) == Mathf.Abs(cell.y);
                bool isGatehouse = module.IsExtractionExit ||
                    (module.RoomId != null && module.RoomId.ToLowerInvariant().Contains("gatehouse"));

                Vector3 inward = InwardDirection(cell);

                if (!isCorner && !isGatehouse)
                {
                    Vector3 torchPos = module.Position - inward * 4.2f + Vector3.up * 3.2f;
                    SpawnTorch(torchPos);
                }

                if (!isCorner)
                {
                    if ((cell.x + cell.y) % 2 == 0)
                    {
                        Vector3 brazierPos = module.Position + inward * 4f;
                        SpawnBrazier(brazierPos);
                    }

                    if (Mod(cell.x + cell.y, 3) == 0)
                        SpawnProp(module.Position, inward, cell);
                }
            }
        }

        private static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        /// <summary>
        /// Reimplements the 4-line rule from <see cref="CastleSpawnResolver"/> (private there — see
        /// CastleSpawnResolver.cs:105): the world direction from a perimeter cell back toward the
        /// castle's centre.
        /// </summary>
        private static Vector3 InwardDirection(Vector2Int cell)
        {
            if (cell == Vector2Int.zero)
                return Vector3.zero;

            int ring = Mathf.Max(Mathf.Abs(cell.x), Mathf.Abs(cell.y));
            if (Mathf.Abs(cell.x) == ring)
                return new Vector3(cell.x > 0 ? -1f : 1f, 0f, 0f);
            return new Vector3(0f, 0f, cell.y > 0 ? -1f : 1f);
        }

        private void SpawnTorch(Vector3 position)
        {
            GameObject flame = CreateEmber(position, 0.12f, castsShadows: false);
            flame.transform.SetParent(_propsRoot.transform, true);

            Light light = flame.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = _fireColor;
            light.range = _torchRange;
            light.intensity = _torchIntensity;
            light.shadows = LightShadows.None;

            RegisterFlicker(light);
        }

        private void SpawnBrazier(Vector3 position)
        {
            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "NightLookPreview_Brazier";
            bowl.transform.SetParent(_propsRoot.transform, true);
            bowl.transform.position = position;
            bowl.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);
            StripCollider(bowl);
            bowl.GetComponent<MeshRenderer>().sharedMaterial = _ironMaterial;

            GameObject flame = CreateEmber(position + Vector3.up * 0.35f, 0.22f, castsShadows: false);
            flame.transform.SetParent(_propsRoot.transform, true);

            Light light = flame.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = _fireColor;
            light.range = _brazierRange;
            light.intensity = _brazierIntensity;
            light.shadows = LightShadows.Soft;

            RegisterFlicker(light);
        }

        private GameObject CreateEmber(Vector3 position, float radius, bool castsShadows)
        {
            GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ember.name = "NightLookPreview_Ember";
            ember.transform.position = position;
            ember.transform.localScale = Vector3.one * (radius * 2f);
            StripCollider(ember);
            ember.GetComponent<MeshRenderer>().sharedMaterial = _emberMaterial;
            ember.GetComponent<MeshRenderer>().shadowCastingMode =
                castsShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return ember;
        }

        private void RegisterFlicker(Light light)
        {
            _flickerLights.Add(new FlickerLight
            {
                Light = light,
                BaseIntensity = light.intensity,
                Seed = Random.Range(0f, 1000f),
            });
        }

        /// <summary>Mirrors add_props' dimensions and offsets from Tools/LookSamples/render_look_samples.py.</summary>
        private void SpawnProp(Vector3 modulePosition, Vector3 inward, Vector2Int cell)
        {
            Vector3 along = new Vector3(inward.z, 0f, -inward.x);
            float offset = (Mod(cell.x * 7 + cell.y * 13, 5) - 2) * 1.2f;
            Vector3 basePos = modulePosition + inward * 7f + along * offset;
            Quaternion rotation = Quaternion.LookRotation(along, Vector3.up);

            int propKind = Mod(cell.x * 3 + cell.y * 5, 3);
            switch (propKind)
            {
                case 0:
                    SpawnCartWithHay(basePos, rotation);
                    break;
                case 1:
                    SpawnStackedCrates(basePos, rotation);
                    break;
                default:
                    SpawnHayBale(basePos, rotation);
                    break;
            }
        }

        private void SpawnCartWithHay(Vector3 position, Quaternion rotation)
        {
            GameObject bed = SpawnBox(position + Vector3.up * 0.6f, rotation, new Vector3(2.2f, 0.7f, 4.0f), _woodMaterial);
            bed.name = "NightLookPreview_CartBed";
            SpawnBox(position + Vector3.up * 1.05f, rotation, new Vector3(2.0f, 0.24f, 3.6f), _hayMaterial)
                .name = "NightLookPreview_CartHay";

            Vector3 along = rotation * Vector3.forward;
            foreach (float wheelOffset in new[] { -1.4f, 1.4f })
            {
                Vector3 wheelPos = position + along * wheelOffset + Vector3.up * 0.55f;
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "NightLookPreview_CartWheel";
                wheel.transform.SetParent(_propsRoot.transform, true);
                wheel.transform.position = wheelPos;
                wheel.transform.rotation = rotation * Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(1.1f, 0.12f, 1.1f);
                StripCollider(wheel);
                wheel.GetComponent<MeshRenderer>().sharedMaterial = _woodMaterial;
            }
        }

        private void SpawnStackedCrates(Vector3 position, Quaternion rotation)
        {
            Vector3[] offsets =
            {
                new Vector3(0f, 0.45f, 0f),
                new Vector3(0.9f, 0.45f, 0.3f),
                new Vector3(0.4f, 1.3f, 0.1f),
            };
            foreach (Vector3 offset in offsets)
                SpawnBox(position + rotation * offset, rotation, Vector3.one * 0.9f, _woodMaterial)
                    .name = "NightLookPreview_Crate";
        }

        private void SpawnHayBale(Vector3 position, Quaternion rotation)
        {
            SpawnBox(position + Vector3.up * 0.5f, rotation, new Vector3(2.8f, 1.4f, 1.0f), _hayMaterial)
                .name = "NightLookPreview_HayBale";
        }

        private GameObject SpawnBox(Vector3 position, Quaternion rotation, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(_propsRoot.transform, true);
            box.transform.SetPositionAndRotation(position, rotation);
            box.transform.localScale = size;
            StripCollider(box);
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static void StripCollider(GameObject go)
        {
            // Primitives are the preview's only building block, but their colliders must not change
            // navigation or gameplay.
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        /// <summary>Slow Perlin flicker on every fire's point light, read from one cached list.</summary>
        private void Update()
        {
            if (_flickerLights.Count == 0)
                return;

            float time = Time.time * _flickerSpeed;
            for (int i = 0; i < _flickerLights.Count; i++)
            {
                FlickerLight flicker = _flickerLights[i];
                if (flicker.Light == null)
                    continue;

                float noise = Mathf.PerlinNoise(flicker.Seed, time) * 2f - 1f;
                flicker.Light.intensity = flicker.BaseIntensity * (1f + noise * _flickerAmount);
            }
        }
    }
}
