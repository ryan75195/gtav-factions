using System;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Orchestrates telemetry: drives a periodic snapshot tick (called from the game loop)
    /// and routes its output to the sink. Subsequent tasks add domain-event subscriptions
    /// and match-meta lifecycle to this class.
    /// </summary>
    public sealed class TelemetryService : IDisposable
    {
        private const float SnapshotIntervalSeconds = 60f;

        private readonly ITelemetrySink _sink;
        private readonly IGameStateManager _gameStateManager;
        private readonly FactionSnapshotBuilder _snapshotBuilder;
        private float _secondsSinceLastSnapshot;
        private bool _disposed;

        public TelemetryService(ITelemetrySink sink,
            IFactionService factionService,
            IZoneService zoneService,
            IGameStateManager gameStateManager)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _snapshotBuilder = new FactionSnapshotBuilder(
                factionService ?? throw new ArgumentNullException(nameof(factionService)),
                zoneService ?? throw new ArgumentNullException(nameof(zoneService)));
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
