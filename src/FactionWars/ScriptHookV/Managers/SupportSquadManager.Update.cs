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
                _rolesByHandle.Remove(handle);
            }
        }

        private void UpdateInbound()
        {
            var anySeated = false;
            foreach (var handle in _rolesByHandle.Keys)
            {
                if (_gameBridge.IsPedInVehicle(handle))
                {
                    anySeated = true;
                    break;
                }
            }

            // No ally still seated means the SUV is effectively gone (destroyed/ejected); treat
            // that the same as "arrived" so the squad falls back to on-foot Search & Destroy.
            var suvGone = !anySeated;
            var arrived = !suvGone
                && _gameBridge.GetPlayerPosition().DistanceTo(_gameBridge.GetVehiclePosition(_suv)) < DismountRange;

            if (!suvGone && !arrived) return;

            foreach (var handle in _rolesByHandle.Keys)
            {
                if (_gameBridge.IsPedInVehicle(handle))
                {
                    _gameBridge.TaskPedLeaveVehicle(handle);
                }
            }

            _phase = Phase.Engaging;
            FileLogger.AI($"SupportSquadManager.Update: squad dismounting (suvGone={suvGone}), entering Search & Destroy");
        }

        private void UpdateEngaging(IReadOnlyList<EnemyTarget> enemies)
        {
            if (_activeZone == null) return;
            _stance.Update(_activeZone.Center, _activeZone.Radius, AliveHandles(), enemies, _rolesByHandle);
        }

        private void ClearActiveSquad()
        {
            if (!HasActiveSquad) return;

            FileLogger.AI("SupportSquadManager.Update: support squad wiped out, call re-enabled");
            HasActiveSquad = false;
            _activeZone = null;
            _suv = -1;
            _phase = Phase.None;
        }
    }
}
