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

        /// <summary>How long line of sight must stay broken while engaged before we stop trusting
        /// <c>TaskCombatPed</c> to reposition and push the ped toward the target ourselves. Short
        /// blips (peeking, smoke, a passing body) are ignored to avoid aim/run flicker.</summary>
        private const int SustainedLosLossMs = 1500;

        /// <summary>Stop range used when advancing purely to regain line of sight: keep closing
        /// almost onto the target (e.g. to a rooftop parapet) until a shot opens up.</summary>
        private const float LosRepositionStopRange = 3f;

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
            int msSinceLastLos)
        {
            float range = _rangeProvider.For(role);

            if (currentPhase == EngagePhase.Engage)
            {
                // Drop back to advance when the target moves out of the hysteresis band...
                bool rangeBroken = distToTarget > range * HysteresisFactor;
                // ...or when line of sight has stayed broken long enough that TaskCombatPed is
                // clearly NOT going to reposition (e.g. an elevated ped aiming through a parapet).
                // A short LOS blip is ignored so the phase doesn't flicker on transient occlusion.
                bool losLostSustained = !hasLineOfSight && msSinceLastLos >= SustainedLosLossMs;

                if (rangeBroken)
                {
                    return new EngageDecision(EngagePhase.Advance, range, EngagePhaseChangeReason.RangeBroken);
                }

                if (losLostSustained)
                {
                    // Push almost onto the target to break the occlusion and regain a firing line.
                    return new EngageDecision(EngagePhase.Advance, LosRepositionStopRange, EngagePhaseChangeReason.LosReposition);
                }

                return new EngageDecision(EngagePhase.Engage, range, EngagePhaseChangeReason.None);
            }

            if (distToTarget <= range && hasLineOfSight)
            {
                return new EngageDecision(EngagePhase.Engage, range, EngagePhaseChangeReason.EngageAcquired);
            }

            // Advancing: close to engage range when we can see the target, but push right up to it
            // when we can't — the blocked sight line means we need a different vantage point.
            float stopRange = hasLineOfSight ? range : LosRepositionStopRange;
            return new EngageDecision(EngagePhase.Advance, stopRange, EngagePhaseChangeReason.None);
        }
    }
}
