using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Event arguments for AI decision events.
    /// </summary>
    public class AIDecisionEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the faction that made the decision.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// The decision that was made.
        /// </summary>
        public AIDecision Decision { get; }

        /// <summary>
        /// Creates new AI decision event arguments.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="decision">The decision made.</param>
        public AIDecisionEventArgs(string factionId, AIDecision decision)
        {
            FactionId = factionId;
            Decision = decision;
        }
    }

    /// <summary>
    /// Manages AI faction decisions and coordinates with the game loop.
    /// Coordinates multiple AI strategies for non-player factions, making periodic
    /// decisions based on the current game state.
    /// </summary>
    public class AIManager
    {
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IDictionary<string, IAIStrategy> _strategies;
        private readonly Dictionary<string, IList<AIDecision>> _lastDecisions;

        private bool _isRunning;
        private float _timeSinceLastDecision;
        private float _decisionIntervalSeconds;
        private string? _playerFactionId;

        private const float DefaultDecisionInterval = 5.0f;
        private const float MinDecisionInterval = 1.0f;

        /// <summary>
        /// Event raised when an AI faction makes a decision.
        /// </summary>
        public event EventHandler<AIDecisionEventArgs>? OnAIDecision;

        /// <summary>
        /// Gets whether the AI manager is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets the interval between AI decision cycles in seconds.
        /// </summary>
        public float DecisionIntervalSeconds => _decisionIntervalSeconds;

        /// <summary>
        /// Creates a new AIManager.
        /// </summary>
        /// <param name="factionService">The faction service for faction data.</param>
        /// <param name="zoneService">The zone service for zone data.</param>
        /// <param name="strategies">Dictionary mapping faction IDs to their AI strategies.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public AIManager(
            IFactionService factionService,
            IZoneService zoneService,
            IDictionary<string, IAIStrategy> strategies)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));

            _lastDecisions = new Dictionary<string, IList<AIDecision>>();
            _isRunning = false;
            _timeSinceLastDecision = 0f;
            _decisionIntervalSeconds = DefaultDecisionInterval;
            _playerFactionId = null;
        }

        /// <summary>
        /// Starts the AI manager. AI decisions will begin processing.
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            _timeSinceLastDecision = 0f;
        }

        /// <summary>
        /// Stops the AI manager. AI decisions will stop processing.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Sets the player's faction ID. This faction will be excluded from AI control.
        /// </summary>
        /// <param name="factionId">The player's faction ID, or null to clear.</param>
        public void SetPlayerFactionId(string? factionId)
        {
            _playerFactionId = factionId;
        }

        /// <summary>
        /// Gets the player's faction ID.
        /// </summary>
        /// <returns>The player's faction ID, or null if not set.</returns>
        public string? GetPlayerFactionId()
        {
            return _playerFactionId;
        }

        /// <summary>
        /// Sets the interval between AI decision cycles.
        /// </summary>
        /// <param name="intervalSeconds">The interval in seconds (minimum 1.0).</param>
        public void SetDecisionInterval(float intervalSeconds)
        {
            _decisionIntervalSeconds = Math.Max(MinDecisionInterval, intervalSeconds);
        }

        /// <summary>
        /// Updates the AI manager with elapsed time.
        /// Should be called each frame/update cycle.
        /// </summary>
        /// <param name="deltaTimeSeconds">The time elapsed since the last update in seconds.</param>
        public void Update(float deltaTimeSeconds)
        {
            if (!_isRunning)
                return;

            _timeSinceLastDecision += deltaTimeSeconds;

            if (_timeSinceLastDecision >= _decisionIntervalSeconds)
            {
                _timeSinceLastDecision = 0f;
                MakeDecisionsForAllAIFactions();
            }
        }

        /// <summary>
        /// Forces an immediate decision cycle for all AI factions.
        /// </summary>
        public void ForceDecision()
        {
            MakeDecisionsForAllAIFactions();
        }

        /// <summary>
        /// Forces an immediate decision cycle for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to make decisions for.</param>
        public void ForceDecision(string factionId)
        {
            if (factionId == _playerFactionId)
                return;

            MakeDecisionsForFaction(factionId);
        }

        /// <summary>
        /// Gets the last decisions made by a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to get decisions for.</param>
        /// <returns>The list of last decisions, or empty if none.</returns>
        public IList<AIDecision> GetLastDecisions(string factionId)
        {
            if (_lastDecisions.TryGetValue(factionId, out var decisions))
            {
                return decisions;
            }
            return new List<AIDecision>();
        }

        /// <summary>
        /// Makes decisions for all AI-controlled factions.
        /// </summary>
        private void MakeDecisionsForAllAIFactions()
        {
            var activeFactions = _factionService.GetActiveFactions();

            foreach (var faction in activeFactions)
            {
                // Skip player faction
                if (faction.Id == _playerFactionId)
                    continue;

                MakeDecisionsForFaction(faction.Id);
            }
        }

        /// <summary>
        /// Makes decisions for a specific faction.
        /// </summary>
        /// <param name="factionId">The faction ID to make decisions for.</param>
        private void MakeDecisionsForFaction(string factionId)
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

            _lastDecisions[factionId] = decisions;

            // Raise events for each decision
            foreach (var decision in decisions)
            {
                OnAIDecision?.Invoke(this, new AIDecisionEventArgs(factionId, decision));
            }
        }

        /// <summary>
        /// Builds an AI context for a faction.
        /// </summary>
        /// <param name="faction">The faction to build context for.</param>
        /// <param name="factionState">The faction's current state.</param>
        /// <returns>The built AI context.</returns>
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
    }
}
