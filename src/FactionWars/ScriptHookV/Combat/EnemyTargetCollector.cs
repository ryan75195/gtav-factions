using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;

namespace FactionWars.ScriptHookV.Combat
{
    public class EnemyTargetCollector : IEnemyTargetCollector
    {
        private readonly IGameBridge _gameBridge;

        public EnemyTargetCollector(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        public IReadOnlyList<EnemyTarget> Collect(IReadOnlyList<int> hostileHandles, Vector3 center, float radius)
        {
            var result = new List<EnemyTarget>();
            if (hostileHandles == null) return result;

            foreach (var handle in hostileHandles)
            {
                var position = _gameBridge.GetPedPosition(handle);
                if (center.DistanceTo(position) <= radius)
                {
                    result.Add(new EnemyTarget(handle, position));
                }
            }

            return result;
        }
    }
}
