using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Services
{
    public class TargetAssignmentResolver : ITargetAssignmentResolver
    {
        public IReadOnlyDictionary<int, int> Assign(IReadOnlyList<BodyguardPosition> bodyguards, IReadOnlyList<EnemyTarget> enemies)
        {
            var result = new Dictionary<int, int>();
            if (bodyguards == null || bodyguards.Count == 0 || enemies == null || enemies.Count == 0)
            {
                return result;
            }

            var load = new Dictionary<int, int>();
            foreach (var enemy in enemies)
            {
                load[enemy.Handle] = 0;
            }

            foreach (var bodyguard in bodyguards)
            {
                int minLoad = load.Values.Min();
                EnemyTarget best = enemies[0];
                float bestDistance = float.MaxValue;
                foreach (var enemy in enemies)
                {
                    if (load[enemy.Handle] != minLoad) continue;
                    float distance = bodyguard.Position.DistanceTo(enemy.Position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = enemy;
                    }
                }

                result[bodyguard.Handle] = best.Handle;
                load[best.Handle]++;
            }

            return result;
        }
    }
}
