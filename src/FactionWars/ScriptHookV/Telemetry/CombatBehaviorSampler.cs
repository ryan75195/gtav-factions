using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.ScriptHookV.Telemetry
{
    /// <summary>
    /// Periodically observes every tracked combat ped and writes one <see cref="BehaviorSampleRow"/>
    /// per live ped to a trace sink. Lives in ScriptHookV (not the portable Telemetry namespace) so it
    /// can report failures via <see cref="FileLogger"/>. All work is wrapped so a bad frame logs and is
    /// swallowed — sampling must never crash the game tick.
    /// </summary>
    public sealed class CombatBehaviorSampler
    {
        private readonly IGameBridge _gameBridge;
        private readonly IReadOnlyList<ITrackedCombatantSource> _sources;
        private readonly IBehaviorTraceSink _sink;
        private readonly ISquadEngagementStateSource? _engagementState;
        private readonly int _sampleIntervalMs;
        private int _lastSampleMs;

        public CombatBehaviorSampler(
            IGameBridge gameBridge,
            IReadOnlyList<ITrackedCombatantSource> sources,
            IBehaviorTraceSink sink,
            ISquadEngagementStateSource? engagementState = null,
            int sampleIntervalMs = 1000)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _engagementState = engagementState;
            _sampleIntervalMs = sampleIntervalMs;
            _lastSampleMs = _gameBridge.GetGameTime();
        }

        /// <summary>Samples on a fixed interval; cheap no-op between intervals. Never throws.</summary>
        public void Update()
        {
            try
            {
                int now = _gameBridge.GetGameTime();
                if (now - _lastSampleMs < _sampleIntervalMs)
                {
                    return;
                }

                _lastSampleMs = now;
                SampleAll(now);
            }
            catch (Exception ex)
            {
                FileLogger.Error("CombatBehaviorSampler.Update failed", ex);
            }
        }

        private void SampleAll(int sampleMs)
        {
            var live = GatherLive();
            var playerPos = _gameBridge.GetPlayerPosition();
            foreach (var entry in live)
            {
                _sink.Write(BuildRow(entry, live, playerPos, sampleMs));
            }
        }

        private List<LiveCombatant> GatherLive()
        {
            var result = new List<LiveCombatant>();
            foreach (var source in _sources)
            {
                foreach (var combatant in source.GetTrackedCombatants())
                {
                    int handle = combatant.Handle;
                    if (!_gameBridge.DoesPedExist(handle) || !_gameBridge.IsPedAlive(handle))
                    {
                        continue;
                    }

                    result.Add(new LiveCombatant(combatant, _gameBridge.GetPedPosition(handle)));
                }
            }

            return result;
        }

        private BehaviorSampleRow BuildRow(
            LiveCombatant self,
            List<LiveCombatant> all,
            Vector3 playerPos,
            int sampleMs)
        {
            int handle = self.Combatant.Handle;
            Vector3 pos = self.Position;
            LiveCombatant? nearest = NearestHostile(self, all);
            var row = new BehaviorSampleRow
            {
                SampleMs = sampleMs,
                Handle = handle,
                Kind = self.Combatant.Kind,
                Role = self.Combatant.Role,
                Weapon = _gameBridge.GetSelectedWeapon(handle),
                IsShooting = _gameBridge.IsPedShooting(handle),
                InCombat = _gameBridge.IsPedInCombat(handle),
                TargetHandle = nearest.HasValue ? nearest.Value.Combatant.Handle : -1,
                DistToTarget = nearest.HasValue ? Distance(pos, nearest.Value.Position) : -1f,
                DistToPlayer = Distance(pos, playerPos),
                PosX = pos.X,
                PosY = pos.Y,
                PosZ = pos.Z,
                InVehicle = _gameBridge.IsPedInVehicle(handle),
                IsFollowingPlayer = _gameBridge.IsPedFollowingPlayer(handle),
                Health = _gameBridge.GetPedHealth(handle),
                CombatAbility = _gameBridge.GetPedCombatAbilityValue(handle)
            };

            if (_engagementState != null && _engagementState.TryGetEngagementState(handle, out var es))
            {
                row.HasLineOfSight = es.HasLineOfSight;
                row.EnginePhase = es.Phase.ToString();
                row.MsSinceLos = es.MsSinceLos;
            }

            return row;
        }

        private static LiveCombatant? NearestHostile(LiveCombatant self, List<LiveCombatant> all)
        {
            LiveCombatant? best = null;
            float bestDist = float.MaxValue;
            foreach (var other in all)
            {
                if (other.Combatant.Handle == self.Combatant.Handle ||
                    !IsHostile(self.Combatant.Kind, other.Combatant.Kind))
                {
                    continue;
                }

                float dist = Distance(self.Position, other.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }

            return best;
        }

        private static bool IsHostile(CombatantKind a, CombatantKind b) => IsFriendly(a) != IsFriendly(b);

        private static bool IsFriendly(CombatantKind kind) =>
            kind == CombatantKind.Follower || kind == CombatantKind.FriendlyDefender;

        private static float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        private readonly struct LiveCombatant
        {
            public LiveCombatant(TrackedCombatant combatant, Vector3 position)
            {
                Combatant = combatant;
                Position = position;
            }

            public TrackedCombatant Combatant { get; }

            public Vector3 Position { get; }
        }
    }
}
