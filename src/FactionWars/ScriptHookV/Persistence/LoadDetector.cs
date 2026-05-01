using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;
using System;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Detects savegame loads by watching TOTAL_PLAYING_TIME for discontinuities.
    /// On the first tick we attempt a sidecar match (handles fresh-launch + auto-load).
    /// On subsequent ticks any backwards play-time jump is treated as a load. We
    /// never use Game.IsLoading because (a) it is obsolete and (b) SHVDN scripts
    /// do not reliably tick during loading screens, so the IsLoading edge is missed.
    ///
    /// TOTAL_PLAYING_TIME advances by ~30s during the post-load animation, so we
    /// match the closest sidecar within a backward window rather than expecting an
    /// exact fingerprint match.
    /// </summary>
    public sealed class LoadDetector
    {
        private const long PostLoadDriftWindowSeconds = 60;

        private readonly IGameBridge _bridge;
        private readonly ISidecarStore _store;
        private readonly Action<Sidecar> _onHydrate;
        private readonly Action _onNewGame;

        private bool _initialized;
        private long _lastPlayTimeSeconds;

        public LoadDetector(IGameBridge bridge, ISidecarStore store, Action<Sidecar> onHydrate, Action onNewGame)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _onHydrate = onHydrate ?? throw new ArgumentNullException(nameof(onHydrate));
            _onNewGame = onNewGame ?? throw new ArgumentNullException(nameof(onNewGame));
        }

        /// <summary>
        /// Called once per script tick. No SHVDN dependencies — pure logic.
        /// </summary>
        public void Tick()
        {
            long currentPlayTime;
            try
            {
                currentPlayTime = _bridge.GetTotalPlayTimeSeconds();
            }
            catch (Exception ex)
            {
                FileLogger.Error("LoadDetector: GetTotalPlayTimeSeconds threw", ex);
                return;
            }

            bool isLoadEvent;
            if (!_initialized)
            {
                isLoadEvent = true;
                FileLogger.Info($"LoadDetector: first tick at play-time {currentPlayTime}s — attempting sidecar match.");
            }
            else if (currentPlayTime < _lastPlayTimeSeconds)
            {
                isLoadEvent = true;
                FileLogger.Info($"LoadDetector: play-time jumped backwards {_lastPlayTimeSeconds}s -> {currentPlayTime}s — treating as load.");
            }
            else
            {
                isLoadEvent = false;
            }

            _initialized = true;
            _lastPlayTimeSeconds = currentPlayTime;

            if (!isLoadEvent) return;

            if (_store.TryFindClosestByPlayTime(currentPlayTime, PostLoadDriftWindowSeconds, out var sidecar))
            {
                FileLogger.Info($"LoadDetector: matched sidecar (play-time {sidecar.Fingerprint.TotalPlayTimeSeconds}s, current {currentPlayTime}s) — hydrating.");
                _onHydrate(sidecar);
            }
            else
            {
                FileLogger.Info($"LoadDetector: no sidecar within {PostLoadDriftWindowSeconds}s of play-time {currentPlayTime}s — starting new game.");
                _onNewGame();
            }
        }
    }
}
