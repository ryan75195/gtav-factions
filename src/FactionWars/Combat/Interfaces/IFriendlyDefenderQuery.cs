using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Read-only accessor over the defenders currently spawned in a zone. Implemented by
    /// FriendlyDefenderManager so DefenderRallyController can list defenders to rally
    /// without taking a hard reference to the concrete manager.
    /// </summary>
    public interface IFriendlyDefenderQuery
    {
        /// <summary>
        /// Returns the spawned defenders for the given zone, mapped from ped handle to tier.
        /// Returns an empty dictionary if the zone has no spawned defenders.
        /// </summary>
        IReadOnlyDictionary<int, DefenderTier> GetDefendersInZone(string zoneId);
    }
}
