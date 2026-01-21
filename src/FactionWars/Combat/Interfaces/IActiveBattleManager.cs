using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Manages active timed battles between AI factions.
    /// </summary>
    public interface IActiveBattleManager
    {
        /// <summary>
        /// Gets all currently active battles.
        /// </summary>
        IReadOnlyList<ActiveBattle> ActiveBattles { get; }

        /// <summary>
        /// Gets the number of active battles.
        /// </summary>
        int BattleCount { get; }

        /// <summary>
        /// Starts a new timed battle.
        /// </summary>
        ActiveBattle StartBattle(
            string attackerFactionId,
            string defenderFactionId,
            string zoneId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops);

        /// <summary>
        /// Gets the battle for a specific zone, if any.
        /// </summary>
        ActiveBattle? GetBattleForZone(string zoneId);

        /// <summary>
        /// Gets a battle by its ID.
        /// </summary>
        ActiveBattle? GetBattle(string battleId);

        /// <summary>
        /// Updates all active battles. Should be called each frame.
        /// </summary>
        void Tick(float deltaTimeSeconds);

        /// <summary>
        /// Called when the player enters a zone with an active battle.
        /// Pauses tick-based simulation for that battle.
        /// </summary>
        void OnPlayerEnterZone(string zoneId);

        /// <summary>
        /// Called when the player exits a zone with an active battle.
        /// Resumes tick-based simulation.
        /// </summary>
        void OnPlayerExitZone(string zoneId);

        /// <summary>
        /// Reports that a troop was killed by the player or physical combat.
        /// Used when IsPlayerPresent is true.
        /// </summary>
        void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier);

        /// <summary>
        /// Adds defender troops to an active battle in the specified zone.
        /// Used when player allocates reinforcements during active battle.
        /// </summary>
        /// <param name="zoneId">The zone with the active battle.</param>
        /// <param name="tier">The tier of troops to add.</param>
        /// <param name="count">The number of troops to add.</param>
        /// <returns>True if troops were added to an existing battle, false if no battle exists.</returns>
        bool AddDefenderTroops(string zoneId, DefenderTier tier, int count);

        /// <summary>
        /// Raised when a kill occurs in a battle.
        /// </summary>
        event EventHandler<BattleKillEvent>? OnKill;

        /// <summary>
        /// Raised when a battle ends.
        /// </summary>
        event EventHandler<BattleEndedEvent>? OnBattleEnded;
    }
}
