using TMPro;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The line a client sees at the portal arch when it walks in: "The host sets out" (#359). World text in the ledger's
    /// Spectral font, eased in and out over <see cref="Seconds"/>. Built in code by <see cref="LairPortalTrigger"/>.
    /// </summary>
    public class HostSetsOutLine : MonoBehaviour
    {
        public const float Seconds = 1f;
        public const string Words = "The host sets out";

        private TextMeshPro _text;
        private float _age = Seconds;

        /// <summary>Opacity now, 0 when idle; for the co-op check.</summary>
        public float Alpha => _text.alpha;

        /// <summary>The highest opacity reached since the last <see cref="Show"/>, for a check that cannot sample inside a second.</summary>
        public float PeakAlpha { get; private set; }

        /// <summary>The line as a child of <paramref name="arch"/>, a little in front of it and above head height.</summary>
        public static HostSetsOutLine Create(Transform arch)
        {
            var go = new GameObject("HostSetsOutLine");
            go.transform.SetParent(arch, false);
            go.transform.localPosition = new Vector3(0.4f, 0.8f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // reads from the room side, looking at the arch
            go.transform.localScale = Vector3.one * 0.01f; // the rect is in centimetres, like the ledger pages

            var text = go.AddComponent<TextMeshPro>();
            text.font = Resources.Load<TMP_FontAsset>("UI/Fonts/Spectral-Regular SDF");
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.text = Words;
            text.alpha = 0f;

            var line = go.AddComponent<HostSetsOutLine>();
            line._text = text;
            return line;
        }

        /// <summary>Starts the fade; a line already showing begins again.</summary>
        public void Show()
        {
            _age = 0f;
            PeakAlpha = 0f;
        }

        private void Update()
        {
            if (_age >= Seconds)
                return;
            _age = Mathf.Min(Seconds, _age + Time.deltaTime);
            _text.alpha = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(2f * _age / Seconds - 1f));
            PeakAlpha = Mathf.Max(PeakAlpha, _text.alpha);
        }
    }
}
