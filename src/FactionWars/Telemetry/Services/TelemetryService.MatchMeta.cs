using System;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Events;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Services
{
    public sealed partial class TelemetryService
    {
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
                        var details = Newtonsoft.Json.JsonConvert.SerializeObject(
                            new SaveMatchStartDetails(e.SaveName));
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

        private void OnNativeSaveWritten(object? sender, SaveEvent e)
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
                var details = Newtonsoft.Json.JsonConvert.SerializeObject(
                    new VictoryDetails(e.WinningFactionId, e.WinningFactionName));
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

        private sealed class PlayerPositionDetails
        {
            public PlayerPositionDetails(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            [Newtonsoft.Json.JsonProperty("x")]
            public float X { get; }

            [Newtonsoft.Json.JsonProperty("y")]
            public float Y { get; }

            [Newtonsoft.Json.JsonProperty("z")]
            public float Z { get; }
        }

        private sealed class PlayerDeathDetails
        {
            public PlayerDeathDetails(float x, float y, float z, string? killerWeapon, int killerHandle)
            {
                X = x;
                Y = y;
                Z = z;
                KillerWeapon = killerWeapon;
                KillerHandle = killerHandle;
            }

            [Newtonsoft.Json.JsonProperty("x")]
            public float X { get; }

            [Newtonsoft.Json.JsonProperty("y")]
            public float Y { get; }

            [Newtonsoft.Json.JsonProperty("z")]
            public float Z { get; }

            [Newtonsoft.Json.JsonProperty("killer_weapon")]
            public string? KillerWeapon { get; }

            [Newtonsoft.Json.JsonProperty("killer_handle")]
            public int KillerHandle { get; }
        }

        private sealed class SaveMatchStartDetails
        {
            public SaveMatchStartDetails(string save)
            {
                Save = save;
            }

            [Newtonsoft.Json.JsonProperty("save")]
            public string Save { get; }
        }

        private sealed class VictoryDetails
        {
            public VictoryDetails(string factionId, string name)
            {
                FactionId = factionId;
                Name = name;
            }

            [Newtonsoft.Json.JsonProperty("factionId")]
            public string FactionId { get; }

            [Newtonsoft.Json.JsonProperty("name")]
            public string Name { get; }
        }
    }
}
