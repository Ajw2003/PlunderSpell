// Headless shim for the URP / post-processing surface the atmosphere code uses: Volume,
// VolumeProfile and the volume-component parameters, camera data, and the renderer-feature base.
// Nothing renders: a VolumeProfile is a typed bag of components whose parameters remember what was
// Override()n, which is all the game logic and the Editor look-forges observe.
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public struct ScriptableRenderContext { }

    /// <summary>The render-pipeline callbacks. Nothing renders headlessly, so nothing ever raises them.</summary>
    public static class RenderPipelineManager
    {
#pragma warning disable CS0067
        public static event Action<ScriptableRenderContext, Camera> beginCameraRendering;
#pragma warning restore CS0067
    }

    public class VolumeParameter<T>
    {
        public T value;
        public bool overrideState;
        public VolumeParameter() { }
        public VolumeParameter(T v) { value = v; }
        public void Override(T v) { value = v; overrideState = true; }
    }

    public class VolumeComponent : ScriptableObject
    {
        public bool active = true;
    }

    public class VolumeProfile : ScriptableObject
    {
        private readonly List<VolumeComponent> _components = new List<VolumeComponent>();
        public List<VolumeComponent> components => _components;

        public T Add<T>(bool overrides = false) where T : VolumeComponent
        {
            if (Has<T>())
                throw new InvalidOperationException("Component already exists in the volume");
            T c = ScriptableObject.CreateInstance<T>();
            _components.Add(c);
            return c;
        }

        public bool Has<T>() where T : VolumeComponent => _components.Exists(c => c is T);

        public bool TryGet<T>(out T component) where T : VolumeComponent
        {
            foreach (VolumeComponent c in _components)
                if (c is T t) { component = t; return true; }
            component = null;
            return false;
        }
    }

    public class Volume : MonoBehaviour
    {
        public bool isGlobal = true;
        public float priority;
        public float weight = 1f;
        public VolumeProfile sharedProfile;
        /// <summary>Approximation: real Unity's <c>profile</c> getter clones sharedProfile on first use; here it aliases it.</summary>
        public VolumeProfile profile
        {
            get => sharedProfile ??= ScriptableObject.CreateInstance<VolumeProfile>();
            set => sharedProfile = value;
        }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public enum TonemappingMode { None, Neutral, ACES }
    public enum FilmGrainLookup { Thin1, Thin2, Medium1, Medium2, Medium3, Medium4, Medium5, Medium6, Large01, Large02, Custom }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public enum AntialiasingQuality { Low, Medium, High }

    public sealed class Tonemapping : VolumeComponent
    {
        public VolumeParameter<TonemappingMode> mode = new VolumeParameter<TonemappingMode>();
    }

    public sealed class Bloom : VolumeComponent
    {
        public VolumeParameter<float> threshold = new VolumeParameter<float>();
        public VolumeParameter<float> intensity = new VolumeParameter<float>();
        public VolumeParameter<float> scatter = new VolumeParameter<float>();
        public VolumeParameter<bool> highQualityFiltering = new VolumeParameter<bool>();
    }

    public sealed class ColorAdjustments : VolumeComponent
    {
        public VolumeParameter<float> postExposure = new VolumeParameter<float>();
        public VolumeParameter<float> contrast = new VolumeParameter<float>();
        public VolumeParameter<float> saturation = new VolumeParameter<float>();
        public VolumeParameter<Color> colorFilter = new VolumeParameter<Color>();
    }

    public sealed class SplitToning : VolumeComponent
    {
        public VolumeParameter<Color> shadows = new VolumeParameter<Color>();
        public VolumeParameter<Color> highlights = new VolumeParameter<Color>();
        public VolumeParameter<float> balance = new VolumeParameter<float>();
    }

    public sealed class LiftGammaGain : VolumeComponent
    {
        public VolumeParameter<Vector4> lift = new VolumeParameter<Vector4>();
        public VolumeParameter<Vector4> gamma = new VolumeParameter<Vector4>();
        public VolumeParameter<Vector4> gain = new VolumeParameter<Vector4>();
    }

    public sealed class Vignette : VolumeComponent
    {
        public VolumeParameter<float> intensity = new VolumeParameter<float>();
        public VolumeParameter<float> smoothness = new VolumeParameter<float>();
        public VolumeParameter<Color> color = new VolumeParameter<Color>();
    }

    public sealed class FilmGrain : VolumeComponent
    {
        public VolumeParameter<FilmGrainLookup> type = new VolumeParameter<FilmGrainLookup>();
        public VolumeParameter<float> intensity = new VolumeParameter<float>();
        public VolumeParameter<float> response = new VolumeParameter<float>();
    }

    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing;
        public AntialiasingMode antialiasing;
        public AntialiasingQuality antialiasingQuality;
    }

    public static class CameraExtensions
    {
        /// <summary>Real URP adds the component on demand; so does this.</summary>
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera)
        {
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            return data != null ? data : camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }
    }

    /// <summary>Base of every URP renderer feature. Only the Editor forges' feature list is modelled.</summary>
    public abstract class ScriptableRendererFeature : ScriptableObject
    {
        public bool isActive { get; private set; } = true;
        public void SetActive(bool active) => isActive = active;
    }
}
