using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;

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
        private readonly IGameBridge _gameBridge;
        private readonly IDictionary<string, IAIStrategy> _strategies;
        private readonly IZoneBattleManager _zoneBattleManager;

        // Configuration
        private const float DefaultDecisionIntervalSeconds = 60f;  // Slowed from 30s for better pacing
        private const float DefaultRecruitmentIntervalSeconds = 60f;
        private const int RecruitCostPerTroop = 200;  // Aligned with player Basic tier cost
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
            IGameBridge gameBridge,
            IDictionary<string, IAIStrategy> strategies,
            IZoneBattleManager zoneBattleManager)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _battleSimulationService = battleSimulationService ?? throw new ArgumentNullException(nameof(battleSimulationService));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _zoneBattleManager = zoneBattleManager ?? throw new ArgumentNullException(nameof(zoneBattleManager));

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
                FileLogger.AI("=== AI Recruitment Cycle Started ===");
                RecruitForAllAIFactions();
            }

            // Update decision timer
            _decisionTimer += deltaTimeSeconds;
            if (_decisionTimer >= DefaultDecisionIntervalSeconds)
            {
                _decisionTimer = 0f;
                FileLogger.AI("=== AI Decision Cycle Started ===");
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
            var factions = _factionService.GetActiveFactions().ToList();
            FileLogger.AI($"Found {factions.Count} active factions, player faction: {_playerFactionId ?? "none"}");

            foreach (var faction in factions)
            {
                if (faction.Id == _playerFactionId)
                {
                    FileLogger.AI($"  Skipping player faction: {faction.Id}");
                    continue;
                }

                FileLogger.AI($"  Processing AI faction: {faction.Id}");
                MakeDecisionForFaction(faction.Id);
            }
        }

        private void MakeDecisionForFaction(string factionId)
        {
            if (!_strategies.TryGetValue(factionId, out var strategy))
            {
                FileLogger.AI($"    No strategy found for faction: {factionId}");
                return;
            }

            var faction = _factionService.GetFaction(factionId);
            if (faction == null)
            {
                FileLogger.AI($"    Faction not found: {factionId}");
                return;
            }

            var factionState = _factionService.GetFactionState(factionId);
            if (factionState == null)
            {
                FileLogger.AI($"    Faction state not found: {factionId}");
                return;
            }

            FileLogger.AI($"    {factionId} state: Cash=${factionState.Cash}, Troops={factionState.TroopCount}, Zones={factionState.ZoneCount}");

            var context = BuildAIContext(faction, factionState);
            var decisions = strategy.MakeDecisions(context);

            FileLogger.AI($"    Strategy returned {decisions.Count} decision(s)");

            foreach (var decision in decisions)
            {
                FileLogger.AI($"      Decision: {decision.DecisionType} -> {decision.TargetZoneId ?? "none"}, Priority={decision.Priority:F2}, Troops={decision.TroopsToCommit}");

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
            {
                FileLogger.AI($"      ExecuteAttack: No target zone specified");
                return;
            }

            // Check budget
            var state = _factionService.GetFactionState(attackerFactionId);
            if (state == null)
            {
                FileLogger.AI($"      ExecuteAttack: Could not get faction state");
                return;
            }

            int cost = decision.TroopsToCommit * AttackCostPerTroop;
            if (state.Cash < cost)
            {
                FileLogger.AI($"      ExecuteAttack: Insufficient funds (need ${cost}, have ${state.Cash})");
                return;
            }

            // Spend cash
            _factionService.SpendCash(attackerFactionId, cost);
            FileLogger.AI($"      ExecuteAttack: {attackerFactionId} attacking {decision.TargetZoneId} with {decision.TroopsToCommit} troops (cost ${cost})");

            // Raise attack started event
            OnAttackStarted?.Invoke(this, new AIAttackEventArgs(
                attackerFactionId,
                decision.TargetZoneId,
                decision.TroopsToCommit));

            // Always start the battle (creates ActiveBattle for timed simulation)
            // Even if player is in zone - the battle needs to exist for attacker spawning
            bool playerInZone = _playerZoneId == decision.TargetZoneId;
            FileLogger.AI($"      ExecuteAttack: Starting battle (player in zone: {playerInZone})...");
            SimulateBattle(attackerFactionId, decision, playerInZone);
        }

        private void SimulateBattle(string attackerFactionId, AIDecision decision, bool playerInZone = false)
        {
            var zone = _zoneService.GetZone(decision.TargetZoneId!);
            if (zone == null)
            {
                FileLogger.AI($"      SimulateBattle: Zone not found: {decision.TargetZoneId}");
                return;
            }

            // Can't attack own zone
            if (zone.OwnerFactionId == attackerFactionId)
            {
                FileLogger.AI($"      SimulateBattle: Cannot attack own zone");
                return;
            }

            // Skip if a battle already exists in this zone
            if (_zoneBattleManager.GetBattleForZone(decision.TargetZoneId!) != null)
            {
                FileLogger.AI($"      SimulateBattle: Battle already in progress in {decision.TargetZoneId}, skipping");
                return;
            }

            var attackerFaction = _factionService.GetFaction(attackerFactionId);
            var attackerFactionName = attackerFaction?.Name ?? attackerFactionId;

            // Handle neutral zone capture
            if (zone.OwnerFactionId == null)
            {
                FileLogger.AI($"      SimulateBattle: Capturing neutral zone {zone.Name}");
                _zoneService.TransferZoneOwnership(decision.TargetZoneId!, attackerFactionId);

                // Allocate defenders to the newly captured zone
                // Always allocate at least 1 defender to prevent "owned with 0 defenders" state
                int defendersToAllocate = decision.TroopsToCommit > 0 ? Math.Max(1, Math.Min((decision.TroopsToCommit + 1) / 2, 5)) : 0;
                if (defendersToAllocate > 0)
                {
                    _allocationService.SetAllocation(attackerFactionId, decision.TargetZoneId!, DefenderTier.Basic, defendersToAllocate);
                    FileLogger.AI($"      SimulateBattle: Allocated {defendersToAllocate} defenders to {zone.Name}");
                }

                _gameBridge.ShowNotification($"~y~{attackerFactionName}~w~ captured ~b~{zone.Name}");

                OnBattleResolved?.Invoke(this, new AIBattleResultEventArgs(
                    attackerFactionId, "neutral", decision.TargetZoneId!, true, 0, 0));
                return;
            }

            var defenderFactionId = zone.OwnerFactionId;
            FileLogger.AI($"      SimulateBattle: {attackerFactionId} vs {defenderFactionId} for {zone.Name}");

            // Build troop dictionaries for timed battle
            var attackerTroopDict = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, decision.TroopsToCommit },
                { DefenderTier.Medium, 0 },
                { DefenderTier.Heavy, 0 }
            };

            var defenderTroopDict = BuildDefenderTroopsDictionary(defenderFactionId, decision.TargetZoneId!);

            FileLogger.AI($"      SimulateBattle: AttackerTroops={decision.TroopsToCommit} Basic, DefenderTroops={defenderTroopDict[DefenderTier.Basic]}B/{defenderTroopDict[DefenderTier.Medium]}M/{defenderTroopDict[DefenderTier.Heavy]}H");

            // Start timed battle instead of instant simulation
            var battle = _zoneBattleManager.StartBattle(
                decision.TargetZoneId!,
                attackerFactionId,
                defenderFactionId,
                attackerTroopDict,
                defenderTroopDict);

            FileLogger.AI($"      SimulateBattle: Started timed battle {battle.Id} for {zone.Name}, TotalAttackers={battle.TotalAttackerTroops}, TotalDefenders={battle.TotalDefenderTroops}");

            // If player is already in the zone, mark battle as player-present to pause tick simulation
            if (playerInZone)
            {
                _zoneBattleManager.OnPlayerEnteredZone(zone);
                FileLogger.AI($"      SimulateBattle: Player is in zone, marked battle as player-present");
            }

            return; // Battle resolution handled by ZoneBattleManager
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

        private Dictionary<DefenderTier, int> BuildDefenderTroopsDictionary(string defenderFactionId, string zoneId)
        {
            var result = new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, 0 },
                { DefenderTier.Medium, 0 },
                { DefenderTier.Heavy, 0 }
            };

            var allocation = _allocationService.GetAllocation(defenderFactionId, zoneId);
            if (allocation != null)
            {
                result[DefenderTier.Basic] = allocation.GetTroopCount(DefenderTier.Basic);
                result[DefenderTier.Medium] = allocation.GetTroopCount(DefenderTier.Medium);
                result[DefenderTier.Heavy] = allocation.GetTroopCount(DefenderTier.Heavy);
            }

            return result;
        }

        private void ApplyBattleResult(BattleSimulationResult result, int attackingTroops)
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

                // Allocate surviving troops as defenders
                // Always allocate at least 1 defender to prevent "owned with 0 defenders" state
                int survivors = attackingTroops - attackerCasualties;
                int defendersToAllocate = survivors > 0 ? Math.Max(1, Math.Min((survivors + 1) / 2, 5)) : 0;
                if (defendersToAllocate > 0)
                {
                    _allocationService.SetAllocation(result.AttackerFactionId, result.ZoneId, DefenderTier.Basic, defendersToAllocate);
                    FileLogger.AI($"      ApplyBattleResult: Allocated {defendersToAllocate} defenders to {result.ZoneId}");
                }
            }
        }
    }
}
