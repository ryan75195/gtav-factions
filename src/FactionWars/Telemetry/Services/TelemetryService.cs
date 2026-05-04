using System;
using System.Collections.Generic;
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
    /// and routes its output to the sink. Also subscribes to ten domain event sources
    /// (zones, battles, AI decisions, allocations, recruitment, resource ticks, attacker
    /// kills, save events, victory and difficulty) and forwards each to the sink.
    /// </summary>
    public sealed class TelemetryService : IDisposable
    {
        private const float SnapshotIntervalSeconds = 60f;

        private readonly ITelemetrySink _sink;
        private readonly IGameStateManager _gameStateManager;
        private readonly FactionSnapshotBuilder _snapshotBuilder;
        private readonly Func<int> _getPlayerPedHandle;
        private readonly List<Action> _unsubscribers = new List<Action>();
        private float _secondsSinceLastSnapshot;
        private bool _disposed;

        public TelemetryService(ITelemetrySink sink,
            IFactionService factionService,
            IZoneService zoneService,
            IGameStateManager gameStateManager,
            Func<int>? getPlayerPedHandle = null,
            IZoneBattleManager? zoneBattleManager = null,
            AIManager? aiManager = null,
            IAIController? aiController = null,
            IZoneDefenderAllocationService? allocationService = null,
            IResourceTickService? resourceTickService = null,
            BattleAttackerManager? battleAttackerManager = null,
            VictoryManager? victoryManager = null,
            IDifficultyService? difficultyService = null,
            NativeSaveWatcher? nativeSaveWatcher = null)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            if (factionService == null) throw new ArgumentNullException(nameof(factionService));
            if (zoneService == null) throw new ArgumentNullException(nameof(zoneService));
            _snapshotBuilder = new FactionSnapshotBuilder(factionService, zoneService);
            // The player ped handle changes on respawn, so capture by delegate. Default to
            // a no-op returning 0 so consumers using the legacy 4-arg form still work; they
            // won't subscribe to AttackerKilled, so the value is never read.
            _getPlayerPedHandle = getPlayerPedHandle ?? (() => 0);

            // 1. Zone ownership (always subscribed - zoneService is required).
            EventHandler<ZoneOwnershipChangedEventArgs> zoneHandler = OnZoneOwnershipChanged;
            zoneService.ZoneOwnershipChanged += zoneHandler;
            _unsubscribers.Add(() => zoneService.ZoneOwnershipChanged -= zoneHandler);
            FileLogger.Info("TelemetryService: subscribed to ZoneOwnershipChanged");

            if (zoneBattleManager != null)
            {
                Action<ZoneBattle> startedHandler = OnBattleStarted;
                Action<ZoneBattle, BattleOutcome> endedHandler = OnBattleEnded;
                zoneBattleManager.BattleStarted += startedHandler;
                zoneBattleManager.BattleEnded += endedHandler;
                _unsubscribers.Add(() => zoneBattleManager.BattleStarted -= startedHandler);
                _unsubscribers.Add(() => zoneBattleManager.BattleEnded -= endedHandler);
                FileLogger.Info("TelemetryService: subscribed to BattleStarted/BattleEnded");
            }

            if (aiManager != null)
            {
                EventHandler<AIDecisionEventArgs> handler = OnAIDecision;
                aiManager.OnAIDecision += handler;
                _unsubscribers.Add(() => aiManager.OnAIDecision -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnAIDecision");
            }

            if (allocationService != null)
            {
                EventHandler<TroopsAllocatedEventArgs> handler = OnTroopsAllocated;
                allocationService.TroopsAllocated += handler;
                _unsubscribers.Add(() => allocationService.TroopsAllocated -= handler);
                FileLogger.Info("TelemetryService: subscribed to TroopsAllocated");
            }

            if (aiController != null)
            {
                EventHandler<FactionWars.AI.Events.TroopsRecruitedEventArgs> handler = OnTroopsRecruited;
                aiController.OnTroopsRecruited += handler;
                _unsubscribers.Add(() => aiController.OnTroopsRecruited -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnTroopsRecruited");
            }

            if (resourceTickService != null)
            {
                EventHandler<ResourceTickEventArgs> handler = OnResourceTick;
                resourceTickService.OnResourceTick += handler;
                _unsubscribers.Add(() => resourceTickService.OnResourceTick -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnResourceTick");
            }

            if (battleAttackerManager != null)
            {
                EventHandler<AttackerKilledEventArgs> handler = OnAttackerKilled;
                battleAttackerManager.AttackerKilled += handler;
                _unsubscribers.Add(() => battleAttackerManager.AttackerKilled -= handler);
                FileLogger.Info("TelemetryService: subscribed to AttackerKilled");
            }

            // OnGameLoaded is on the required IGameStateManager — only subscribe once we
            // have a service that exposes the event (it always does).
            EventHandler<GameStateLoadedEventArgs> loadedHandler = OnGameLoaded;
            gameStateManager.OnGameLoaded += loadedHandler;
            _unsubscribers.Add(() => gameStateManager.OnGameLoaded -= loadedHandler);
            FileLogger.Info("TelemetryService: subscribed to OnGameLoaded");

            if (nativeSaveWatcher != null)
            {
                EventHandler<NativeSaveWatcher.SaveEvent> handler = OnNativeSaveWritten;
                nativeSaveWatcher.OnNativeSaveWritten += handler;
                _unsubscribers.Add(() => nativeSaveWatcher.OnNativeSaveWritten -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnNativeSaveWritten");
            }

            if (victoryManager != null)
            {
                EventHandler<VictoryEventArgs> handler = OnVictory;
                victoryManager.OnVictory += handler;
                _unsubscribers.Add(() => victoryManager.OnVictory -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnVictory");
            }

            if (difficultyService != null)
            {
                EventHandler<DifficultySettings> handler = OnDifficultyChanged;
                difficultyService.DifficultyChanged += handler;
                _unsubscribers.Add(() => difficultyService.DifficultyChanged -= handler);
                FileLogger.Info("TelemetryService: subscribed to DifficultyChanged");
            }
        }

        /// <summary>
        /// Drive the telemetry timer. Call once per game tick from the game loop with the
        /// elapsed game-time delta. Triggers a snapshot every <c>SnapshotIntervalSeconds</c>.
        /// </summary>
        public void Update(float deltaTimeSeconds)
        {
            if (_disposed) return;
            _secondsSinceLastSnapshot += deltaTimeSeconds;
            if (_secondsSinceLastSnapshot >= SnapshotIntervalSeconds)
            {
                _secondsSinceLastSnapshot = 0f;
                SafeTick();
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

        private void OnZoneOwnershipChanged(object? sender, ZoneOwnershipChangedEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnZoneOwnershipChanged: zone={e.ZoneId} prev={e.PreviousOwner ?? "null"} new={e.NewOwner ?? "null"}");
            try
            {
                var ts = DateTime.Now;
                var pt = _gameStateManager.TotalPlayTimeSeconds;
                if (e.NewOwner == null)
                {
                    _sink.WriteZoneEvent(new ZoneEventRow(ts, pt,
                        ZoneEventType.Neutralized, e.ZoneId, e.PreviousOwner, null));
                    return;
                }

                _sink.WriteZoneEvent(new ZoneEventRow(ts, pt,
                    ZoneEventType.Captured, e.ZoneId, e.PreviousOwner, e.NewOwner));

                if (e.PreviousOwner != null)
                {
                    _sink.WriteZoneEvent(new ZoneEventRow(ts, pt,
                        ZoneEventType.Lost, e.ZoneId, e.PreviousOwner, e.NewOwner));
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnZoneOwnershipChanged failed", ex);
            }
        }

        private void OnBattleStarted(ZoneBattle b)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnBattleStarted: zone={b.ZoneId} attacker={b.AttackerFactionId} defender={b.DefenderFactionId}");
            try
            {
                _sink.WriteBattle(new BattleEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    BattleEventType.Started, b.ZoneId,
                    b.AttackerFactionId, b.DefenderFactionId,
                    b.TotalAttackerTroops, b.TotalDefenderTroops,
                    outcome: null, attackerCasualties: 0, defenderCasualties: 0));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnBattleStarted failed", ex);
            }
        }

        private void OnBattleEnded(ZoneBattle b, BattleOutcome outcome)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnBattleEnded: zone={b.ZoneId} outcome={outcome}");
            try
            {
                _sink.WriteBattle(new BattleEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    BattleEventType.Ended, b.ZoneId,
                    b.AttackerFactionId, b.DefenderFactionId,
                    b.TotalAttackerTroops, b.TotalDefenderTroops,
                    outcome: outcome,
                    attackerCasualties: b.InitialAttackerTroops - b.TotalAttackerTroops,
                    defenderCasualties: b.InitialDefenderTroops - b.TotalDefenderTroops));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnBattleEnded failed", ex);
            }
        }

        private void OnAIDecision(object? sender, AIDecisionEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnAIDecision: faction={e.FactionId} type={e.Decision.DecisionType}");
            try
            {
                _sink.WriteDecision(new DecisionEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, e.Decision.DecisionType, e.Decision.TargetZoneId,
                    e.Decision.TroopsToCommit, e.Decision.Priority, executed: true));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnAIDecision failed", ex);
            }
        }

        private void OnTroopsAllocated(object? sender, TroopsAllocatedEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnTroopsAllocated: faction={e.FactionId} zone={e.ZoneId} tier={e.Tier} count={e.Count}");
            try
            {
                // Default Source to AI for now — player allocation isn't currently wired
                // through this event. Source becomes meaningful once UI flows are added.
                _sink.WriteAllocation(new AllocationEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, e.ZoneId, e.Tier, e.Count, AllocationSource.AI));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnTroopsAllocated failed", ex);
            }
        }

        private void OnTroopsRecruited(object? sender, FactionWars.AI.Events.TroopsRecruitedEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnTroopsRecruited: faction={e.FactionId} troops={e.TroopsRecruited}");
            try
            {
                _sink.WriteRecruitment(new RecruitmentEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, e.TroopsRecruited, e.Cost, e.CashBefore, e.CashAfter));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnTroopsRecruited failed", ex);
            }
        }

        private void OnResourceTick(object? sender, ResourceTickEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnResourceTick: faction={e.FactionId} cash={e.CashGenerated}");
            try
            {
                // ZonesContributing is set to 0 because the ResourceTickEventArgs does not
                // expose a zone count, and we deliberately do NOT couple TelemetryService
                // to IZoneService for an extra side-call: the current event set is the
                // contract, and a richer DTO is a future enhancement (see plan task 11).
                _sink.WriteResourceTick(new ResourceTickEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    e.FactionId, income: e.CashGenerated, zonesContributing: 0));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnResourceTick failed", ex);
            }
        }

        private void OnAttackerKilled(object? sender, AttackerKilledEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnAttackerKilled: zone={e.ZoneId} faction={e.FactionId} ped={e.PedHandle} killer={e.KillerPedHandle}");
            try
            {
                var row = PlayerKillResolver.Resolve(e, _getPlayerPedHandle(),
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds);
                if (row != null) _sink.WritePlayerEvent(row);
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnAttackerKilled failed", ex);
            }
        }

        private void OnGameLoaded(object? sender, GameStateLoadedEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnGameLoaded: success={e.Success} save={e.SaveName}");
            try
            {
                if (e.Success && !string.IsNullOrEmpty(e.SaveName))
                {
                    _sink.SetSaveFile(e.SaveName);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnGameLoaded failed", ex);
            }
        }

        private void OnNativeSaveWritten(object? sender, NativeSaveWatcher.SaveEvent e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnNativeSaveWritten: path={e.Path}");
            try
            {
                var filename = System.IO.Path.GetFileName(e.Path);
                if (!string.IsNullOrEmpty(filename))
                {
                    _sink.SetSaveFile(filename);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnNativeSaveWritten failed", ex);
            }
        }

        private void OnVictory(object? sender, VictoryEventArgs e)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnVictory: faction={e.WinningFactionId} ({e.WinningFactionName})");
            try
            {
                // MatchMetaEventRow has no FactionId field; encode the winning faction id
                // via Details (and append the human-readable name in parentheses).
                var details = $"{e.WinningFactionId}|{e.WinningFactionName}";
                _sink.WriteMatchMeta(new MatchMetaEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    MatchMetaEventType.Victory, details));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnVictory failed", ex);
            }
        }

        private void OnDifficultyChanged(object? sender, DifficultySettings settings)
        {
            if (_disposed) return;
            FileLogger.Debug($"TelemetryService.OnDifficultyChanged: level={settings.Level}");
            try
            {
                _sink.WriteMatchMeta(new MatchMetaEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    MatchMetaEventType.DifficultyChanged, settings.Level.ToString()));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnDifficultyChanged failed", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var u in _unsubscribers)
            {
                try { u(); }
                catch (Exception ex) { FileLogger.Error("TelemetryService: unsubscribe failed", ex); }
            }
            _unsubscribers.Clear();
        }
    }
}
