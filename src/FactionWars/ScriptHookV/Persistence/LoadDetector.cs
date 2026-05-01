using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;
using System;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Observes loading-screen edges and reacts when a new save has been loaded.
    /// Pure logic — no SHVDN dependencies; the host script feeds it the IsLoading
    /// flag each tick and supplies callbacks for hydrate / newGame.
    /// </summary>
    public sealed class LoadDetector
    {
        private readonly IGameBridge _bridge;
        private readonly ISidecarStore _store;
        private readonly Action<Sidecar> _onHydrate;
        private readonly Action _onNewGame;

        private bool _wasLoading;
        private long _lastKnownTotalPlayTimeSeconds = -1;

        public LoadDetector(IGameBridge bridge, ISidecarStore store, Action<Sidecar> onHydrate, Action onNewGame)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _onHydrate = onHydrate ?? throw new ArgumentNullException(nameof(onHydrate));
            _onNewGame = onNewGame ?? throw new ArgumentNullException(nameof(onNewGame));
        }

        /// <summary>
        /// Called once per script tick. Detects the IsLoading edge and reacts.
        /// </summary>
        public void Tick(bool isLoading)
        {
            if (!isLoading && !_wasLoading)
            {
                _lastKnownTotalPlayTimeSeconds = _bridge.GetTotalPlayTimeSeconds();
                return;
            }

            if (_wasLoading && !isLoading)
            {
                var currentPlayTime = _bridge.GetTotalPlayTimeSeconds();

                if (currentPlayTime == _lastKnownTotalPlayTimeSeconds)
                {
                    FileLogger.Debug("LoadDetector: loading-end transition with unchanged play time — skipping (likely cutscene/fast-travel).");
                    _wasLoading = isLoading;
                    return;
                }

                var fingerprint = SaveFingerprint.Capture(_bridge);
                if (_store.TryFindByFingerprint(fingerprint, out var sidecar))
                {
                    FileLogger.Info($"LoadDetector: matched sidecar for play time {fingerprint.TotalPlayTimeSeconds}s — hydrating.");
                    _onHydrate(sidecar);
                }
                else
                {
                    FileLogger.Info($"LoadDetector: no sidecar matched play time {fingerprint.TotalPlayTimeSeconds}s — starting new game.");
                    _onNewGame();
                }

                _lastKnownTotalPlayTimeSeconds = currentPlayTime;
            }

            _wasLoading = isLoading;
        }
    }
}
