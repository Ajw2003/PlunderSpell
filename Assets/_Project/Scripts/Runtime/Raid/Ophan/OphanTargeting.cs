using System.Collections.Generic;
using System.Linq;

namespace Plunderspell.Raid
{
    /// <summary>One player as the Ophan weighs them.</summary>
    public readonly struct OphanCandidate<T>
    {
        public readonly T Player;
        public readonly float Worth;
        public readonly bool Alive;

        public OphanCandidate(T player, float worth, bool alive)
        {
            Player = player;
            Worth = worth;
            Alive = alive;
        }
    }

    /// <summary>
    /// Who the Ophan's eye fixes on: the living player carrying the most worth, then the next down the list (approved
    /// 2026-10-27, pick 3A: worth is the sell value of the piece they carry). Ties keep the order the players were given,
    /// so every call with the same players picks the same one.
    /// </summary>
    public static class OphanTargeting
    {
        public static bool TryPick<T>(IEnumerable<OphanCandidate<T>> players, out T target)
        {
            foreach (OphanCandidate<T> candidate in players.Where(c => c.Alive).OrderByDescending(c => c.Worth))
            {
                target = candidate.Player;
                return true;
            }
            target = default;
            return false;
        }
    }
}
