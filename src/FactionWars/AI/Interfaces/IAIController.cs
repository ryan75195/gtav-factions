using System;
using FactionWars.AI.Events;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Consolidated AI controller interface.
    /// Manages all AI faction behavior including decisions, recruitment, and battle simulation.
    /// Implementations can be swapped to change AI behavior.
    /// </summary>
    public interface IAIController
    {
        /// <summary>
        /// Gets whether the AI controller is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts the AI controller. AI factions will begin making decisions.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the AI controller. AI factions will stop making decisions.
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates the AI controller. Should be called each frame.
        /// Handles all AI timing internally (decisions, recruitment, etc.)
        /// </summary>
        /// <param name="deltaTimeSeconds">Time elapsed since last update in seconds.</param>
        void Update(float deltaTimeSeconds);

        /// <summary>
        /// Sets the player's faction ID. This faction will be excluded from AI control.
        /// </summary>
        /// <param name="factionId">The player's faction ID, or null to clear.</param>
        void SetPlayerFactionId(string? factionId);

        /// <summary>
        /// Gets the player's faction ID.
        /// </summary>
        string? PlayerFactionId { get; }

        /// <summary>
        /// Sets the current zone where the player is located.
        /// Battles will not be simulated in this zone.
        /// </summary>
        /// <param name="zoneId">The zone ID, or null if not in any zone.</param>
        void SetPlayerZone(string? zoneId);

        /// <summary>
        /// Gets the current zone where the player is located.
        /// </summary>
        string? PlayerZoneId { get; }

        /// <summary>
        /// Raised when an AI faction starts an attack on a zone.
        /// </summary>
        event EventHandler<AIAttackEventArgs>? OnAttackStarted;

        /// <summary>
        /// Raised when an AI vs AI battle is resolved.
        /// </summary>
        event EventHandler<AIBattleResultEventArgs>? OnBattleResolved;

        /// <summary>
        /// Raised after a successful recruitment cycle for a single AI faction.
        /// Args expose cash before/after and troops recruited so telemetry can record
        /// a complete recruitment row. Not raised when zero troops were recruited.
        /// </summary>
        event EventHandler<TroopsRecruitedEventArgs>? OnTroopsRecruited;
    }
}
