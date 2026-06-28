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
        private readonly IFollowerService? _followerService;
        private readonly IPlayerFactionDetector? _factionDetector;
        private readonly ConcurrentDictionary<string, SaveEvent> _pendingSaves =
            new ConcurrentDictionary<string, SaveEvent>(StringComparer.OrdinalIgnoreCase);

        public NativeSaveSidecarWriter(
            IGameBridge gameBridge,
            IGameStateManager gameStateManager,
            IFollowerService? followerService = null,
            IPlayerFactionDetector? factionDetector = null)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _followerService = followerService;
            _factionDetector = factionDetector;
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
                var runtimeWorldState = CaptureRuntimeWorldState(position);

                _gameStateManager.WriteCurrentSidecar(fingerprint, position, nativeFilename, runtimeWorldState);
            }
            catch (Exception ex)
            {
                FileLogger.Error("NativeSaveSidecarWriter: failed", ex);
            }
        }

        private RuntimeWorldState CaptureRuntimeWorldState(PlayerPosition playerPosition)
        {
            var state = new RuntimeWorldState { PlayerPosition = playerPosition };

            var playerVehicle = _gameBridge.IsPlayerInVehicle() ? _gameBridge.GetPlayerVehicle() : -1;
            if (playerVehicle >= 0)
            {
                var vehiclePos = _gameBridge.GetVehiclePosition(playerVehicle);
                state.PlayerVehicle = new SavedVehicleState
                {
                    ModelName = _gameBridge.GetVehicleModelName(playerVehicle),
                    Position = new PlayerPosition
                    {
                        X = vehiclePos.X,
                        Y = vehiclePos.Y,
                        Z = vehiclePos.Z,
                        Heading = _gameBridge.GetVehicleHeading(playerVehicle),
                    },
                };
            }

            CaptureFollowers(state, playerVehicle);
            return state;
        }

        private void CaptureFollowers(RuntimeWorldState state, int playerVehicle)
        {
            if (_followerService == null || _factionDetector == null)
            {
                return;
            }

            var factionId = _factionDetector.GetFactionIdFromCharacterModel(_gameBridge.GetPlayerCharacterModel());
            if (string.IsNullOrEmpty(factionId))
            {
                return;
            }

            foreach (var follower in _followerService.GetFollowers(factionId!))
            {
                if (follower.PedHandle < 0 || !_gameBridge.IsPedAlive(follower.PedHandle))
                {
                    continue;
                }

                var followerPos = _gameBridge.GetPedPosition(follower.PedHandle);
                var seatIndex = _gameBridge.GetPedVehicle(follower.PedHandle) == playerVehicle
                    ? _gameBridge.GetPedVehicleSeat(follower.PedHandle)
                    : -1;

                state.Followers.Add(new SavedFollowerState
                {
                    FactionId = follower.FactionId,
                    Role = follower.Tier,
                    Position = new PlayerPosition
                    {
                        X = followerPos.X,
                        Y = followerPos.Y,
                        Z = followerPos.Z,
                    },
                    VehicleSeatIndex = seatIndex,
                });
            }
        }
    }
}
