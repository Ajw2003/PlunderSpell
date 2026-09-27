using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>What a piece of dressing is for.</summary>
    public enum DressingKind
    {
        /// <summary>Furnishes the bailey strip of a straight curtain-wall cell.</summary>
        Curtain = 0,

        /// <summary>Always beside the gatehouse: carts waiting to leave, the main brazier pair.</summary>
        GateYard = 1,

        /// <summary>Fills a courtyard cell carved out of the interior.</summary>
        Courtyard = 2,

        /// <summary>Laid over the gatehouse: portcullis down, gate barred.</summary>
        GateSealed = 3,
    }

    /// <summary>
    /// The outer bailey's furniture and the courtyards' yards (docs/plans/night-atmosphere.md,
    /// section 4), built by the asset pipeline (Tools/AssetPipeline/castle_builders_dressing.py)
    /// and placed by <see cref="CastleDressingPlanner"/>. Dressing is visual and cover: it never
    /// holds loot.
    /// </summary>
    [CreateAssetMenu(fileName = "CastleDressingSet", menuName = "Plunderspell/Castle/Dressing Set")]
    public class CastleDressingSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The pipeline key, e.g. DressingLeanTo.")]
            public string Id;
            public DressingKind Kind;
            [Tooltip("For a courtyard: the zone whose carved cells it may fill.")]
            public CastleZone Zone;
            public GameObject Prefab;
            [Tooltip("Relative chance among the entries it competes with.")]
            public int Weight = 1;
            [Tooltip("Written by Tools/Plunderspell/Import Castle Fire Anchors; do not edit by hand.")]
            public CastleFireAnchor[] FireAnchors = new CastleFireAnchor[0];
        }

        public List<Entry> Entries = new List<Entry>();

        /// <summary>The entry with this id, or null.</summary>
        public Entry GetById(string id)
        {
            foreach (Entry entry in Entries)
            {
                if (entry != null && entry.Id == id)
                    return entry;
            }
            return null;
        }
    }
}
