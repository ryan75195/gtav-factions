using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Events;
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
        private readonly IAIRecruitmentService? _recruitmentService;

        // Configuration
        private const float DefaultDecisionIntervalSeconds = 60f;  // Slowed from 30s for better pacing
        private const float DefaultRecruitmentIntervalSeconds = 60f;
        private const int RecruitCostPerTroop = 200;  // Aligned with player Basic tier cost
        // NOTE: Deployment cost removed - troops are free to deploy once recruited
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
        /// Raised after a successful recruitment cycle for a single AI faction.
        /// Args expose cash before/after and troops recruited so telemetry can record
        /// a complete recruitment row. Not raised when zero troops were recruited.
        /// </summary>
        public event EventHandler<TroopsRecruitedEventArgs>? OnTroopsRecruited;

        /// <summary>
        /// Creates a new AIController.
        /// </summary>
        public AIController(AIControllerDependencies dependencies, IAIRecruitmentService? recruitmentService = null)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _factionService = dependencies.FactionService ?? throw new ArgumentNullException(nameof(dependencies.FactionService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _battleSimulationService = dependencies.BattleSimulationService ?? throw new ArgumentNullException(nameof(dependencies.BattleSimulationService));
            _allocationService = dependencies.AllocationService ?? throw new ArgumentNullException(nameof(dependencies.AllocationService));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _strategies = dependencies.Strategies ?? throw new ArgumentNullException(nameof(dependencies.Strategies));
            _zoneBattleManager = dependencies.ZoneBattleManager ?? throw new ArgumentNullException(nameof(dependencies.ZoneBattleManager));
            _recruitmentService = recruitmentService;

            _isRunning = false;
            _decisionTimer = 0f;
            _recruitmentTimer = 0f;
        }

        public AIController(params object?[] dependencies)
            : this(
                new AIControllerDependencies
                {
                    FactionService = (IFactionService?)dependencies[0],
                    ZoneService = (IZoneService?)dependencies[1],
                    BattleSimulationService = (IBattleSimulationService?)dependencies[2],
                    AllocationService = (IZoneDefenderAllocationService?)dependencies[3],
                    GameBridge = (IGameBridge?)dependencies[4],
                    Strategies = (IDictionary<string, IAIStrategy>?)dependencies[5],
                    ZoneBattleManager = (IZoneBattleManager?)dependencies[6]
                },
                dependencies.Length > 7 ? (IAIRecruitmentService?)dependencies[7] : null)
        {
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

                var stateBefore = _factionService.GetFactionState(faction.Id);
                int cashBefore = stateBefore?.Cash ?? 0;

                int recruited;
                if (_recruitmentService != null)
                {
                    recruited = _recruitmentService.TryAutoRecruit(faction.Id, MaxRecruitPerCycle);
                }
                else
                {
                    recruited = TryRecruitTroops(faction.Id);
                }

                if (recruited > 0)
                {
                    var stateAfter = _factionService.GetFactionState(faction.Id);
                    int cashAfter = stateAfter?.Cash ?? 0;
                    int cost = cashBefore - cashAfter;

                    OnTroopsRecruited?.Invoke(this, new TroopsRecruitedEventArgs(
                        faction.Id, recruited, cost, cashBefore, cashAfter));
                }
            }
        }

        private int TryRecruitTroops(string factionId)
        {
            var state = _factionService.GetFactionState(factionId);
            if (state == null)
                return 0;

            int affordableTroops = state.Cash / RecruitCostPerTroop;
            int troopsToRecruit = Math.Min(affordableTroops, MaxRecruitPerCycle);

            if (troopsToRecruit <= 0)
                return 0;

            int cost = troopsToRecruit * RecruitCostPerTroop;
            _factionService.RecruitTroops(factionId, troopsToRecruit);
            _factionService.SpendCash(factionId, cost);
            return troopsToRecruit;
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
                else if (decision.DecisionType == AIDecisionType.Defend)
                {
                    ExecuteDefendDecision(factionId, factionState, decision);
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

            // NOTE: Deployment cost removed - troops are free to deploy once recruited
            // Cash is only spent during recruitment, not during attack execution

            FileLogger.AI($"      ExecuteAttack: {attackerFactionId} attacking {decision.TargetZoneId} with {decision.TroopsToCommit} troops (deployment free)");

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

        private void ExecuteDefendDecision(string factionId, FactionState factionState, AIDecision decision)
        {
            if (decision.TargetZoneId == null)
            {
                FileLogger.AI($"      ExecuteDefend: No target zone specified");
                return;
            }

            // Desperation scaling based on zones owned
            float deployPercent = factionState.ZoneCount switch
            {
                1 => 0.80f,  // Last stand - deploy 80%
                2 => 0.50f,  // Significant threat - deploy 50%
                _ => 0.30f   // Conservative - deploy 30%
            };

            FileLogger.AI($"      ExecuteDefend: {factionId} reinforcing {decision.TargetZoneId} ({factionState.ZoneCount} zones, {deployPercent:P0} deploy)");

            int totalDeployed = 0;

            // Deploy all tiers proportionally
            foreach (var tier in new[] { DefenderTier.Basic, DefenderTier.Medium, DefenderTier.Heavy, DefenderTier.Elite })
            {
                int reserves = factionState.GetReserveTroops(tier);
                int toDeploy = (int)(reserves * deployPercent);

                if (toDeploy > 0)
                {
                    if (_allocationService.AllocateTroops(factionState, decision.TargetZoneId, tier, toDeploy))
                    {
                        totalDeployed += toDeploy;
                        FileLogger.AI($"      ExecuteDefend: Allocated {toDeploy} {tier} to {decision.TargetZoneId}");
                    }
                }
            }

            FileLogger.AI($"      ExecuteDefend: Total {totalDeployed} troops allocated to {decision.TargetZoneId}");
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
