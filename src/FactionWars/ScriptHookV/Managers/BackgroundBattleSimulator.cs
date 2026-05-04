using System;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

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
        private readonly IEventAlertService _eventAlertService;
        private readonly IEventFeedService _eventFeedService;
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
        /// <param name="eventAlertService">The service for raising event alerts.</param>
        /// <param name="eventFeedService">The service for the event feed.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public BackgroundBattleSimulator(BackgroundBattleSimulatorDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _battleSimulationService = dependencies.BattleSimulationService ?? throw new ArgumentNullException(nameof(dependencies.BattleSimulationService));
            _factionService = dependencies.FactionService ?? throw new ArgumentNullException(nameof(dependencies.FactionService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _allocationService = dependencies.AllocationService ?? throw new ArgumentNullException(nameof(dependencies.AllocationService));
            _eventAlertService = dependencies.EventAlertService ?? throw new ArgumentNullException(nameof(dependencies.EventAlertService));
            _eventFeedService = dependencies.EventFeedService ?? throw new ArgumentNullException(nameof(dependencies.EventFeedService));
        }

        public BackgroundBattleSimulator(params object?[] dependencies)
            : this(new BackgroundBattleSimulatorDependencies
            {
                BattleSimulationService = (IBattleSimulationService?)dependencies[0],
                FactionService = (IFactionService?)dependencies[1],
                ZoneService = (IZoneService?)dependencies[2],
                AllocationService = (IZoneDefenderAllocationService?)dependencies[3],
                EventAlertService = (IEventAlertService?)dependencies[4],
                EventFeedService = (IEventFeedService?)dependencies[5]
            })
        {
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

            // Can't attack own zone
            if (zone.OwnerFactionId == attackerFactionId)
                return null;

            var attackerFaction = _factionService.GetFaction(attackerFactionId);
            var attackerFactionName = attackerFaction?.Name ?? attackerFactionId;

            // Handle neutral zone capture (no battle needed)
            if (zone.OwnerFactionId == null)
            {
                // Neutral zone - automatic capture with no combat
                _zoneService.TransferZoneOwnership(decision.TargetZoneId, attackerFactionId);

                // Allocate defenders to the newly captured zone
                // Always allocate at least 1 defender to prevent "owned with 0 defenders" state
                int defendersToAllocate = decision.TroopsToCommit > 0 ? Math.Max(1, Math.Min((decision.TroopsToCommit + 1) / 2, 5)) : 0;
                if (defendersToAllocate > 0)
                {
                    _allocationService.SetAllocation(attackerFactionId, decision.TargetZoneId, DefenderTier.Basic, defendersToAllocate);
                }

                // Notify UI of capture
                _eventFeedService.AddZoneCaptured(zone.Name, attackerFactionName);
                _eventAlertService.RaiseZoneCaptured(zone.Name, attackerFactionName);

                // Create a "won" result for neutral capture (no casualties, no real defender)
                var neutralResult = BattleSimulationResult.AttackerVictory(
                    attackerFactionId,
                    "neutral",
                    decision.TargetZoneId,
                    TroopComposition.Empty,
                    TroopComposition.Empty);

                OnBattleCompleted?.Invoke(this, neutralResult);
                return neutralResult;
            }

            var defenderFactionId = zone.OwnerFactionId;
            var defenderFactionName = _factionService.GetFaction(defenderFactionId)?.Name ?? defenderFactionId;

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
            ApplyBattleResult(result, decision.TroopsToCommit);

            // Notify UI of battle result
            if (result.AttackerWon)
            {
                _eventFeedService.AddZoneCaptured(zone.Name, attackerFactionName);
                _eventAlertService.RaiseZoneCaptured(zone.Name, attackerFactionName);
            }
            else
            {
                _eventFeedService.AddCombatEnded(zone.Name, defenderFactionName, defenderWon: true);
            }

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
        /// <param name="result">The battle simulation result.</param>
        /// <param name="attackingTroops">The number of attacking troops committed to the battle.</param>
        private void ApplyBattleResult(BattleSimulationResult result, int attackingTroops)
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

                // Allocate surviving troops as defenders
                // Always allocate at least 1 defender to prevent "owned with 0 defenders" state
                int survivors = attackingTroops - attackerCasualtiesCount;
                int defendersToAllocate = survivors > 0 ? Math.Max(1, Math.Min((survivors + 1) / 2, 5)) : 0;
                if (defendersToAllocate > 0)
                {
                    _allocationService.SetAllocation(result.AttackerFactionId, result.ZoneId, DefenderTier.Basic, defendersToAllocate);
                }
            }
        }
    }
}
