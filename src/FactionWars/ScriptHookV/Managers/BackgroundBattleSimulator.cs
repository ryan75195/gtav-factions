using System;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Handles background simulation of AI vs AI battles when the player is not present.
    /// Listens for AI attack decisions and simulates the combat using the battle simulation service.
    /// </summary>
    public class BackgroundBattleSimulator
    {
        private readonly IBattleSimulationService _battleSimulationService;
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IZoneDefenderAllocationService _allocationService;
        private string? _currentPlayerZone;

        /// <summary>
        /// Gets the current zone where the player is located.
        /// </summary>
        public string? CurrentPlayerZone => _currentPlayerZone;

        /// <summary>
        /// Raised when a background battle is completed.
        /// </summary>
        public event EventHandler<BattleSimulationResult>? OnBattleCompleted;

        /// <summary>
        /// Creates a new BackgroundBattleSimulator.
        /// </summary>
        /// <param name="battleSimulationService">The service for simulating battles.</param>
        /// <param name="factionService">The service for faction management.</param>
        /// <param name="zoneService">The service for zone management.</param>
        /// <param name="allocationService">The service for defender allocation.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public BackgroundBattleSimulator(
            IBattleSimulationService battleSimulationService,
            IFactionService factionService,
            IZoneService zoneService,
            IZoneDefenderAllocationService allocationService)
        {
            _battleSimulationService = battleSimulationService ?? throw new ArgumentNullException(nameof(battleSimulationService));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
        }

        /// <summary>
        /// Sets the current zone where the player is located.
        /// Battles will not be simulated in this zone (player handles combat directly).
        /// </summary>
        /// <param name="zoneId">The zone ID where the player is, or null if not in any zone.</param>
        public void SetPlayerZone(string? zoneId)
        {
            _currentPlayerZone = zoneId;
        }

        /// <summary>
        /// Handles AI decision events from the AIManager.
        /// Processes attack decisions by simulating battles.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The AI decision event arguments.</param>
        public void HandleAIDecision(object? sender, AIDecisionEventArgs args)
        {
            if (args == null)
                return;

            ProcessAttackDecision(args.FactionId, args.Decision);
        }

        /// <summary>
        /// Processes an attack decision by simulating the battle and applying results.
        /// </summary>
        /// <param name="attackerFactionId">The faction ID of the attacker.</param>
        /// <param name="decision">The AI decision to process.</param>
        /// <returns>The battle result, or null if the battle was not processed.</returns>
        public BattleSimulationResult? ProcessAttackDecision(string attackerFactionId, AIDecision decision)
        {
            // Only process attack decisions
            if (decision.DecisionType != AIDecisionType.Attack)
                return null;

            // Must have a target zone
            if (decision.TargetZoneId == null)
                return null;

            // Don't process if player is in the target zone
            if (_currentPlayerZone != null && _currentPlayerZone == decision.TargetZoneId)
                return null;

            // Get the target zone
            var zone = _zoneService.GetZone(decision.TargetZoneId);
            if (zone == null)
                return null;

            // Zone must have an owner
            if (zone.OwnerFactionId == null)
                return null;

            // Can't attack own zone
            if (zone.OwnerFactionId == attackerFactionId)
                return null;

            var defenderFactionId = zone.OwnerFactionId;

            // Build attacker troop composition from decision
            var attackerTroops = BuildAttackerTroops(decision.TroopsToCommit);

            // Build defender troop composition from zone allocation
            var defenderTroops = BuildDefenderTroops(defenderFactionId, decision.TargetZoneId);

            // Simulate the battle
            var result = _battleSimulationService.SimulateBattle(
                attackerFactionId,
                defenderFactionId,
                decision.TargetZoneId,
                attackerTroops,
                defenderTroops);

            // Apply the results
            ApplyBattleResult(result);

            // Raise the event
            OnBattleCompleted?.Invoke(this, result);

            return result;
        }

        /// <summary>
        /// Builds attacker troop composition from the number of troops committed.
        /// Default distribution is all basic troops.
        /// </summary>
        private static TroopComposition BuildAttackerTroops(int troopsToCommit)
        {
            // For now, all committed troops are basic
            // Future: could distribute based on faction's reserve composition
            return new TroopComposition(troopsToCommit, 0, 0);
        }

        /// <summary>
        /// Builds defender troop composition from the zone's defender allocation.
        /// </summary>
        private TroopComposition BuildDefenderTroops(string defenderFactionId, string zoneId)
        {
            var allocation = _allocationService.GetAllocation(defenderFactionId, zoneId);
            if (allocation == null)
                return TroopComposition.Empty;

            return new TroopComposition(
                allocation.GetTroopCount(DefenderTier.Basic),
                allocation.GetTroopCount(DefenderTier.Medium),
                allocation.GetTroopCount(DefenderTier.Heavy));
        }

        /// <summary>
        /// Applies the battle result to the game state.
        /// </summary>
        private void ApplyBattleResult(BattleSimulationResult result)
        {
            // Apply attacker casualties
            int attackerCasualtiesCount = result.AttackerCasualties.TotalCount;
            if (attackerCasualtiesCount > 0)
            {
                _factionService.LoseTroops(result.AttackerFactionId, attackerCasualtiesCount);
            }

            // Apply defender casualties
            int defenderCasualtiesCount = result.DefenderCasualties.TotalCount;
            if (defenderCasualtiesCount > 0)
            {
                _factionService.LoseTroops(result.DefenderFactionId, defenderCasualtiesCount);
            }

            // Transfer zone ownership if attacker won
            if (result.AttackerWon)
            {
                _zoneService.TransferZoneOwnership(result.ZoneId, result.AttackerFactionId);
            }
        }
    }
}
