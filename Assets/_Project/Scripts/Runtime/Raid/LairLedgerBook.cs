using Code.Scripts.EventSystems;
using Plunderspell.Lair;
using TMPro;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The Lair ledger's open book (#357, #447): the left page is the demanded piece's drawing
    /// (<see cref="LedgerDemandSketch"/>) and the right page says only "The Ophan demands", in ink that glows off the
    /// page. Nothing names the piece or counts the raids: the glow's steady colour tells how soon the demand is due
    /// (faint white with 3 raids left, amber with 2, red when due this raid), and its brightness swells and fades,
    /// faster and brighter as it nears. Decision of 2026-10-27, "The demand in the ledger is a picture and three glowing
    /// words". Prototype: the piece is set by LairRoomForge and the raids left by <see cref="_raidsUntilDue"/> until the
    /// demand itself (#419) supplies both.
    /// </summary>
    public class LairLedgerBook : MonoBehaviour
    {
        public const string Words = "The Ophan demands";

        [SerializeField] private TextMeshPro _rightPage;
        [SerializeField] private Plunderspell.Loot.LootItem _demanded;
        [Tooltip("Prototype until the demand exists: raids left before it falls due (1 is this raid).")]
        [SerializeField, Range(1, 3)] private int _raidsUntilDue = 1;

        private Material _ink;

        /// <summary>What the left page shows, in words for the checks: it holds no text, only the drawing.</summary>
        public string LeftText => $"[drawing of {(_demanded != null ? _demanded.DisplayName : "nothing")}]";
        public string RightText => _rightPage.text;
        public int RaidsUntilDue => _raidsUntilDue;

        /// <summary>The steady glow for the raids left: faint white far off, amber at the halfway mark, red when due this raid.</summary>
        public static Color Glow(int raidsUntilDue) => raidsUntilDue <= 1 ? new Color(1f, 0.08f, 0.03f)
            : raidsUntilDue == 2 ? new Color(1f, 0.52f, 0.06f)
            : new Color(0.95f, 0.93f, 0.86f);

        /// <summary>Seconds for one swell and fade of the glow: 5 far off, 3.5 at the halfway mark, 2 when due.</summary>
        public static float BreathSeconds(int raidsUntilDue) => raidsUntilDue <= 1 ? 2f : raidsUntilDue == 2 ? 3.5f : 5f;

        /// <summary>How bright the glow's swell peaks, 0 to 1: dim far off, brightest when due.</summary>
        public static float PeakBrightness(int raidsUntilDue) => raidsUntilDue <= 1 ? 1f : raidsUntilDue == 2 ? 0.7f : 0.45f;

        private void Start()
        {
            _rightPage.text = Words;
            _rightPage.alignment = TextAlignmentOptions.Center;
            _rightPage.fontSizeMax = 48f;
            _rightPage.fontSizeMin = 20f;
            // This page's own copy of the font material, so no other text glows. The ledger font uses TMP's mobile shader,
            // which has no glow pass, so the halo is a soft, widened underlay in the glow's colour.
            _ink = _rightPage.fontMaterial;
            _ink.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            _ink.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.9f);
            _ink.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            _ink.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            _rightPage.color = Color.white; // the colour lives in the material, where it can go past 1 and bloom
            SetRaidsUntilDue(_raidsUntilDue);
        }

        public void SetRaidsUntilDue(int raids) => _raidsUntilDue = Mathf.Clamp(raids, 1, 3);

        // per-frame: the magic ink's brightness swells and fades, faster and brighter as the demand nears; its colour holds
        private void Update()
        {
            if (_ink == null)
                return;
            float swell = 0.5f - 0.5f * Mathf.Cos(Time.time * 2f * Mathf.PI / BreathSeconds(_raidsUntilDue));
            float bright = Mathf.Lerp(0.2f, 1f, swell) * PeakBrightness(_raidsUntilDue);
            Color glow = Glow(_raidsUntilDue);
            // Kept near 1 so tone mapping does not bleach the hue to white; the halo carries most of the brightness.
            _ink.SetColor(ShaderUtilities.ID_FaceColor, glow * (0.9f + 0.8f * bright));
            _ink.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(glow.r, glow.g, glow.b, 0.25f + 0.6f * bright));
            _ink.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f + 0.6f * bright);
        }
    }
}
