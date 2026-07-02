using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class SupportSquadManager
    {
        /// <summary>
        /// Ticks the active squad: prunes dead/streamed-out allies, drives the Inbound -&gt;
        /// Engaging transition once the SUV reaches the player (or is gone), and hands off to the
        /// private Search &amp; Destroy stance controller once engaging. No-op when no squad is
        /// active (<see cref="HasActiveSquad"/> false is the caller's cue to skip calling this).
        /// </summary>
        public void Update(IReadOnlyList<EnemyTarget> enemies)
        {
            PruneDeadHandles();

            if (_rolesByHandle.Count == 0)
            {
                ClearActiveSquad();
                return;
            }

            switch (_phase)
            {
                case Phase.Inbound:
                    UpdateInbound();
                    break;
                case Phase.Engaging:
                    UpdateEngaging(enemies);
                    break;
            }
        }

        private void PruneDeadHandles()
        {
            if (_rolesByHandle.Count == 0) return;

            var dead = new List<int>();
            foreach (var handle in _rolesByHandle.Keys)
            {
                // Streamed-out or killed allies both free the slot; support allies aren't
                // battle-tracked casualties, so no death event is reported anywhere.
                if (!_gameBridge.DoesPedExist(handle) || !_gameBridge.IsPedAlive(handle))
                {
                    dead.Add(handle);
                }
            }

            foreach (var handle in dead)
            {
                _pedBlip.RemoveBlipForPed(handle);
                if (!_gameBridge.DoesPedExist(handle))
                    _pedDespawn.UntrackPed(handle);   // engine already culled the entity
                else
                    _pedDespawn.DespawnPed(handle);   // dead corpse present: free pool slot + delete entity
                _rolesByHandle.Remove(handle);
            }
        }

        private void UpdateInbound()
        {
            var seatedCount = 0;
            foreach (var handle in _rolesByHandle.Keys)
            {
                if (_gameBridge.IsPedInVehicle(handle))
                {
                    seatedCount++;
                }
            }

            var anySeated = seatedCount > 0;
            MonitorInboundDrive(seatedCount);

            // No ally still seated means the SUV is effectively gone (destroyed/ejected); treat
            // that the same as "arrived" so the squad falls back to on-foot Search & Destroy.
            var suvGone = !anySeated;
            var arrived = !suvGone
                && _gameBridge.GetPlayerPosition().DistanceTo(_gameBridge.GetVehiclePosition(_suv)) < DismountRange;

            if (!suvGone && !arrived) return;

            foreach (var handle in _rolesByHandle.Keys)
            {
                // Re-enable event reactions (blocked for the drive in) so S&D allies fight.
                _gameBridge.SetPedBlockPermanentEvents(handle, false);
                if (_gameBridge.IsPedInVehicle(handle))
                {
                    _gameBridge.TaskPedLeaveVehicle(handle);
                }
            }

            _phase = Phase.Engaging;
            FileLogger.AI($"SupportSquadManager.Update: squad dismounting (suvGone={suvGone}), entering Search & Destroy");
        }

        // Watchdog for the inbound drive: in-game evidence showed the SUV spawning and getting a
        // drive task but never arriving, with nothing logged in between. Samples the SUV's position
        // every few seconds so a stuck/never-moving/combat-interrupted drive is visible in the log,
        // and re-issues the drive task toward the player's current position when the SUV has not
        // moved — GTA drops native tasks (same-frame warp+task, ambient event overrides), so
        // reassertion is the same repair the follower system uses.
        private void MonitorInboundDrive(int seatedCount)
        {
            var now = _gameBridge.GetGameTime();
            if (now - _lastInboundLogMs < InboundLogIntervalMs) return;
            _lastInboundLogMs = now;

            var suvPos = _gameBridge.GetVehiclePosition(_suv);
            var dist = _gameBridge.GetPlayerPosition().DistanceTo(suvPos);
            var moved = suvPos.DistanceTo(_lastInboundSuvPos);
            _lastInboundSuvPos = suvPos;

            var inCombat = 0;
            foreach (var handle in _rolesByHandle.Keys)
            {
                if (_gameBridge.IsPedInCombat(handle)) inCombat++;
            }

            FileLogger.AI(
                $"SupportSquadManager.Inbound: suv={_suv} pos=({suvPos.X:F1}, {suvPos.Y:F1}, {suvPos.Z:F1}) " +
                $"distToPlayer={dist:F1} movedSinceLast={moved:F1} seated={seatedCount}/{_rolesByHandle.Count} inCombat={inCombat}");

            if (moved < DriveStallEpsilon && dist > DismountRange)
            {
                FileLogger.AI($"SupportSquadManager.Inbound: SUV {_suv} stalled - reasserting drive task toward player");
                _gameBridge.TaskVehicleDriveToCoord(_suv, _gameBridge.GetPlayerPosition(), DriveSpeed, DriveStopRange);
            }
        }

        private void UpdateEngaging(IReadOnlyList<EnemyTarget> enemies)
        {
            if (_activeZone == null) return;
            _stance.Update(_activeZone.Center, _activeZone.Radius, AliveHandles(), enemies, _rolesByHandle);
        }

        private void ClearActiveSquad()
        {
            if (!HasActiveSquad) return;

            if (_suv != -1) _gameBridge.DeleteVehicle(_suv);

            FileLogger.AI("SupportSquadManager.Update: support squad wiped out, call re-enabled");
            HasActiveSquad = false;
            _activeZone = null;
            _suv = -1;
            _phase = Phase.None;
        }

        /// <summary>
        /// Force-despawns the active squad: removes blips and despawns every still-tracked ally,
        /// then deletes the SUV and resets state via <see cref="ClearActiveSquad"/>. Used by mod
        /// shutdown/reload so a support squad never survives past the manager that owns it.
        /// </summary>
        public void DespawnSquad()
        {
            if (!HasActiveSquad) return;

            foreach (var handle in _rolesByHandle.Keys)
            {
                _pedBlip.RemoveBlipForPed(handle);
                _pedDespawn.DespawnPed(handle);
            }
            _rolesByHandle.Clear();

            FileLogger.AI("SupportSquadManager.DespawnSquad: support squad force-despawned");
            ClearActiveSquad();
        }
    }
}
