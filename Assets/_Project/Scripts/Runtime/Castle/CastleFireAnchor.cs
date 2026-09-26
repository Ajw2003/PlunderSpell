using System;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>The kinds of fire a castle burns (docs/plans/night-atmosphere.md, section 2).</summary>
    public enum FireKind
    {
        /// <summary>A torch in a wall bracket.</summary>
        Sconce = 0,

        /// <summary>A free-standing iron bowl on a pole, in yards and halls.</summary>
        Brazier = 1,

        /// <summary>A fireplace, forge or kitchen fire: always lit.</summary>
        Hearth = 2,

        /// <summary>A big fire on a wall or tower, lit when the castle is roused.</summary>
        Beacon = 3,
    }

    /// <summary>
    /// Where a module burns a fire, authored by the asset pipeline beside its loot anchors
    /// (Assets/_Project/Data/Castle/CastleFireAnchors.json) and imported into
    /// <see cref="CastleRoomModuleData.FireAnchors"/>.
    /// </summary>
    [Serializable]
    public struct CastleFireAnchor
    {
        [Tooltip("Offset from the module's position before its placement rotation, like a loot anchor. " +
                 "The fire's base: the floor under a brazier, the wall bracket of a sconce.")]
        public Vector3 Position;

        [Tooltip("Which way the fire faces, in degrees about Y before placement rotation. A sconce " +
                 "hangs on a wall and faces away from it.")]
        public float Yaw;

        public FireKind Kind;

        [Tooltip("The alarm state that first lights it: 0 Calm, 1 Stirred, 2 Roused, 3 HueAndCry. " +
                 "An int so the castle assembly need not know the alarm's.")]
        public int LitFrom;

        [Tooltip("True when the fire brings its own iron (bracket, bowl, basket). False when the room " +
                 "already models it and only the flame is wanted; Position is then the flame's base.")]
        public bool BringsHolder;

        public CastleFireAnchor(Vector3 position, float yaw, FireKind kind, int litFrom, bool bringsHolder = true)
        {
            Position = position;
            Yaw = yaw;
            Kind = kind;
            LitFrom = litFrom;
            BringsHolder = bringsHolder;
        }
    }
}
