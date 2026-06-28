using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Telemetry.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Flattens the per-zone (pedHandle -&gt; role) tracking dictionary shared by the zone defender and
    /// battle attacker managers into the flat <see cref="TrackedCombatant"/> list the behavior sampler
    /// consumes. Keeps the three managers' source implementations DRY.
    /// </summary>
    public static class TrackedCombatantProjection
    {
        public static IReadOnlyList<TrackedCombatant> FromTierMap(
            IReadOnlyDictionary<string, Dictionary<int, DefenderRole>> tierByZone,
            CombatantKind kind)
        {
            var result = new List<TrackedCombatant>();
            foreach (var zone in tierByZone.Values)
            {
                foreach (var pair in zone)
                {
                    result.Add(new TrackedCombatant(pair.Key, kind, pair.Value));
                }
            }

            return result;
        }
    }
}
