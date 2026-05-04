using System;
using System.Threading;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Orchestrates telemetry: drives a periodic snapshot timer and routes its output
    /// to the sink. Subsequent tasks add domain-event subscriptions and match-meta
    /// lifecycle to this class.
    /// </summary>
    public sealed class TelemetryService : IDisposable
    {
        private const int SnapshotIntervalMs = 60_000;

        private readonly ITelemetrySink _sink;
        private readonly IGameStateManager _gameStateManager;
        private readonly FactionSnapshotBuilder _snapshotBuilder;
        private readonly Timer _timer;
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

            _timer = new Timer(_ => SafeTick(), null, SnapshotIntervalMs, SnapshotIntervalMs);
        }

        /// <summary>
        /// Public entry for tests — bypasses the timer.
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
            try { _timer.Dispose(); } catch { }
        }
    }
}
