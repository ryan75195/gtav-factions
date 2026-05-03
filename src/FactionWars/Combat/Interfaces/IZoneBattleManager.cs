using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.Territory.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Unified manager for all battle lifecycle operations.
    /// Owns the entire battle lifecycle including spawning, simulation, and resolution.
    /// </summary>
    public interface IZoneBattleManager
    {
        #region State

        /// <summary>
        /// Gets the number of active battles.
        /// </summary>
        int BattleCount { get; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Starts a new battle in a zone.
        /// </summary>
        /// <param name="zoneId">The zone where the battle takes place.</param>
        /// <param name="attackerFactionId">The attacking faction.</param>
        /// <param name="defenderFactionId">The defending faction.</param>
        /// <param name="attackerTroops">Initial attacker troops by tier.</param>
        /// <param name="defenderTroops">Initial defender troops by tier.</param>
        /// <returns>The created battle.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a battle already exists in this zone.</exception>
        ZoneBattle StartBattle(
            string zoneId,
            string attackerFactionId,
            string defenderFactionId,
            Dictionary<DefenderTier, int> attackerTroops,
            Dictionary<DefenderTier, int> defenderTroops);

        /// <summary>
        /// Ends a battle in a zone.
        /// </summary>
        /// <param name="zoneId">The zone where the battle is ending.</param>
        /// <param name="outcome">The outcome of the battle.</param>
        void EndBattle(string zoneId, BattleOutcome outcome);

        #endregion

        #region Player Presence

        /// <summary>
        /// Called when the player enters a zone. Pauses tick simulation for this battle.
        /// </summary>
        void OnPlayerEnteredZone(Zone zone);

        /// <summary>
        /// Called when the player exits a zone. Resumes tick simulation for this battle.
        /// </summary>
        void OnPlayerExitedZone(Zone zone);

        /// <summary>
        /// Sets the player's faction ID.
        /// </summary>
        void SetPlayerFaction(string? factionId);

        #endregion

        #region Tick

        /// <summary>
        /// Updates all battles. Called every frame.
        /// Advances time and processes kills for battles where player is not present.
        /// </summary>
        /// <param name="deltaTime">Time since last tick in seconds.</param>
        void Tick(float deltaTime);

        #endregion

        #region Troop Reporting

        /// <summary>
        /// Reports that a troop was killed in physical combat (player present).
        /// </summary>
        /// <param name="zoneId">The zone where the kill occurred.</param>
        /// <param name="factionId">The faction that lost the troop.</param>
        /// <param name="tier">The tier of the killed troop.</param>
        void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier);

        #endregion

        #region Queries

        /// <summary>
        /// Gets the active battle in a zone, if any.
        /// </summary>
        ZoneBattle? GetBattleForZone(string zoneId);

        /// <summary>
        /// Gets all active battles.
        /// </summary>
        IReadOnlyList<ZoneBattle> GetAllActiveBattles();

        /// <summary>
        /// Adds an Attacker-role participant to an existing battle in the given zone.
        /// </summary>
        /// <param name="zoneId">The zone whose battle to modify.</param>
        /// <param name="factionId">The faction joining as attacker.</param>
        /// <param name="isPlayer">True if this is the player. v1 rejects non-player third parties.</param>
        /// <param name="aliveCountCallback">Required when isPlayer==true; ignored otherwise.</param>
        /// <param name="troops">Required when isPlayer==false; ignored otherwise.</param>
        /// <returns>
        /// True if the participant was added. False if no battle exists in the zone, the
        /// attacker cap (2) is reached, the faction is already a participant, or
        /// isPlayer==false (rejected in v1, Q2.A).
        /// </returns>
        bool JoinAsAttacker(
            string zoneId,
            string factionId,
            bool isPlayer,
            Func<int>? aliveCountCallback,
            Dictionary<DefenderTier, int>? troops);

        /// <summary>
        /// Returns true if the player is currently a participant in any battle.
        /// </summary>
        bool IsPlayerInBattle();

        /// <summary>
        /// Returns the battle the player is currently a participant in, or null if none.
        /// </summary>
        ZoneBattle? GetPlayerCurrentBattle();

        #endregion

        #region Events

        /// <summary>
        /// Raised when a new battle starts.
        /// </summary>
        event Action<ZoneBattle>? BattleStarted;

        /// <summary>
        /// Raised when a battle ends.
        /// </summary>
        event Action<ZoneBattle, BattleOutcome>? BattleEnded;

        /// <summary>
        /// Raised when a troop is killed in battle.
        /// Parameters: battle, tier of killed troop, side ("attacker" or "defender")
        /// </summary>
        event Action<ZoneBattle, DefenderTier, string>? TroopKilled;

        #endregion
    }
}
