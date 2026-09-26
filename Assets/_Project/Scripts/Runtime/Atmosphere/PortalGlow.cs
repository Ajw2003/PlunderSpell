using Plunderspell.Extraction;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// The portal's look: a lapis swirl with its own light and fog glow, standing wherever the raid
    /// opened the portal (<see cref="RaidDirector.PortalOpened"/>), and faltering through the last
    /// minute of the raid so the closing reads without a glance at the HUD
    /// (docs/plans/night-atmosphere.md, section 6).
    /// </summary>
    public class PortalGlow : MonoBehaviour
    {
        [SerializeField] private RaidDirector _director;
        [SerializeField] private ExtractionZone _zone;
        [Tooltip("The swirl quad.")]
        [SerializeField] private Renderer _swirl;
        [Tooltip("The fire that lights the world and the fog round the portal, burning its own lapis.")]
        [SerializeField] private FireSource _light;
        [Tooltip("Seconds before the raid ends that the portal starts to falter.")]
        [SerializeField] private float _falterSeconds = 60f;

        private static readonly int s_strength = Shader.PropertyToID("_Strength");
        private MaterialPropertyBlock _block;
        private Vector3 _swirlScale;
        private bool _open;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (_director == null)
                _director = FindFirstObjectByType<RaidDirector>();
            if (_zone == null)
                _zone = FindFirstObjectByType<ExtractionZone>();
            if (_swirl != null)
                _swirlScale = _swirl.transform.localScale;
            Show(false);
        }

        private void OnEnable()
        {
            if (_director != null)
            {
                _director.PortalOpened += OnPortalOpened;
                _director.PhaseChanged += OnPhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.PortalOpened -= OnPortalOpened;
                _director.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void OnPortalOpened(Vector3 floorPoint)
        {
            transform.position = floorPoint;
            // The placeholder pad stays as the trigger's footprint on the floor, but the portal is the landmark now.
            if (_zone != null)
            {
                Transform marker = _zone.transform.Find("Marker");
                if (marker != null && marker.TryGetComponent(out Renderer pad))
                    pad.enabled = false;
            }
            Show(true);
        }

        private void OnPhaseChanged(RaidPhase phase)
        {
            if (phase == RaidPhase.InLair || phase == RaidPhase.Resolved)
                Show(false);
        }

        private void Show(bool open)
        {
            _open = open;
            if (_swirl != null)
                _swirl.gameObject.SetActive(open);
            if (_light != null)
                _light.gameObject.SetActive(open);
        }

        /// <summary>
        /// The portal's brightness, 0 to 1, at <paramref name="closing"/> (0 with a minute or more
        /// left, 1 as it shuts) and <paramref name="time"/> in seconds. It dims steadily and gutters
        /// slowly, about two dips a second at most: the old falter strobed at up to nine, past the
        /// three flashes a second that photosensitivity guidance allows (#145).
        /// </summary>
        public static float FalterStrength(float closing, float time)
        {
            closing = Mathf.Clamp01(closing);
            if (closing <= 0f)
                return 1f;
            float fade = Mathf.Lerp(1f, 0.35f, closing);
            float gutter = Mathf.PerlinNoise(time * Mathf.Lerp(0.8f, 2f, closing), 3.7f);
            return fade * Mathf.Lerp(1f, gutter, 0.15f + 0.25f * closing);
        }

        private void Update()
        {
            if (!_open)
                return;

            float closing = 0f;
            if (_zone != null && _falterSeconds > 0f && _zone.TimeRemaining < _falterSeconds && !_zone.ExtractionComplete)
                closing = 1f - Mathf.Clamp01(_zone.TimeRemaining / _falterSeconds);
            float strength = FalterStrength(closing, Time.time);

            if (_swirl != null)
            {
                _block.SetFloat(s_strength, strength);
                _swirl.SetPropertyBlock(_block);
                // The shrink follows the clock only, so the portal's size never jitters with the gutter.
                _swirl.transform.localScale = _swirlScale * Mathf.Lerp(1f, 0.6f, closing);
            }
            if (_light != null)
                _light.Strength = strength;
        }
    }
}
