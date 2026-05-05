using System;
using System.Collections.Concurrent;
using System.IO;
using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Persistence
{
    /// <summary>
    /// Defers native save sidecar writes onto the script tick thread.
    /// NativeSaveWatcher raises events from timer/file-watcher threads, so this class
    /// deliberately does no GTA native bridge reads from Enqueue.
    /// </summary>
    public sealed class NativeSaveSidecarWriter
    {
        private readonly IGameBridge _gameBridge;
        private readonly IGameStateManager _gameStateManager;
        private readonly ConcurrentDictionary<string, SaveEvent> _pendingSaves =
            new ConcurrentDictionary<string, SaveEvent>(StringComparer.OrdinalIgnoreCase);

        public NativeSaveSidecarWriter(IGameBridge gameBridge, IGameStateManager gameStateManager)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
        }

        public void Enqueue(SaveEvent saveEvent)
        {
            if (saveEvent == null) throw new ArgumentNullException(nameof(saveEvent));
            _pendingSaves[saveEvent.Path] = saveEvent;
        }

        public void ProcessPending()
        {
            foreach (var pending in _pendingSaves.ToArray())
            {
                if (!_pendingSaves.TryRemove(pending.Key, out var saveEvent))
                    continue;

                WriteSidecar(saveEvent);
            }
        }

        private void WriteSidecar(SaveEvent saveEvent)
        {
            try
            {
                var playTime = _gameBridge.GetTotalPlayTimeSeconds();
                if (playTime == null)
                {
                    FileLogger.Warn("NativeSaveSidecarWriter: play-time read failed; skipping sidecar write to avoid orphaning state at TotalPlayTimeSeconds=0.");
                    return;
                }

                var fingerprint = SaveFingerprint.Capture(_gameBridge);
                var pos = _gameBridge.GetPlayerPosition();
                var heading = _gameBridge.GetPlayerHeading();
                var position = new PlayerPosition { X = pos.X, Y = pos.Y, Z = pos.Z, Heading = heading };
                var nativeFilename = Path.GetFileName(saveEvent.Path);

                _gameStateManager.WriteCurrentSidecar(fingerprint, position, nativeFilename);
            }
            catch (Exception ex)
            {
                FileLogger.Error("NativeSaveSidecarWriter: failed", ex);
            }
        }
    }
}
