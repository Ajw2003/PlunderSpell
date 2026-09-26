using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Plunderspell.Alarm;
using Plunderspell.Atmosphere;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Raid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds everything the castle's night is made of (docs/plans/night-atmosphere.md) and wires it
    /// into a scene: the Low/Medium/High quality levels and their URP assets, the fog pass on each
    /// renderer, the four alarm-state grades, the atmosphere profile, the fire and portal prefabs.
    /// Idempotent: re-running rebuilds the same assets in place, keeping their GUIDs.
    /// See docs/4-systems/atmosphere.md.
    /// </summary>
    public static class NightAtmosphereForge
    {
        private const string SettingsDir = "Assets/_Project/Settings/Atmosphere";
        private const string PrefabDir = "Assets/_Project/Prefabs/Atmosphere";
        private const string MeshDir = "Assets/_Project/Art/Models/Atmosphere";
        private const string MaterialDir = "Assets/_Project/Art/Materials/Atmosphere";
        private const string ShaderDir = "Assets/_Project/Shaders/Atmosphere";
        private const string PipelineDir = "Assets/Settings";

        // ------------------------------------------------------------------------------------------
        // Menu
        // ------------------------------------------------------------------------------------------

        [MenuItem("Tools/Plunderspell/Night Atmosphere/Build Assets")]
        public static void BuildAllMenu() => Debug.Log(BuildAll());

        [MenuItem("Tools/Plunderspell/Night Atmosphere/Install In Open Scene")]
        public static void InstallMenu() => Debug.Log(InstallInOpenScene());

        /// <summary>Builds or rebuilds every atmosphere asset. Returns a report.</summary>
        public static string BuildAll()
        {
            var report = new StringBuilder("[NightAtmosphere] Built:\n");
            EnsureFolder(SettingsDir);
            EnsureFolder(PrefabDir);
            EnsureFolder(MeshDir);
            EnsureFolder(MaterialDir);

            report.AppendLine(BuildQualityLevels());
            Materials materials = BuildMaterials();
            report.AppendLine("  materials");
            NightAtmosphereProfile profile = BuildProfile();
            report.AppendLine($"  profile {AssetDatabase.GetAssetPath(profile)}");
            report.AppendLine(BuildFirePrefabs(materials));

            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        // ------------------------------------------------------------------------------------------
        // Quality levels and render pipeline assets
        // ------------------------------------------------------------------------------------------

        private readonly struct TierSettings
        {
            public readonly string Name;
            public readonly int MainShadowResolution;
            public readonly float ShadowDistance;
            public readonly int Cascades;
            public readonly int AdditionalShadowAtlas;
            public readonly int SoftShadowQuality;
            public readonly bool Ssao;
            public readonly bool SsaoHalfResolution;

            public TierSettings(string name, int mainShadow, float distance, int cascades, int atlas,
                int softQuality, bool ssao, bool ssaoHalf)
            {
                Name = name;
                MainShadowResolution = mainShadow;
                ShadowDistance = distance;
                Cascades = cascades;
                AdditionalShadowAtlas = atlas;
                SoftShadowQuality = softQuality;
                Ssao = ssao;
                SsaoHalfResolution = ssaoHalf;
            }
        }

        // docs/plans/night-atmosphere.md, section 5, "Quality levels". Fire shadows cost six atlas
        // slices each (a point light renders a cube); at 512 a 2048 atlas holds 16, a 4096 holds 64.
        private static readonly TierSettings[] s_tiers =
        {
            new TierSettings("Low", 1024, 30f, 1, 2048, 1, false, false),
            new TierSettings("Medium", 2048, 50f, 2, 4096, 2, true, true),
            new TierSettings("High", 4096, 80f, 4, 4096, 3, true, false),
        };

        private static string BuildQualityLevels()
        {
            const string sourceAsset = PipelineDir + "/PC_RPAsset.asset";
            const string sourceRenderer = PipelineDir + "/PC_Renderer.asset";
            var assets = new UniversalRenderPipelineAsset[s_tiers.Length];

            for (int i = 0; i < s_tiers.Length; i++)
            {
                TierSettings tier = s_tiers[i];
                string rendererPath = $"{PipelineDir}/{tier.Name}_Renderer.asset";
                string assetPath = $"{PipelineDir}/{tier.Name}_RPAsset.asset";
                if (!File.Exists(rendererPath))
                    AssetDatabase.CopyAsset(sourceRenderer, rendererPath);
                if (!File.Exists(assetPath))
                    AssetDatabase.CopyAsset(sourceAsset, assetPath);

                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                ConfigureRenderer(renderer, tier);

                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
                ConfigurePipelineAsset(asset, renderer, tier);
                assets[i] = asset;
            }

            ConfigureQualitySettings(assets);
            GraphicsSettings.defaultRenderPipeline = assets[1];
            return "  quality levels Low/Medium/High (default Medium)";
        }

        private static void ConfigurePipelineAsset(UniversalRenderPipelineAsset asset, UniversalRendererData renderer,
            TierSettings tier)
        {
            var so = new SerializedObject(asset);
            SerializedProperty list = so.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.FindProperty("m_RequireDepthTexture").boolValue = true;
            so.FindProperty("m_SupportsHDR").boolValue = true;
            so.FindProperty("m_MSAA").intValue = 1;
            so.FindProperty("m_RenderScale").floatValue = 1f;
            so.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = tier.MainShadowResolution;
            so.FindProperty("m_ShadowDistance").floatValue = tier.ShadowDistance;
            so.FindProperty("m_ShadowCascadeCount").intValue = tier.Cascades;
            so.FindProperty("m_AdditionalLightsRenderingMode").intValue = 1; // per pixel
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
            so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = tier.AdditionalShadowAtlas;
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_SoftShadowQuality").intValue = tier.SoftShadowQuality;
            so.FindProperty("m_ColorGradingMode").intValue = 1; // HDR grading
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureRenderer(UniversalRendererData renderer, TierSettings tier)
        {
            renderer.renderingMode = RenderingMode.ForwardPlus;

            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature == null || feature.GetType().Name != "ScreenSpaceAmbientOcclusion")
                    continue;
                feature.SetActive(tier.Ssao);
                var so = new SerializedObject(feature);
                SerializedProperty downsample = so.FindProperty("m_Settings.Downsample");
                if (downsample != null)
                    downsample.boolValue = tier.SsaoHalfResolution;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            NightFogFeature fog = renderer.rendererFeatures.OfType<NightFogFeature>().FirstOrDefault();
            if (fog == null)
            {
                fog = ScriptableObject.CreateInstance<NightFogFeature>();
                fog.name = "NightFog";
                AssetDatabase.AddObjectToAsset(fog, renderer);
                renderer.rendererFeatures.Add(fog);
            }
            var fogSo = new SerializedObject(fog);
            fogSo.FindProperty("_shader").objectReferenceValue = Shader.Find("Hidden/Plunderspell/NightFog");
            fogSo.ApplyModifiedPropertiesWithoutUndo();

            RepairFeatureMap(renderer);
            EditorUtility.SetDirty(renderer);
        }

        /// <summary>
        /// A renderer keeps a parallel list of its features' local file ids; a feature added from code
        /// needs its id written there too, or the renderer drops it on the next load.
        /// </summary>
        private static void RepairFeatureMap(ScriptableRendererData renderer)
        {
            var so = new SerializedObject(renderer);
            SerializedProperty features = so.FindProperty("m_RendererFeatures");
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize = features.arraySize;
            for (int i = 0; i < features.arraySize; i++)
            {
                Object feature = features.GetArrayElementAtIndex(i).objectReferenceValue;
                if (feature != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId))
                    map.GetArrayElementAtIndex(i).longValue = localId;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureQualitySettings(UniversalRenderPipelineAsset[] assets)
        {
            Object settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            var so = new SerializedObject(settings);
            SerializedProperty levels = so.FindProperty("m_QualitySettings");
            // Grows by copying the last level (PC), so every field a level needs is already set.
            levels.arraySize = s_tiers.Length;
            for (int i = 0; i < s_tiers.Length; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                level.FindPropertyRelative("name").stringValue = s_tiers[i].Name;
                level.FindPropertyRelative("customRenderPipeline").objectReferenceValue = assets[i];
                level.FindPropertyRelative("excludedTargetPlatforms").arraySize = 0;
                level.FindPropertyRelative("antiAliasing").intValue = 0;
                level.FindPropertyRelative("vSyncCount").intValue = 0;
                level.FindPropertyRelative("skinWeights").intValue = i == 0 ? 2 : 4;
                level.FindPropertyRelative("anisotropicTextures").intValue = i == 0 ? 1 : 2;
            }

            SerializedProperty defaults = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < defaults.arraySize; i++)
                defaults.GetArrayElementAtIndex(i).FindPropertyRelative("second").intValue = 1;
            so.FindProperty("m_CurrentQuality").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------------------------------
        // Materials
        // ------------------------------------------------------------------------------------------

        private class Materials
        {
            public Material Flame;
            public Material Ember;
            public Material PortalSwirl;
            public Material Sky;
            public Material Iron;
            public Material PortalEmber;
            public Material Earth;
        }

        private static Materials BuildMaterials()
        {
            var m = new Materials
            {
                Flame = MaterialAt("Flame", "Plunderspell/Flame"),
                Ember = MaterialAt("Ember", "Plunderspell/Ember"),
                PortalSwirl = MaterialAt("PortalSwirl", "Plunderspell/PortalSwirl"),
                Sky = MaterialAt("NightSky", "Plunderspell/NightSky"),
                Iron = MaterialAt("FireIron", CastleSurfaceMaterials.SurfaceShaderName),
                PortalEmber = MaterialAt("PortalEmber", "Plunderspell/Ember"),
                Earth = MaterialAt("BaileyEarth", CastleSurfaceMaterials.SurfaceShaderName),
            };
            var detail = AssetDatabase.LoadAssetAtPath<Texture2D>(CastleSurfaceMaterials.DetailTexturePath);
            SurfaceMaterial(m.Iron, new Color(0.2f, 0.19f, 0.18f), detail, new Vector4(0, 0, 1, 0), 0.5f, 0.5f, false);
            // Packed earth under the whole castle: the stone channel at a quarter scale reads as
            // trodden mud with the odd flag in it.
            SurfaceMaterial(m.Earth, new Color(0.36f, 0.29f, 0.22f), detail, new Vector4(1, 0, 0, 0), 0.25f, 0.4f, false);
            m.PortalEmber.SetColor("_Color", new Color(1.2f, 1.0f, 3.2f));
            // Lapis, the art bible's colour of magic; kept below white so ACES does not bleach it.
            m.PortalSwirl.SetColor("_ColorA", new Color(0.42f, 0.46f, 1.4f));
            m.PortalSwirl.SetColor("_ColorB", new Color(0.3f, 0.9f, 0.7f));
            m.PortalSwirl.SetFloat("_Brightness", 0.55f);
            foreach (Material material in new[] { m.Flame, m.Ember, m.PortalSwirl, m.Sky, m.Iron, m.PortalEmber, m.Earth })
                EditorUtility.SetDirty(material);
            return m;
        }

        private static void SurfaceMaterial(Material material, Color color, Texture2D detail, Vector4 mask,
            float scale, float strength, bool isStone)
        {
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_DetailMap", detail);
            material.SetVector("_DetailMask", mask);
            material.SetFloat("_DetailScale", scale);
            material.SetFloat("_DetailStrength", strength);
            material.SetFloat("_IsStone", isStone ? 1f : 0f);
            material.SetFloat("_GroundGrime", 1f);
        }

        private static Material MaterialAt(string name, string shaderName)
        {
            string path = $"{MaterialDir}/{name}.mat";
            Shader shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }

        // ------------------------------------------------------------------------------------------
        // Profile and grades
        // ------------------------------------------------------------------------------------------

        private static NightAtmosphereProfile BuildProfile()
        {
            string path = $"{SettingsDir}/NightAtmosphere.asset";
            var profile = AssetDatabase.LoadAssetAtPath<NightAtmosphereProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<NightAtmosphereProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.TransitionSeconds = 2f;
            profile.Calm = NightLooks.Calm(BuildGrade("Grade_Calm", NightLooks.CalmGrade));
            profile.Stirred = NightLooks.Stirred(BuildGrade("Grade_Stirred", NightLooks.StirredGrade));
            profile.Roused = NightLooks.Roused(BuildGrade("Grade_Roused", NightLooks.RousedGrade));
            profile.HueAndCry = NightLooks.HueAndCry(BuildGrade("Grade_HueAndCry", NightLooks.HueAndCryGrade));
            profile.BronzeAge = NightLooks.BronzeAgeTint;
            profile.HighMedieval = NightLooks.HighMedievalTint;
            profile.LateMedieval = NightLooks.LateMedievalTint;
            profile.AgeOfPowder = NightLooks.AgeOfPowderTint;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static VolumeProfile BuildGrade(string name, System.Action<VolumeProfile> fill)
        {
            string path = $"{SettingsDir}/{name}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            else
            {
                foreach (VolumeComponent component in profile.components.ToList())
                {
                    profile.components.Remove(component);
                    Object.DestroyImmediate(component, true);
                }
            }

            fill(profile);
            foreach (VolumeComponent component in profile.components)
            {
                component.name = component.GetType().Name;
                if (!AssetDatabase.IsSubAsset(component))
                    AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // ------------------------------------------------------------------------------------------
        // Fire and portal prefabs
        // ------------------------------------------------------------------------------------------

        private readonly struct FireSpec
        {
            public readonly string Name;
            public readonly FireKind Kind;
            public readonly int LitFrom;
            public readonly Mesh Prop;
            public readonly Vector3 FlameBase;
            public readonly Vector2 FlameSize;
            public readonly float Intensity;
            public readonly float Range;
            public readonly float EmberRate;
            public readonly bool Obstacle;

            public FireSpec(string name, FireKind kind, int litFrom, Mesh prop, Vector3 flameBase, Vector2 flameSize,
                float intensity, float range, float emberRate, bool obstacle)
            {
                Name = name;
                Kind = kind;
                LitFrom = litFrom;
                Prop = prop;
                FlameBase = flameBase;
                FlameSize = flameSize;
                Intensity = intensity;
                Range = range;
                EmberRate = emberRate;
                Obstacle = obstacle;
            }
        }

        private static string BuildFirePrefabs(Materials m)
        {
            Mesh brazier = SaveMesh(FireMeshes.Brazier(), "Brazier");
            Mesh sconce = SaveMesh(FireMeshes.Sconce(), "Sconce");
            Mesh beacon = SaveMesh(FireMeshes.Beacon(), "Beacon");

            var specs = new[]
            {
                new FireSpec("Fire_Sconce", FireKind.Sconce, 0, sconce, new Vector3(0f, 0.34f, 0.34f),
                    new Vector2(0.34f, 0.6f), 3.2f, 9f, 3f, false),
                new FireSpec("Fire_Brazier", FireKind.Brazier, 0, brazier, new Vector3(0f, 1.42f, 0f),
                    new Vector2(0.7f, 1.05f), 4.5f, 12f, 7f, true),
                new FireSpec("Fire_Hearth", FireKind.Hearth, 0, null, new Vector3(0f, 0.05f, 0f),
                    new Vector2(0.9f, 0.95f), 5f, 11f, 6f, false),
                new FireSpec("Fire_Beacon", FireKind.Beacon, 2, beacon, new Vector3(0f, 0.72f, 0f),
                    new Vector2(1.5f, 2.2f), 9f, 18f, 14f, true),
            };

            foreach (FireSpec spec in specs)
                BuildFirePrefab(spec, m);
            BuildPortalPrefab(m);
            return $"  fire prefabs ({specs.Length}) and the portal in {PrefabDir}";
        }

        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = $"{MeshDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                mesh.name = name;
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            existing.Clear();
            existing.vertices = mesh.vertices;
            existing.normals = mesh.normals;
            existing.uv = mesh.uv;
            existing.triangles = mesh.triangles;
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        private static void BuildFirePrefab(FireSpec spec, Materials m)
        {
            var root = new GameObject(spec.Name);
            if (spec.Prop != null)
            {
                var prop = new GameObject("Prop");
                prop.transform.SetParent(root.transform, false);
                prop.AddComponent<MeshFilter>().sharedMesh = spec.Prop;
                prop.AddComponent<MeshRenderer>().sharedMaterial = m.Iron;
                if (spec.Obstacle)
                {
                    var capsule = prop.AddComponent<CapsuleCollider>();
                    capsule.center = new Vector3(0f, spec.FlameBase.y * 0.5f, 0f);
                    capsule.height = spec.FlameBase.y;
                    capsule.radius = spec.Kind == FireKind.Beacon ? 0.7f : 0.3f;
                    var obstacle = prop.AddComponent<NavMeshObstacle>();
                    obstacle.shape = NavMeshObstacleShape.Capsule;
                    obstacle.center = capsule.center;
                    obstacle.height = capsule.height;
                    obstacle.radius = capsule.radius + 0.1f;
                    obstacle.carving = true;
                }
            }

            Transform flameRoot = AddFlame(root.transform, spec.FlameBase, spec.FlameSize, m.Flame, out Renderer flame);
            Vector3 glow = spec.FlameBase + Vector3.up * spec.FlameSize.y * 0.4f;
            Light light = AddLight(root.transform, glow, spec.Intensity, spec.Range);
            ParticleSystem embers = AddEmbers(root.transform, spec.FlameBase + Vector3.up * spec.FlameSize.y * 0.3f,
                spec.EmberRate, spec.FlameSize.x, m.Ember);
            LightSource tag = root.AddComponent<LightSource>();

            FireSource fire = root.AddComponent<FireSource>();
            var so = new SerializedObject(fire);
            so.FindProperty("_kind").intValue = (int)spec.Kind;
            so.FindProperty("_litFrom").intValue = spec.LitFrom;
            so.FindProperty("_light").objectReferenceValue = light;
            SerializedProperty flames = so.FindProperty("_flames");
            flames.arraySize = 1;
            flames.GetArrayElementAtIndex(0).objectReferenceValue = flame;
            so.FindProperty("_flameRoot").objectReferenceValue = flameRoot;
            so.FindProperty("_embers").objectReferenceValue = embers;
            so.FindProperty("_lightSource").objectReferenceValue = tag;
            so.FindProperty("_intensity").floatValue = spec.Intensity;
            so.FindProperty("_range").floatValue = spec.Range;
            so.FindProperty("_flameOffset").vector3Value = glow;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{spec.Name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static Transform AddFlame(Transform parent, Vector3 at, Vector2 size, Material material, out Renderer renderer)
        {
            var flameRoot = new GameObject("Flame").transform;
            flameRoot.SetParent(parent, false);
            flameRoot.localPosition = at;

            var quad = new GameObject("FlameQuad");
            quad.transform.SetParent(flameRoot, false);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var meshRenderer = quad.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            renderer = meshRenderer;
            return flameRoot;
        }

        private static Light AddLight(Transform parent, Vector3 at, float intensity, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.52f, 0.2f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.shadowNearPlane = 0.15f;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.5f;
            if (!go.TryGetComponent(out UniversalAdditionalLightData data))
                data = go.AddComponent<UniversalAdditionalLightData>();
            // Read-only in code; the atlas sizes in s_tiers assume the medium tier's 512.
            var dataSo = new SerializedObject(data);
            dataSo.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue =
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            dataSo.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }

        private static ParticleSystem AddEmbers(Transform parent, Vector3 at, float rate, float width, Material material)
        {
            var go = new GameObject("Embers");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.4f), new Color(1f, 0.45f, 0.15f));
            main.gravityModifier = -0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = width * 0.25f;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.8f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            return system;
        }

        private static void BuildPortalPrefab(Materials m)
        {
            var root = new GameObject("Portal");
            PortalGlow glow = root.AddComponent<PortalGlow>();

            var swirl = new GameObject("Swirl");
            swirl.transform.SetParent(root.transform, false);
            swirl.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            swirl.transform.localScale = new Vector3(2.4f, 3.3f, 1f);
            swirl.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var swirlRenderer = swirl.AddComponent<MeshRenderer>();
            swirlRenderer.sharedMaterial = m.PortalSwirl;
            swirlRenderer.shadowCastingMode = ShadowCastingMode.Off;
            swirlRenderer.receiveShadows = false;

            var lightRoot = new GameObject("Glow");
            lightRoot.transform.SetParent(root.transform, false);
            Light light = AddLight(lightRoot.transform, new Vector3(0f, 1.7f, 0f), 3f, 10f);
            ParticleSystem motes = AddEmbers(lightRoot.transform, new Vector3(0f, 0.3f, 0f), 10f, 2.2f, m.PortalEmber);
            LightSource tag = lightRoot.AddComponent<LightSource>();
            FireSource fire = lightRoot.AddComponent<FireSource>();
            var fireSo = new SerializedObject(fire);
            fireSo.FindProperty("_kind").intValue = (int)FireKind.Hearth;
            fireSo.FindProperty("_light").objectReferenceValue = light;
            fireSo.FindProperty("_embers").objectReferenceValue = motes;
            fireSo.FindProperty("_lightSource").objectReferenceValue = tag;
            fireSo.FindProperty("_intensity").floatValue = 3f;
            fireSo.FindProperty("_range").floatValue = 10f;
            fireSo.FindProperty("_flameOffset").vector3Value = new Vector3(0f, 1.7f, 0f);
            fireSo.FindProperty("_flickerAmount").floatValue = 0.06f;
            fireSo.FindProperty("_useOwnColor").boolValue = true;
            fireSo.FindProperty("_ownColor").colorValue = new Color(0.5f, 0.42f, 0.78f);
            fireSo.ApplyModifiedPropertiesWithoutUndo();

            var glowSo = new SerializedObject(glow);
            glowSo.FindProperty("_swirl").objectReferenceValue = swirlRenderer;
            glowSo.FindProperty("_light").objectReferenceValue = fire;
            glowSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/Portal.prefab");
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------------------------------------
        // Scene
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds the atmosphere, the fire spawner and the portal to the open scene, replacing the
        /// throwaway NightLookPreview. Returns a report. Save the scene afterwards.
        /// </summary>
        public static string InstallInOpenScene()
        {
            var report = new StringBuilder("[NightAtmosphere] Installed in the open scene:\n");

            GameObject preview = GameObject.Find("NightLookPreview");
            if (preview != null)
            {
                Object.DestroyImmediate(preview);
                report.AppendLine("  removed NightLookPreview");
            }

            GameObject root = GameObject.Find("NightAtmosphere") ?? new GameObject("NightAtmosphere");

            Light moon = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.type == LightType.Directional);
            if (moon != null)
            {
                moon.transform.rotation = Quaternion.Euler(26f, -150f, 0f);
                moon.shadows = LightShadows.Soft;
                moon.shadowStrength = 0.85f;
            }

            CastleAtmosphere atmosphere = GetOrAdd<CastleAtmosphere>(root);
            var atmosphereSo = new SerializedObject(atmosphere);
            atmosphereSo.FindProperty("_profile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<NightAtmosphereProfile>($"{SettingsDir}/NightAtmosphere.asset");
            atmosphereSo.FindProperty("_moon").objectReferenceValue = moon;
            atmosphereSo.FindProperty("_alarm").objectReferenceValue = Object.FindFirstObjectByType<AlarmFSMManager>();
            atmosphereSo.FindProperty("_director").objectReferenceValue = Object.FindFirstObjectByType<RaidDirector>();
            atmosphereSo.FindProperty("_surfaceShader").objectReferenceValue = Shader.Find(CastleSurfaceMaterials.SurfaceShaderName);
            atmosphereSo.FindProperty("_skyMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/NightSky.mat");
            atmosphereSo.ApplyModifiedPropertiesWithoutUndo();

            CastleFireSpawner spawner = GetOrAdd<CastleFireSpawner>(root);
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("_director").objectReferenceValue = Object.FindFirstObjectByType<RaidDirector>();
            spawnerSo.FindProperty("_generator").objectReferenceValue = Object.FindFirstObjectByType<ProceduralCastleGenerator>();
            spawnerSo.FindProperty("_sconce").objectReferenceValue = LoadFire("Fire_Sconce");
            spawnerSo.FindProperty("_brazier").objectReferenceValue = LoadFire("Fire_Brazier");
            spawnerSo.FindProperty("_hearth").objectReferenceValue = LoadFire("Fire_Hearth");
            spawnerSo.FindProperty("_beacon").objectReferenceValue = LoadFire("Fire_Beacon");
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            Transform portal = root.transform.Find("Portal");
            if (portal == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Portal.prefab");
                portal = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform)).transform;
                portal.name = "Portal";
            }
            var portalSo = new SerializedObject(portal.GetComponent<PortalGlow>());
            portalSo.FindProperty("_director").objectReferenceValue = Object.FindFirstObjectByType<RaidDirector>();
            portalSo.FindProperty("_zone").objectReferenceValue = Object.FindFirstObjectByType<ExtractionZone>();
            portalSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject ground = GameObject.Find("Ground");
            var earth = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/BaileyEarth.mat");
            if (ground != null && earth != null && ground.TryGetComponent(out MeshRenderer groundRenderer))
            {
                groundRenderer.sharedMaterial = earth;
                report.AppendLine("  Ground on packed earth");
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            report.AppendLine($"  NightAtmosphere (moon: {(moon != null ? moon.name : "none")}), fire spawner, portal");
            return report.ToString();
        }

        private static FireSource LoadFire(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab")?.GetComponent<FireSource>();

        private static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T existing) ? existing : go.AddComponent<T>();

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
