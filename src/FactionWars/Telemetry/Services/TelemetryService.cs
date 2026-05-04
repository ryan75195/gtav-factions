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
    public sealed class TelemetryService : IDisposable
    {
        private const float SnapshotIntervalSeconds = 60f;

        private readonly ITelemetrySink _sink;
        private readonly IGameStateManager _gameStateManager;
        private readonly FactionSnapshotBuilder _snapshotBuilder;
        private readonly Func<int> _getPlayerPedHandle;
        private readonly Func<string, bool> _isFirstTimeSeenSave;
        private readonly Func<bool>? _isPlayerDead;
        private readonly Func<string?> _getCurrentZoneId;
        private readonly Func<Vector3>? _getPlayerPosition;
        private readonly List<Action> _unsubscribers = new List<Action>();
        private float _secondsSinceLastSnapshot;
        private bool _wasPlayerDead;
        private bool _disposed;

        public TelemetryService(ITelemetrySink sink,
            IFactionService factionService,
            IZoneService zoneService,
            IGameStateManager gameStateManager,
            TelemetryServiceOptions? options = null)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            if (factionService == null) throw new ArgumentNullException(nameof(factionService));
            if (zoneService == null) throw new ArgumentNullException(nameof(zoneService));
            _snapshotBuilder = new FactionSnapshotBuilder(factionService, zoneService);

            var opts = options ?? new TelemetryServiceOptions();

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

            if (opts.AIManager != null)
            {
                EventHandler<AIDecisionEventArgs> handler = OnAIDecision;
                opts.AIManager.OnAIDecision += handler;
                _unsubscribers.Add(() => opts.AIManager.OnAIDecision -= handler);
                FileLogger.Info("TelemetryService: subscribed to OnAIDecision");
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
                EventHandler<NativeSaveWatcher.SaveEvent> handler = OnNativeSaveWritten;
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
                    details: BuildPlayerPositionDetails()));
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

        private string BuildPlayerPositionDetails()
        {
            if (_getPlayerPosition == null) return string.Empty;

            var position = _getPlayerPosition();
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                x = position.X,
                y = position.Y,
                z = position.Z,
            });
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
                var attackerFactionId = GetBattleFactionId(b, BattleRole.Attacker);
                var defenderFactionId = GetBattleFactionId(b, BattleRole.Defender);
                _sink.WriteBattle(new BattleEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    BattleEventType.Started, b.ZoneId,
                    attackerFactionId, defenderFactionId,
                    GetBattleAliveCount(b, BattleRole.Attacker),
                    GetBattleAliveCount(b, BattleRole.Defender),
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
                var attackerFactionId = GetBattleFactionId(b, BattleRole.Attacker);
                var defenderFactionId = GetBattleFactionId(b, BattleRole.Defender);
                var attackerTroops = GetBattleAliveCount(b, BattleRole.Attacker);
                var defenderTroops = GetBattleAliveCount(b, BattleRole.Defender);
                var attackerCasualties = Math.Max(0, b.InitialAttackerTroops - attackerTroops);
                var defenderCasualties = Math.Max(0, b.InitialDefenderTroops - defenderTroops);
                _sink.WriteBattle(new BattleEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    BattleEventType.Ended, b.ZoneId,
                    attackerFactionId, defenderFactionId,
                    attackerTroops, defenderTroops,
                    outcome: outcome,
                    attackerCasualties: attackerCasualties,
                    defenderCasualties: defenderCasualties));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService.OnBattleEnded failed", ex);
            }
        }

        private void OnAIDecision(object? sender, AIDecisionEventArgs e)
        {
            if (_disposed) return;
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

                    // First time we ever see this save? Emit MatchStart. The predicate
                    // call sits inside the same try/catch so a misbehaving stub or disk
                    // I/O failure cannot escape into the game thread.
                    if (_isFirstTimeSeenSave(e.SaveName))
                    {
                        // Mirror OnVictory's JSON encoding so downstream CSV consumers can
                        // parse details consistently across MatchMeta event types.
                        var details = Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            save = e.SaveName,
                        });
                        _sink.WriteMatchMeta(new MatchMetaEventRow(
                            DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                            MatchMetaEventType.MatchStart, details));
                    }
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
                // and human-readable name as a JSON object so downstream CSV consumers
                // can parse it cleanly (a pipe delimiter would collide with name punctuation).
                var details = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    factionId = e.WinningFactionId,
                    name = e.WinningFactionName,
                });
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

            // Emit ModSessionEnd BEFORE setting _disposed=true so the write is not
            // suppressed by the disposed-guards on other handlers, and BEFORE the
            // unsubscribe loop so a faulty unsubscriber can't skip the lifecycle row.
            try
            {
                _sink.WriteMatchMeta(new MatchMetaEventRow(
                    DateTime.Now, _gameStateManager.TotalPlayTimeSeconds,
                    MatchMetaEventType.ModSessionEnd, string.Empty));
            }
            catch (Exception ex)
            {
                FileLogger.Error("TelemetryService: ModSessionEnd write failed", ex);
            }

            _disposed = true;
            foreach (var u in _unsubscribers)
            {
                try { u(); }
                catch (Exception ex) { FileLogger.Error("TelemetryService: unsubscribe failed", ex); }
            }
            _unsubscribers.Clear();
        }

        private static string GetBattleFactionId(ZoneBattle battle, BattleRole role)
        {
            return battle.Participants.FirstOrDefault(p => p.Role == role)?.FactionId ?? string.Empty;
        }

        private static int GetBattleAliveCount(ZoneBattle battle, BattleRole role)
        {
            return battle.Participants.FirstOrDefault(p => p.Role == role)?.AliveCount ?? 0;
        }
    }
}
