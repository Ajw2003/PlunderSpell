namespace Plunderspell.Inventory
{
    /// <summary>
    /// The historical eras a raid can be set in and that loot can be acquired from.
    /// Ordered chronologically; the ordinal is only used for display/sorting — it does NOT
    /// gate equipping (see <see cref="InventoryItem.CanEquipInEra"/>: cross-era is always allowed).
    /// </summary>
    public enum HistoricalEra
    {
        BronzeAge = 0,      // bows, bronze swords, shields, slings, spears
        HighMedieval = 1,   // crossbows, longswords, plate armour, war hammers
        LateMedieval = 2,   // early firearms (matchlock), halberds, pavise shields
        AgeOfPowder = 3     // flintlock pistols, muskets, early grenades
    }
}
