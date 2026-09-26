using Plunderspell.Core;
using UnityEngine;

namespace RogueAi.Atmosphere
{
    /// <summary>The three graphics levels (docs/plans/night-atmosphere.md, section 5).</summary>
    public enum QualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    /// What each graphics level spends. The render-pipeline half (moon shadow cascades, SSAO,
    /// shadow atlas) lives in the Low/Medium/High URP assets; this is the half the atmosphere code
    /// decides at runtime.
    /// </summary>
    public readonly struct TierBudget
    {
        /// <summary>Nearest fires whose light casts shadows.</summary>
        public readonly int ShadowedFires;

        /// <summary>Further fires that still light the world, without shadows. Past these, flame and halo only.</summary>
        public readonly int UnshadowedFires;

        /// <summary>Fires whose glow the fog pass computes.</summary>
        public readonly int ScatteredFires;

        /// <summary>Triplanar surface detail; off means one cheap projection.</summary>
        public readonly bool TriplanarDetail;

        /// <summary>The raymarched fog with moon shafts.</summary>
        public readonly bool VolumetricFog;

        /// <summary>Very light film grain.</summary>
        public readonly bool FilmGrain;

        public TierBudget(int shadowed, int unshadowed, int scattered, bool triplanar, bool volumetric, bool grain)
        {
            ShadowedFires = shadowed;
            UnshadowedFires = unshadowed;
            ScatteredFires = scattered;
            TriplanarDetail = triplanar;
            VolumetricFog = volumetric;
            FilmGrain = grain;
        }
    }

    /// <summary>
    /// Maps the active Unity quality level to a <see cref="QualityTier"/> and its budget, and picks
    /// the default level for a machine. Pure apart from reading <see cref="QualitySettings"/> and
    /// <see cref="SystemInfo"/>, so the table is testable.
    /// </summary>
    public static class AtmosphereQuality
    {
        /// <summary>Names of the quality levels in ProjectSettings, lowest first.</summary>
        public static readonly string[] LevelNames = { "Low", "Medium", "High" };

        /// <summary>The budget for a tier.</summary>
        public static TierBudget BudgetFor(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Low: return new TierBudget(2, 16, 12, false, false, false);
                case QualityTier.High: return new TierBudget(8, int.MaxValue, 32, true, true, true);
                default: return new TierBudget(4, 32, 24, true, false, false);
            }
        }

        /// <summary>The tier of the quality level Unity is using now.</summary>
        public static QualityTier Current => TierForLevelName(QualitySettings.names[QualitySettings.GetQualityLevel()]);

        /// <summary>The tier a quality level's name stands for. Unknown names count as Medium.</summary>
        public static QualityTier TierForLevelName(string levelName)
        {
            for (int i = 0; i < LevelNames.Length; i++)
            {
                if (string.Equals(LevelNames[i], levelName, System.StringComparison.OrdinalIgnoreCase))
                    return (QualityTier)i;
            }
            return QualityTier.Medium;
        }

        /// <summary>
        /// The tier a machine should start on: Low on a Steam Deck, Medium everywhere else. The
        /// Deck reports its APU as "AMD Custom GPU 0405" (LCD) or "0932" (OLED), and its board as
        /// "Jupiter" or "Galileo".
        /// </summary>
        public static QualityTier DefaultTierFor(string deviceModel, string graphicsDeviceName)
        {
            string model = deviceModel ?? string.Empty;
            string gpu = graphicsDeviceName ?? string.Empty;
            bool isDeck = model.IndexOf("Jupiter", System.StringComparison.OrdinalIgnoreCase) >= 0
                          || model.IndexOf("Galileo", System.StringComparison.OrdinalIgnoreCase) >= 0
                          || gpu.IndexOf("AMD Custom GPU 0405", System.StringComparison.OrdinalIgnoreCase) >= 0
                          || gpu.IndexOf("AMD Custom GPU 0932", System.StringComparison.OrdinalIgnoreCase) >= 0;
            return isDeck ? QualityTier.Low : QualityTier.Medium;
        }

        /// <summary>
        /// At startup: the level the player chose, or, until they choose, the machine's default (Low on a
        /// Steam Deck, Medium elsewhere). Runs before the first scene, so nothing renders on the wrong level.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedOrDefault()
        {
            string saved = PlayerPrefs.GetString(GraphicsLevelSettings.QualityKey, string.Empty);
            QualityTier tier = string.IsNullOrEmpty(saved)
                ? DefaultTierFor(SystemInfo.deviceModel, SystemInfo.graphicsDeviceName)
                : TierForLevelName(saved);
            Apply(tier);
            Debug.Log($"[Atmosphere] Graphics {LevelNames[(int)tier]} ({(string.IsNullOrEmpty(saved) ? "default for this machine" : "chosen in Settings")}).");
        }

        /// <summary>Switches Unity to the quality level for <paramref name="tier"/>, if it exists.</summary>
        public static void Apply(QualityTier tier)
        {
            string wanted = LevelNames[(int)tier];
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == wanted)
                {
                    QualitySettings.SetQualityLevel(i, true);
                    return;
                }
            }
            Debug.LogWarning($"[Atmosphere] No quality level named {wanted}; staying on {names[QualitySettings.GetQualityLevel()]}.");
        }
    }
}
