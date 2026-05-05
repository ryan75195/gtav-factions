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
    public partial class AIController : IAIController
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
        private const float DefaultDecisionIntervalSeconds = 90f;
        private const float DefaultRecruitmentIntervalSeconds = 90f;
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

    }
}
