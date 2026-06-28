using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>Default <see cref="ISquadEngagementResolver"/>. Advance until in range + LOS, then
    /// engage; drop back to advance only past the hysteresis band or after sustained LOS loss.</summary>
    public sealed class SquadEngagementResolver : ISquadEngagementResolver
    {
        private const float HysteresisFactor = 1.3f;

        private readonly IEngageRangeProvider _rangeProvider;

        public SquadEngagementResolver(IEngageRangeProvider rangeProvider)
        {
            _rangeProvider = rangeProvider ?? throw new ArgumentNullException(nameof(rangeProvider));
        }

        public EngageDecision Resolve(
            float distToTarget,
            bool hasLineOfSight,
            DefenderRole role,
            EngagePhase currentPhase,
            int consecutiveLosMisses)
        {
            float range = _rangeProvider.For(role);
            int losMisses = hasLineOfSight ? 0 : consecutiveLosMisses + 1;

            if (currentPhase == EngagePhase.Engage)
            {
                // Once engaged, drop back to advance ONLY when the target moves out of range.
                // Do NOT drop on transient line-of-sight loss: TaskCombatPed already repositions
                // for LOS, and re-tasking on every LOS blip caused an aim/run flicker. losMisses
                // is still tracked for telemetry but no longer flips the phase.
                bool rangeBroken = distToTarget > range * HysteresisFactor;
                var phase = rangeBroken ? EngagePhase.Advance : EngagePhase.Engage;
                return new EngageDecision(phase, range, losMisses);
            }

            bool canEngage = distToTarget <= range && hasLineOfSight;
            return new EngageDecision(canEngage ? EngagePhase.Engage : EngagePhase.Advance, range, losMisses);
        }
    }
}
