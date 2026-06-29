using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class SquadStanceController : ISquadEngagementStateSource
    {
        private readonly Dictionary<int, SquadEngagementState> _engagementState = new Dictionary<int, SquadEngagementState>();
        private readonly List<EngagementTransition> _transitions = new List<EngagementTransition>();

        bool ISquadEngagementStateSource.TryGetEngagementState(int handle, out SquadEngagementState state)
            => _engagementState.TryGetValue(handle, out state);

        IReadOnlyList<EngagementTransition> ISquadEngagementStateSource.DrainEngagementTransitions()
        {
            if (_transitions.Count == 0)
            {
                return Array.Empty<EngagementTransition>();
            }

            var drained = _transitions.ToArray();
            _transitions.Clear();
            return drained;
        }

        // Stores the current engagement snapshot and, on a phase change (Reason != None), appends a
        // transition event stamped with the game time it occurred. Called once per engaged ped/tick.
        private void RecordEngagementTelemetry(int pedHandle, EngagePhase priorPhase, EngageDecision decision, bool los, int msSinceLos, int nowMs, float dist)
        {
            _engagementState[pedHandle] = new SquadEngagementState(decision.Phase, los, msSinceLos);
            if (decision.Reason == EngagePhaseChangeReason.None)
            {
                return;
            }

            _transitions.Add(new EngagementTransition(pedHandle, nowMs, priorPhase, decision.Phase, decision.Reason, dist, los, msSinceLos));
        }
    }
}
