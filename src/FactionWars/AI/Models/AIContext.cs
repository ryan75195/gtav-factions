using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Models
{
    /// <summary>
    /// Provides all the context an AI strategy needs to make decisions.
    /// Contains information about the faction, its state, and the game world.
    /// </summary>
    public class AIContext
    {
        /// <summary>
        /// The faction this AI is making decisions for.
        /// </summary>
        public Faction Faction { get; }

        /// <summary>
        /// The current state of the faction (resources, troops, etc.).
        /// </summary>
        public FactionState FactionState { get; }

        /// <summary>
        /// Zones currently owned by this faction.
        /// </summary>
        public IReadOnlyList<Zone> OwnedZones { get; }

        /// <summary>
        /// All zones in the game world.
        /// </summary>
        public IReadOnlyList<Zone> AllZones { get; }

        /// <summary>
        /// All enemy factions (factions other than this one).
        /// </summary>
        public IReadOnlyList<Faction> EnemyFactions { get; }

        /// <summary>
        /// Creates a new AI context with the specified information.
        /// </summary>
        /// <param name="faction">The faction making decisions.</param>
        /// <param name="factionState">The faction's current state.</param>
        /// <param name="ownedZones">Zones owned by the faction.</param>
        /// <param name="allZones">All zones in the game.</param>
        /// <param name="enemyFactions">Enemy factions.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public AIContext(
            Faction faction,
            FactionState factionState,
            IEnumerable<Zone> ownedZones,
            IEnumerable<Zone> allZones,
            IEnumerable<Faction> enemyFactions)
        {
            Faction = faction ?? throw new ArgumentNullException(nameof(faction));
            FactionState = factionState ?? throw new ArgumentNullException(nameof(factionState));

            if (ownedZones == null)
                throw new ArgumentNullException(nameof(ownedZones));
            if (allZones == null)
                throw new ArgumentNullException(nameof(allZones));
            if (enemyFactions == null)
                throw new ArgumentNullException(nameof(enemyFactions));

            OwnedZones = ownedZones.ToList().AsReadOnly();
            AllZones = allZones.ToList().AsReadOnly();
            EnemyFactions = enemyFactions.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all zones not owned by this faction.
        /// </summary>
        public IEnumerable<Zone> GetNonOwnedZones()
        {
            return AllZones.Where(z => z.OwnerFactionId != Faction.Id);
        }

        /// <summary>
        /// Gets all zones owned by enemy factions.
        /// </summary>
        public IEnumerable<Zone> GetEnemyZones()
        {
            var enemyIds = EnemyFactions.Select(f => f.Id).ToHashSet();
            return AllZones.Where(z => z.OwnerFactionId != null && enemyIds.Contains(z.OwnerFactionId));
        }

        /// <summary>
        /// Gets all neutral zones (not owned by any faction).
        /// </summary>
        public IEnumerable<Zone> GetNeutralZones()
        {
            return AllZones.Where(z => z.OwnerFactionId == null);
        }

        /// <summary>
        /// Gets owned zones that are currently contested.
        /// </summary>
        public IEnumerable<Zone> GetThreatenedZones()
        {
            return OwnedZones.Where(z => z.IsContested);
        }
    }
}
