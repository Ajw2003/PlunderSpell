using TMPro;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The chalk text on a Market counter's slate. A new text fades the old one out, swaps, and fades the new one in,
    /// <see cref="Seconds"/> in all, so nothing on the slate snaps. Built by MarketYardForge; fed by <see cref="SellCounter"/>.
    /// </summary>
    public class CounterSlate : MonoBehaviour
    {
        public const float Seconds = 1f;

        [SerializeField] private TextMeshPro _text;

        private string _wanted = "";
        private float _fade = 1f; // 0 to 1 through the swap: first half out, second half in
        private float _alpha;

        /// <summary>The text it is showing or fading to; for the checks.</summary>
        public string Text => _wanted;

        /// <summary>The text on the board right now, mid-fade included.</summary>
        public string Shown => _text.text;

        public float Alpha => _text.alpha;

        public void Set(TextMeshPro text) => _text = text;

        private void Awake()
        {
            if (_text.font == null)
                _text.font = Resources.Load<TMP_FontAsset>("UI/Fonts/Spectral-Regular SDF");
            _text.text = "";
            _text.alpha = 0f;
        }

        /// <summary>Shows <paramref name="text"/>; the first text of an empty slate only fades in.</summary>
        public void Show(string text)
        {
            if (text == _wanted)
                return;
            _wanted = text;
            bool blank = _text.text == "";
            _fade = blank ? 0.5f : 0f;
            _alpha = blank ? 0f : _text.alpha; // a swap cut short fades out from where it was
        }

        private void Update()
        {
            if (_fade >= 1f)
                return;
            _fade = Mathf.Min(1f, _fade + Time.deltaTime / Seconds);
            if (_fade >= 0.5f && _text.text != _wanted)
                _text.text = _wanted;
            _text.alpha = _fade < 0.5f ? _alpha * Mathf.SmoothStep(0f, 1f, 1f - _fade * 2f) : Mathf.SmoothStep(0f, 1f, (_fade - 0.5f) * 2f);
        }
    }
}
