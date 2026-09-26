using PurrNet;

namespace Plunderspell.Inventory
{
    /// <summary>
    /// Names this assembly's replicated types for PurrNet. PurrNet only generates a serializer inside
    /// the assembly that declares a type, and only for types that assembly itself sends; nothing here
    /// is a network type, so <see cref="HistoricalEra"/> (replicated by <c>RaidDirector</c>) has to be
    /// registered by name or it fails to pack.
    /// </summary>
    [RegisterNetworkType(typeof(HistoricalEra))]
    internal static class NetworkTypes
    {
    }
}
