using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.AI.Controllers
{
    /// <summary>
    /// Consolidated AI controller that manages all AI faction behavior.
    /// Handles decision-making, recruitment, budget enforcement, and battle simulation.
    /// </summary>
    public class AIController : IAIController
    {
        // Dependencies
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IBattleSimulationService _battleSimulationService;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IEventFeedService _eventFeedService;
        private readonly IDictionary<string, IAIStrategy> _strategies;

        // Configuration
        private const float DefaultDecisionIntervalSeconds = 30f;
        private const float DefaultRecruitmentIntervalSeconds = 60f;
        private const int RecruitCostPerTroop = 100;
        private const int AttackCostPerTroop = 50;
        private const int MaxRecruitPerCycle = 5;

        // State
        private bool _isRunning;
        private string? _playerFactionId;
        private string? _playerZoneId;
        private float _decisionTimer;
        private float _recruitmentTimer;

        /// <inheritdoc />
        public bool IsRunning => _isRunning;

        /// <inheritdoc />
        public string? PlayerFactionId => _playerFactionId;

        /// <inheritdoc />
        public string? PlayerZoneId => _playerZoneId;

        /// <inheritdoc />
        public event EventHandler<AIAttackEventArgs>? OnAttackStarted;

        /// <inheritdoc />
        public event EventHandler<AIBattleResultEventArgs>? OnBattleResolved;

        /// <summary>
        /// Creates a new AIController.
        /// </summary>
        public AIController(
            IFactionService factionService,
            IZoneService zoneService,
            IBattleSimulationService battleSimulationService,
            IZoneDefenderAllocationService allocationService,
            IEventFeedService eventFeedService,
            IDictionary<string, IAIStrategy> strategies)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _battleSimulationService = battleSimulationService ?? throw new ArgumentNullException(nameof(battleSimulationService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _eventFeedService = eventFeedService ?? throw new ArgumentNullException(nameof(eventFeedService));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));

            _isRunning = false;
            _decisionTimer = 0f;
            _recruitmentTimer = 0f;
        }

        /// <inheritdoc />
        public void Start()
        {
            _isRunning = true;
            _decisionTimer = 0f;
            _recruitmentTimer = 0f;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _isRunning = false;
        }

        /// <inheritdoc />
        public void SetPlayerFactionId(string? factionId)
        {
            _playerFactionId = factionId;
        }

        /// <inheritdoc />
        public void SetPlayerZone(string? zoneId)
        {
            _playerZoneId = zoneId;
        }

        /// <inheritdoc />
        public void Update(float deltaTimeSeconds)
        {
            if (!_isRunning)
                return;

            // Update recruitment timer
            _recruitmentTimer += deltaTimeSeconds;
            if (_recruitmentTimer >= DefaultRecruitmentIntervalSeconds)
            {
                _recruitmentTimer = 0f;
                RecruitForAllAIFactions();
            }

            // Update decision timer
            _decisionTimer += deltaTimeSeconds;
            if (_decisionTimer >= DefaultDecisionIntervalSeconds)
            {
                _decisionTimer = 0f;
                MakeDecisionsForAllAIFactions();
            }
        }

        private void RecruitForAllAIFactions()
        {
            var factions = _factionService.GetActiveFactions();

            foreach (var faction in factions)
            {
                if (faction.Id == _playerFactionId)
                    continue;

                TryRecruitTroops(faction.Id);
            }
        }

        private void TryRecruitTroops(string factionId)
        {
            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return;

            int affordableTroops = state.Cash / RecruitCostPerTroop;
            int troopsToRecruit = Math.Min(affordableTroops, MaxRecruitPerCycle);

            if (troopsToRecruit <= 0)
                return;

            int cost = troopsToRecruit * RecruitCostPerTroop;
            _factionService.RecruitTroops(factionId, troopsToRecruit);
            _factionService.SpendCash(factionId, cost);
        }

        private void MakeDecisionsForAllAIFactions()
        {
            var factions = _factionService.GetActiveFactions();

            foreach (var faction in factions)
            {
                if (faction.Id == _playerFactionId)
                    continue;

                MakeDecisionForFaction(faction.Id);
            }
        }

        private void MakeDecisionForFaction(string factionId)
        {
            if (!_strategies.TryGetValue(factionId, out var strategy))
                return;

            var faction = _factionService.GetFaction(factionId);
            if (faction == null)
                return;

            var factionState = _factionService.GetFactionState(factionId);
            if (factionState == null)
                return;

            var context = BuildAIContext(faction, factionState);
            var decisions = strategy.MakeDecisions(context);

            foreach (var decision in decisions)
            {
                if (decision.DecisionType == AIDecisionType.Attack)
                {
                    ExecuteAttackDecision(factionId, decision);
                }
            }
        }

        private AIContext BuildAIContext(Faction faction, FactionState factionState)
        {
            var allZones = _zoneService.GetAllZones().ToList();
            var ownedZones = _zoneService.GetZonesByOwner(faction.Id);
            var allFactions = _factionService.GetAllFactions();
            var enemyFactions = allFactions.Where(f => f.Id != faction.Id);

            return new AIContext(
                faction,
                factionState,
                ownedZones,
                allZones,
                enemyFactions);
        }

        private void ExecuteAttackDecision(string attackerFactionId, AIDecision decision)
        {
            if (decision.TargetZoneId == null)
                return;

            // Check budget
            var state = _factionService.GetFactionState(attackerFactionId);
            if (state == null)
                return;

            int cost = decision.TroopsToCommit * AttackCostPerTroop;
            if (state.Cash < cost)
                return;

            // Spend cash
            _factionService.SpendCash(attackerFactionId, cost);

            // Raise attack started event
            OnAttackStarted?.Invoke(this, new AIAttackEventArgs(
                attackerFactionId,
                decision.TargetZoneId,
                decision.TroopsToCommit));

            // Don't simulate if player is in the zone
            if (_playerZoneId == decision.TargetZoneId)
                return;

            // Simulate the battle
            SimulateBattle(attackerFactionId, decision);
        }

        private void SimulateBattle(string attackerFactionId, AIDecision decision)
        {
            var zone = _zoneService.GetZone(decision.TargetZoneId!);
            if (zone == null)
                return;

            // Can't attack own zone
            if (zone.OwnerFactionId == attackerFactionId)
                return;

            var attackerFaction = _factionService.GetFaction(attackerFactionId);
            var attackerFactionName = attackerFaction?.Name ?? attackerFactionId;

            // Handle neutral zone capture
            if (zone.OwnerFactionId == null)
            {
                _zoneService.TransferZoneOwnership(decision.TargetZoneId!, attackerFactionId);
                _eventFeedService.AddZoneCaptured(zone.Name, attackerFactionName);

                OnBattleResolved?.Invoke(this, new AIBattleResultEventArgs(
                    attackerFactionId, "neutral", decision.TargetZoneId!, true, 0, 0));
                return;
            }

            var defenderFactionId = zone.OwnerFactionId;

            // Build troop compositions
            var attackerTroops = new TroopComposition(decision.TroopsToCommit, 0, 0);
            var defenderTroops = BuildDefenderTroops(defenderFactionId, decision.TargetZoneId!);

            // Simulate battle
            var result = _battleSimulationService.SimulateBattle(
                attackerFactionId,
                defenderFactionId,
                decision.TargetZoneId!,
                attackerTroops,
                defenderTroops);

            // Apply results
            ApplyBattleResult(result);

            // Notify
            var defenderFactionName = _factionService.GetFaction(defenderFactionId)?.Name ?? defenderFactionId;
            if (result.AttackerWon)
            {
                _eventFeedService.AddZoneCaptured(zone.Name, attackerFactionName);
            }
            else
            {
                _eventFeedService.AddCombatEnded(zone.Name, defenderFactionName, defenderWon: true);
            }

            OnBattleResolved?.Invoke(this, new AIBattleResultEventArgs(
                attackerFactionId,
                defenderFactionId,
                decision.TargetZoneId!,
                result.AttackerWon,
                result.AttackerCasualties.TotalCount,
                result.DefenderCasualties.TotalCount));
        }

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

        private void ApplyBattleResult(BattleSimulationResult result)
        {
            int attackerCasualties = result.AttackerCasualties.TotalCount;
            if (attackerCasualties > 0)
            {
                _factionService.LoseTroops(result.AttackerFactionId, attackerCasualties);
            }

            int defenderCasualties = result.DefenderCasualties.TotalCount;
            if (defenderCasualties > 0)
            {
                _factionService.LoseTroops(result.DefenderFactionId, defenderCasualties);
            }

            if (result.AttackerWon)
            {
                _zoneService.TransferZoneOwnership(result.ZoneId, result.AttackerFactionId);
            }
        }
    }
}
