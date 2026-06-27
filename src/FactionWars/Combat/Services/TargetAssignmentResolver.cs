using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    public class TargetAssignmentResolver : ITargetAssignmentResolver
    {
        public IReadOnlyDictionary<int, int> Assign(
            IReadOnlyList<BodyguardPosition> bodyguards,
            IReadOnlyList<EnemyTarget> enemies,
            IReadOnlyDictionary<int, int> previousAssignment)
        {
            var result = new Dictionary<int, int>();
            if (bodyguards == null || bodyguards.Count == 0 || enemies == null || enemies.Count == 0)
            {
                return result;
            }

            var positions = new Dictionary<int, Vector3>();
            var load = new Dictionary<int, int>();
            foreach (var enemy in enemies)
            {
                positions[enemy.Handle] = enemy.Position;
                load[enemy.Handle] = 0;
            }

            var pending = RetainStableTargets(bodyguards, positions, previousAssignment, result, load);

            foreach (var bodyguard in pending)
            {
                int target = PickDispersedTarget(bodyguard, positions, load, result.Values);
                result[bodyguard.Handle] = target;
                load[target]++;
            }

            return result;
        }

        private static List<BodyguardPosition> RetainStableTargets(
            IReadOnlyList<BodyguardPosition> bodyguards,
            Dictionary<int, Vector3> positions,
            IReadOnlyDictionary<int, int> previousAssignment,
            Dictionary<int, int> result,
            Dictionary<int, int> load)
        {
            var pending = new List<BodyguardPosition>();
            foreach (var bodyguard in bodyguards)
            {
                if (previousAssignment != null
                    && previousAssignment.TryGetValue(bodyguard.Handle, out var prior)
                    && positions.ContainsKey(prior))
                {
                    result[bodyguard.Handle] = prior;
                    load[prior]++;
                }
                else
                {
                    pending.Add(bodyguard);
                }
            }

            return pending;
        }

        private static int PickDispersedTarget(
            BodyguardPosition bodyguard,
            Dictionary<int, Vector3> positions,
            Dictionary<int, int> load,
            IEnumerable<int> assignedTargets)
        {
            int minLoad = load.Values.Min();
            var assignedPositions = assignedTargets.Select(h => positions[h]).ToList();

            int best = -1;
            float bestDispersion = float.MinValue;
            float bestProximity = float.MaxValue;
            foreach (var entry in positions)
            {
                if (load[entry.Key] != minLoad) continue;

                float dispersion = NearestDistance(entry.Value, assignedPositions);
                float proximity = bodyguard.Position.DistanceTo(entry.Value);
                if (dispersion > bestDispersion
                    || (dispersion == bestDispersion && proximity < bestProximity))
                {
                    best = entry.Key;
                    bestDispersion = dispersion;
                    bestProximity = proximity;
                }
            }

            return best;
        }

        private static float NearestDistance(Vector3 point, List<Vector3> others)
        {
            if (others.Count == 0) return 0f;

            float nearest = float.MaxValue;
            foreach (var other in others)
            {
                float distance = point.DistanceTo(other);
                if (distance < nearest) nearest = distance;
            }

            return nearest;
        }
    }
}
