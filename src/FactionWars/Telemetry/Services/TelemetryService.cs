using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Events;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using FactionWars.Territory.Events;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Orchestrates telemetry: drives a periodic snapshot tick (called from the game loop)
    /// and routes its output to the sink. Also subscribes to optional domain event sources
    /// (zones, battles, AI decisions, allocations, recruitment, resource ticks, attacker
    /// kills, save events, victory and difficulty) supplied via <see cref="TelemetryServiceOptions"/>
    /// and forwards each to the sink.
    /// </summary>
    public sealed partial class TelemetryService : ITelemetryService
    {
        private const float SnapshotIntervalSeconds = 60f;

        private readonly ITelemetrySink _sink;
        private readonly IGameStateManager _gameStateManager;
        private readonly IFactionSnapshotBuilder _snapshotBuilder;
        private readonly Func<int> _getPlayerPedHandle;
        private readonly Func<string, bool> _isFirstTimeSeenSave;
        private readonly Func<bool>? _isPlayerDead;
        private readonly Func<string?> _getCurrentZoneId;
        private readonly Func<Vector3>? _getPlayerPosition;
        private readonly Func<PlayerDeathCause>? _getPlayerDeathCause;
        private readonly IZoneBattleManager? _zoneBattleManager;
        private readonly List<Action> _unsubscribers = [];
        private float _secondsSinceLastSnapshot;
        private bool _wasPlayerDead;
        private string? _currentPlayerBattleZoneId;
        private bool _playerBattleEndedSinceLastPoll;
        private bool _disposed;

        public TelemetryService(ITelemetrySink sink,
            IFactionService factionService,
            IZoneService zoneService,
            IGameStateManager gameStateManager,
            ITelemetryServiceOptions? options = null)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            if (factionService == null) throw new ArgumentNullException(nameof(factionService));
            if (zoneService == null) throw new ArgumentNullException(nameof(zoneService));

            var opts = options ?? new TelemetryServiceOptions();
            _snapshotBuilder = new FactionSnapshotBuilder(
                factionService,
                zoneService,
                opts.AllocationService,
                opts.GetPlayerFactionId,
                opts.GetPlayerMoney);

            // BattleAttackerManager requires a real player-ped handle source: every player
            // kill check would silently fail otherwise (no real ped has handle 0).
            if (opts.BattleAttackerManager != null && opts.GetPlayerPedHandle == null)
            {
                throw new ArgumentException(
                    "GetPlayerPedHandle is required when BattleAttackerManager is provided",
                    nameof(options));
            }

            // Default the delegate so the AttackerKilled handler (and only it) reads a
            // benign value when no manager is wired up. The validation above guarantees
            // we don't hit that path if BattleAttackerManager is set.
            _getPlayerPedHandle = opts.GetPlayerPedHandle ?? (() => 0);

            // Default to "never first-time" so MatchStart is never emitted unless the host
            // explicitly supplies a predicate (Task 13 wires a directory-existence check).
            _isFirstTimeSeenSave = opts.IsFirstTimeSeenSave ?? (_ => false);
            _isPlayerDead = opts.IsPlayerDead;
            _getCurrentZoneId = opts.GetCurrentZoneId ?? (() => null);
            _getPlayerPosition = opts.GetPlayerPosition;
            _getPlayerDeathCause = opts.GetPlayerDeathCause;
            _zoneBattleManager = opts.ZoneBattleManager;
            if (_isPlayerDead != null)
            {
                _wasPlayerDead = SafeReadPlayerDead(initialRead: true);
            }

            // 1. Zone ownership (always subscribed - zoneService is required).
            EventHandler<ZoneOwnershipChangedEventArgs> zoneHandler = OnZoneOwnershipChanged;
            zoneService.ZoneOwnershipChanged += zoneHandler;
            _unsubscribers.Add(() => zoneService.ZoneOwnershipChanged -= zoneHandler);
            FileLogger.Info("TelemetryService: subscribed to ZoneOwnershipChanged");

            if (opts.ZoneBattleManager != null)
            {
                Action<ZoneBattle> startedHandler = OnBattleStarted;
                Action<ZoneBattle, BattleOutcome> endedHandler = OnBattleEnded;
                opts.ZoneBattleManager.BattleStarted += startedHandler;
                opts.ZoneBattleManager.BattleEnded += endedHandler;
                _unsubscribers.Add(() => opts.ZoneBattleManager.BattleStarted -= startedHandler);
                _unsubscribers.Add(() => opts.ZoneBattleManager.BattleEnded -= endedHandler);
                FileLogger.Info("TelemetryService: subscribed to BattleStarted/BattleEnded");
            }

            if (opts.AllocationService != null)
            {
                EventHandler<TroopsAllocatedEventArgs> handler = OnTroopsAllocated;
                opts.AllocationService.TroopsAllocated += handler;
                _unsubscribers.Add(() => opts.AllocationService.TroopsAllocated -= handler);
                FileLogger.Info("TelemetryService: subscribed to TroopsAllocated");
            }

            if (opts.AIController != null)
            {
                EventHandler<FactionWars.AI.Events.TroopsRecruitedEventArgs> handler = OnTroopsRecruited;
                opts.AIController.OnTroopsRecruited += handler;
                _unsubscribers.Add(() => opts.AIController.OnTroopsRecruited -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnTroopsRecruited");
            }

            if (opts.ResourceTickService != null)
            {
                EventHandler<ResourceTickEventArgs> handler = OnResourceTick;
                opts.ResourceTickService.OnResourceTick += handler;
                _unsubscribers.Add(() => opts.ResourceTickService.OnResourceTick -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnResourceTick");
            }

            if (opts.BattleAttackerManager != null)
            {
                EventHandler<AttackerKilledEventArgs> handler = OnAttackerKilled;
                opts.BattleAttackerManager.AttackerKilled += handler;
                _unsubscribers.Add(() => opts.BattleAttackerManager.AttackerKilled -= handler);
                FileLogger.Info("TelemetryService: subscribed to AttackerKilled");
            }

            // OnGameLoaded is on the required IGameStateManager — only subscribe once we
            // have a service that exposes the event (it always does).
            EventHandler<GameStateLoadedEventArgs> loadedHandler = OnGameLoaded;
            gameStateManager.OnGameLoaded += loadedHandler;
            _unsubscribers.Add(() => gameStateManager.OnGameLoaded -= loadedHandler);
            FileLogger.Info("TelemetryService: subscribed to OnGameLoaded");

            if (opts.NativeSaveWatcher != null)
            {
                EventHandler<SaveEvent> handler = OnNativeSaveWritten;
                opts.NativeSaveWatcher.OnNativeSaveWritten += handler;
                _unsubscribers.Add(() => opts.NativeSaveWatcher.OnNativeSaveWritten -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnNativeSaveWritten");
            }

            if (opts.VictoryManager != null)
            {
                EventHandler<VictoryEventArgs> handler = OnVictory;
                opts.VictoryManager.OnVictory += handler;
                _unsubscribers.Add(() => opts.VictoryManager.OnVictory -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnVictory");
            }

            if (opts.DifficultyService != null)
            {
                EventHandler<DifficultySettings> handler = OnDifficultyChanged;
                opts.DifficultyService.DifficultyChanged += handler;
                _unsubscribers.Add(() => opts.DifficultyService.DifficultyChanged -= handler);
                FileLogger.Info("TelemetryService: subscribed to DifficultyChanged");
            }

            // Emit ModSessionStart AFTER all subscriptions are wired so the row reflects
            // a fully-initialised service. Wrapped in try/catch so a sink failure here
            // can't take the constructor (and therefore mod startup) down.
            try
            {
                _sink.WriteMatchMeta(new MatchMetaEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    MatchMetaEventType.ModSessionStart, string.Empty));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService: ModSessionStart write failed", ex);
            }
        }

        /// <summary>
        /// Drive the telemetry timer. Call once per game tick from the game loop with the
        /// elapsed game-time delta. Triggers a snapshot every <c>SnapshotIntervalSeconds</c>.
        /// </summary>
        public void Update(float deltaTimeSeconds)
        {
            if (_disposed) return;
            PollPlayerBattleState();
            PollPlayerDeathState();
            _secondsSinceLastSnapshot += deltaTimeSeconds;
            if (_secondsSinceLastSnapshot >= SnapshotIntervalSeconds)
            {
                _secondsSinceLastSnapshot = 0f;
                SafeTick();
            }
        }

        private void PollPlayerDeathState()
        {
            if (_isPlayerDead == null) return;

            var isDead = SafeReadPlayerDead(initialRead: false);
            if (isDead == _wasPlayerDead) return;

            _wasPlayerDead = isDead;
            try
            {
                var eventType = isDead ? PlayerEventType.Death : PlayerEventType.RespawnAtHospital;
                _sink.WritePlayerEvent(new PlayerEventRow(
                    DateTime.Now,
                    _gameStateManager.TotalPlayTimeSeconds,
                    eventType,
                    _getCurrentZoneId(),
                    targetFaction: null,
                    targetTier: null,
                    details: isDead ? BuildPlayerDeathDetails() : BuildPlayerPositionDetails()));

                if (isDead)
                {
                    WritePlayerBattleDeath();
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.PollPlayerDeathState failed", ex);
            }
        }

        private bool SafeReadPlayerDead(bool initialRead)
        {
            try
            {
                return _isPlayerDead?.Invoke() ?? false;
            }
            catch (Exception ex)
            {
                FileLogger.Error(initialRead
                    ? "TelemetryService: initial player death state read failed"
                    : "TelemetryService: player death state read failed", ex);
                return _wasPlayerDead;
            }
        }

        /// <summary>
        /// Force an immediate snapshot tick. Intended for tests and explicit on-demand
        /// snapshots; the periodic schedule is driven by <see cref="Update"/>.
        /// </summary>
        public void Tick() => SafeTick();

        private void SafeTick()
        {
            if (_disposed) return;
            try
            {
                var rows = _snapshotBuilder.Build(DateTime.Now, _gameStateManager.TotalPlayTimeSeconds);
                if (rows.Count > 0) _sink.WriteSnapshot(rows);
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService: snapshot tick failed", ex);
            }
        }

        // ---- Domain event handlers ----

    }
}
