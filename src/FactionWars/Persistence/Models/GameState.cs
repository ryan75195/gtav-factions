using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Represents the complete state of the game that can be serialized/deserialized.
    /// Contains all factions, zones, faction states, and relationships.
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Current version of the save format for migration compatibility.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// User-defined name for this save.
        /// </summary>
        public string SaveName { get; set; } = "Unnamed Save";

        /// <summary>
        /// Timestamp when this save was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when this save was last modified.
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Total time played in this save in seconds.
        /// </summary>
        public long TotalPlayTimeSeconds { get; set; }

        /// <summary>
        /// All factions in the game.
        /// </summary>
        public List<FactionData> Factions { get; set; } = new List<FactionData>();

        /// <summary>
        /// Runtime state of each faction (resources, troops, etc.).
        /// </summary>
        public List<FactionStateData> FactionStates { get; set; } = new List<FactionStateData>();

        /// <summary>
        /// All zones in the game world.
        /// </summary>
        public List<ZoneData> Zones { get; set; } = new List<ZoneData>();

        /// <summary>
        /// Relationships between factions.
        /// </summary>
        public List<RelationshipData> Relationships { get; set; } = new List<RelationshipData>();

        /// <summary>
        /// Zone defender allocations (troop deployments to zones).
        /// </summary>
        public List<ZoneDefenderAllocationData> Allocations { get; set; } = new List<ZoneDefenderAllocationData>();

        /// <summary>
        /// Creates a new GameState with current timestamps.
        /// </summary>
        public GameState()
        {
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = CreatedAt;
        }

        /// <summary>
        /// Updates the ModifiedAt timestamp to the current time.
        /// </summary>
        public void MarkModified()
        {
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a GameState snapshot from the current game data.
        /// </summary>
        public static GameState CreateSnapshot(
            IEnumerable<Faction> factions,
            IEnumerable<FactionState> factionStates,
            IEnumerable<Zone> zones,
            IEnumerable<FactionRelationship> relationships,
            IEnumerable<ZoneDefenderAllocation>? allocations = null)
        {
            var gameState = new GameState();

            foreach (var faction in factions)
            {
                gameState.Factions.Add(FactionData.FromFaction(faction));
            }

            foreach (var state in factionStates)
            {
                gameState.FactionStates.Add(FactionStateData.FromFactionState(state));
            }

            foreach (var zone in zones)
            {
                gameState.Zones.Add(ZoneData.FromZone(zone));
            }

            foreach (var relationship in relationships)
            {
                gameState.Relationships.Add(RelationshipData.FromFactionRelationship(relationship));
            }

            if (allocations != null)
            {
                foreach (var allocation in allocations)
                {
                    gameState.Allocations.Add(ZoneDefenderAllocationData.FromAllocation(allocation));
                }
            }

            return gameState;
        }
    }
}
