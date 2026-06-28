using System.Collections.Generic;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FriendlyDefenderManager : ITrackedCombatantSource
    {
        /// <summary>
        /// Exposes the currently spawned friendly defenders (handle + role) for behavior sampling.
        /// Reads the existing per-zone tracking dictionary; takes no new ownership.
        /// </summary>
        IReadOnlyList<TrackedCombatant> ITrackedCombatantSource.GetTrackedCombatants()
            => TrackedCombatantProjection.FromTierMap(_spawnedPedTierByZone, CombatantKind.FriendlyDefender);
    }
}
