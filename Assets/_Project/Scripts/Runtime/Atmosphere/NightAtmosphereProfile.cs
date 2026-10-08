using System;
using Plunderspell.Alarm;
using Plunderspell.Inventory;
using UnityEngine;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// The castle's night in its four alarm states (docs/plans/night-atmosphere.md, section 2):
    /// calm and asleep, then warmer, brighter and redder as the alarm climbs.
    /// </summary>
    [CreateAssetMenu(menuName = "Plunderspell/Night Atmosphere Profile", fileName = "NightAtmosphere")]
    public class NightAtmosphereProfile : ScriptableObject
    {
        [Header("Overall")]
        [Tooltip("One dial for all the night mist. 1 is the mist as tuned below; 2 is twice as thick, 0.5 half, " +
                 "0 no mist at all. Try 0.5 to 2.")]
        [Range(0f, 4f)] public float FogAmount = 1f;
        [Tooltip("One dial for the glow of fire in the mist (the soft halo round every flame). 1 is as tuned " +
                 "below; 0 removes the halos, 2 doubles them. Try 0 to 2.")]
        [Range(0f, 4f)] public float FireGlowAmount = 1f;
        [Tooltip("Seconds every part of a fire takes to fade in or out: its light, shadow, flame, colour, embers and " +
                 "fog halo. Nothing about a fire pops; it all eases over this time. Try 0.5 to 2.")]
        public float FireFadeSeconds = 1f;
        [Tooltip("Seconds the whole look takes to change when the alarm goes up or down. Short is abrupt, " +
                 "long is gradual. Try 1 to 4.")]
        public float TransitionSeconds = 2f;

        [Header("Looks per alarm state (fog, moon, ambient light, sky, fire, colour grade)")]
        [Tooltip("The quiet night, before anyone has noticed anything.")]
        public AtmosphereLook Calm;
        [Tooltip("The first stirrings: a guard has heard something.")]
        public AtmosphereLook Stirred;
        [Tooltip("The castle is awake and searching.")]
        public AtmosphereLook Roused;
        [Tooltip("Full alarm: the garrison is hunting you.")]
        public AtmosphereLook HueAndCry;

        /// <summary>What shifts from one Age to the next: only the stone and the flame (section 1, rule 6).</summary>
        [Serializable]
        public struct EraTint
        {
            [Tooltip("Colour the stone is multiplied by at night. Darker makes the walls gloomier, so fire stands " +
                     "out against them; a warm tint gives a brown cast, a cool one a blue cast. White changes nothing.")]
            public Color Stone;
            [Tooltip("Colour the flame is multiplied by in this Age. White changes nothing; orange-red makes " +
                     "the fires redder.")]
            public Color Flame;
        }

        [Header("Per-Age tints (stone and flame for each Age)")]
        [Tooltip("Stone and flame colour for the Bronze Age.")]
        public EraTint BronzeAge;
        [Tooltip("Stone and flame colour for the High Medieval Age.")]
        public EraTint HighMedieval;
        [Tooltip("Stone and flame colour for the Late Medieval Age.")]
        public EraTint LateMedieval;
        [Tooltip("Stone and flame colour for the Age of Powder.")]
        public EraTint AgeOfPowder;

        [Header("Painted look: light bands")]
        [Tooltip("How much firelight snaps into flat bands of colour, like a cartoon. 0 keeps each material's own " +
                 "setting, 1 is fully banded.")]
        [Range(0f, 1f)] public float CelAmount = 1f;
        [Tooltip("How soft the edges between the light bands are. Lower is crisper, higher is smoother. " +
                 "0 keeps each material's own setting. Try 0.02 to 0.15.")]
        [Range(0f, 0.5f)] public float CelSoftness = 0.04f;

        [Header("Painted look: pen hatching in the shadows")]
        [Tooltip("How dark the pen strokes in shadow get. 0 turns hatching off, 1 is full black ink. Off by " +
                 "default: the owner asked for less hatching and more cel shading (2026-10-06).")]
        [Range(0f, 1f)] public float HatchStrength = 0f;
        [Tooltip("How many pen strokes fit in a metre of wall. Higher is finer and busier. Try 5 to 15.")]
        public float HatchLinesPerMetre = 9f;
        [Tooltip("How dark a surface must be before strokes appear. 0 never, 1 everywhere. The second, crossing " +
                 "layer of strokes starts at half this.")]
        [Range(0f, 1f)] public float HatchStart = 0.35f;

        [Header("Painted look: ink outlines")]
        [Tooltip("How dark the ink lines are where a surface turns away from you and in crevices. 0 turns them " +
                 "off, 1 is nearly black.")]
        [Range(0f, 1f)] public float OutlineStrength = 0.35f;
        [Tooltip("How black the ink gets at its darkest. 1 is pitch black, 0.5 leaves half the colour showing " +
                 "through. Only matters while the outline strength above is more than 0.")]
        [Range(0f, 1f)] public float InkDarkness = 0.85f;
        [Tooltip("Where on a curved surface the outline begins, as how side-on it faces you (0 face-on, 1 edge-on). " +
                 "Lower puts ink further in from the edge, so lines look thicker. Keep it below the next value.")]
        [Range(0f, 1f)] public float OutlineEdgeStart = 0.62f;
        [Tooltip("Where the outline reaches full strength, same scale as above. The gap between the two sets how " +
                 "soft the line is: a small gap is a crisp line.")]
        [Range(0f, 1f)] public float OutlineEdgeEnd = 0.72f;
        [Tooltip("How curved a surface must be to get an outline. Higher gives ink on gentler curves; lower " +
                 "keeps it to sharp corners. Flat walls never get an edge line. Try 3 to 15.")]
        public float OutlineCurvatureGain = 8f;
        [Tooltip("How dirty a crevice must be before it gets an ink line, start of the fade (0 clean, 1 sooty). " +
                 "Should be higher than the end value below.")]
        [Range(0f, 1f)] public float CreviceInkStart = 0.75f;
        [Tooltip("The dirtiness at which a crevice's ink line is at full strength. Raise it so only the sootiest " +
                 "cracks get inked, lower it for ink in more cracks.")]
        [Range(0f, 1f)] public float CreviceInkEnd = 0.45f;

        [Header("Painted look: paint grain")]
        [Tooltip("How much the paint grain and blotches darken the walls. 0 turns them off, 0.7 is heavy and " +
                 "mottled.")]
        [Range(0f, 0.7f)] public float PaperGrain = 0.14f;
        [Tooltip("Size of the large blotches in the paint: how many repeat in a metre. Bigger numbers give smaller, " +
                 "busier blotches. Try 0.3 to 2.")]
        public float BlotchScale = 0.9f;
        [Tooltip("Size of the second, smaller layer of blotches mixed in: how many repeat in a metre. Bigger " +
                 "numbers give smaller blotches. Try 1 to 5.")]
        public float SmallBlotchScale = 2.3f;
        [Tooltip("Where the light-to-dark change of a blotch starts. Lower makes more of the wall dark. " +
                 "Keep it below the next value.")]
        [Range(0f, 1f)] public float BlotchContrastStart = 0.42f;
        [Tooltip("Where a blotch becomes fully dark. The gap between this and the start value is how soft the " +
                 "blotch edges are: a small gap is hard-edged patches.")]
        [Range(0f, 1f)] public float BlotchContrastEnd = 0.56f;
        [Tooltip("Size of the fine fibre grain: how many specks fit in a metre. Bigger numbers give finer grain. " +
                 "Very fine grain fades out at a distance. Try 10 to 60.")]
        public float FineGrainScale = 24f;

        [Header("Painted look: soot at the foot of walls")]
        [Tooltip("How much extra soot darkens the bottom of walls. 0 leaves only each material's own grime, " +
                 "0.8 is very black.")]
        [Range(0f, 0.8f)] public float SootStrength = 0.3f;
        [Tooltip("Height above the floor, in metres, where the soot is at its darkest. Higher lifts the whole " +
                 "soot band.")]
        public float SootStartHeight = 0.3f;
        [Tooltip("How many metres above that height the soot takes to fade out completely. Bigger makes soot " +
                 "climb higher up the wall. Try 0.5 to 3.")]
        public float SootFadeHeight = 1.6f;

        [Header("Fire and sky details")]
        [Tooltip("How soft the middle of a fire's glow is, in metres. A tiny value gives a hot white spot; larger " +
                 "gives a softer, flatter glow. Try 0.2 to 1.5.")]
        public float FireHaloSoftness = 0.6f;
        [Tooltip("How quickly the mist clears as you look up. Low (0.3) clears it fast so most of the sky is " +
                 "clear; high (1.5) keeps it misty until you look straight up. Try 0.3 to 1.5.")]
        public float SkyClarityCurve = 0.6f;
        [Tooltip("How much of a fire's glow still shows through a wall or other solid thing between you and it. " +
                 "0 hides it completely, 1 lets it shine through as if there were no wall.")]
        [Range(0f, 1f)] public float OccludedGlow = 0.2f;

        /// <summary>The tints for one Age.</summary>
        public EraTint ForEra(HistoricalEra era)
        {
            switch (era)
            {
                case HistoricalEra.BronzeAge: return BronzeAge;
                case HistoricalEra.LateMedieval: return LateMedieval;
                case HistoricalEra.AgeOfPowder: return AgeOfPowder;
                default: return HighMedieval;
            }
        }

        /// <summary>The look for one alarm state.</summary>
        public AtmosphereLook For(AlarmState state)
        {
            switch (state)
            {
                case AlarmState.Stirred: return Stirred;
                case AlarmState.Roused: return Roused;
                case AlarmState.HueAndCry: return HueAndCry;
                default: return Calm;
            }
        }
    }
}
