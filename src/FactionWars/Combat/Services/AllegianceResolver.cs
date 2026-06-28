using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Utils;

namespace FactionWars.Combat.Services
{
    /// <inheritdoc />
    public class AllegianceResolver : IAllegianceResolver
    {
        public CombatantProfile Resolve(string combatantFactionId, string playerFactionId)
        {
            if (combatantFactionId == null) throw new ArgumentNullException(nameof(combatantFactionId));

            var group = combatantFactionId.ToUpperInvariant();
            var blipColor = FactionBlipColor.ForFactionId(combatantFactionId);
            var allegiance = string.Equals(combatantFactionId, playerFactionId, StringComparison.OrdinalIgnoreCase)
                ? Allegiance.Friendly
                : Allegiance.Hostile;

            return new CombatantProfile(group, blipColor, allegiance);
        }
    }
}
