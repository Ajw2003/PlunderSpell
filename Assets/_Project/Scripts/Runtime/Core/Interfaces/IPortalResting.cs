namespace Interfaces
{
    /// <summary>
    /// Something the extraction portal can hold safe (#158): loot put down inside the portal stops
    /// moving and cannot break, so the haul is not kicked about or shattered by feet and spells.
    /// The portal calls this as a piece enters and leaves it. See docs/4-systems/raid.md, "Loot in
    /// the portal".
    /// </summary>
    public interface IPortalResting
    {
        /// <summary>True while the piece is inside the portal.</summary>
        void SetInPortal(bool inPortal);
    }
}
