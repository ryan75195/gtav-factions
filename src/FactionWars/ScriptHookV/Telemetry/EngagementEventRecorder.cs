using System;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Telemetry.Interfaces;

namespace FactionWars.ScriptHookV.Telemetry
{
    /// <summary>
    /// Each tick, drains the squad controller's buffered engagement phase-change events and forwards
    /// them to the engagement event sink. Wrapped so a bad frame logs and is swallowed — telemetry
    /// must never crash the game tick. Draining on a slower cadence than the controller's decisions
    /// loses no fidelity: each transition is timestamped when it occurred.
    /// </summary>
    public sealed class EngagementEventRecorder
    {
        private readonly ISquadEngagementStateSource _source;
        private readonly IEngagementEventSink _sink;

        public EngagementEventRecorder(ISquadEngagementStateSource source, IEngagementEventSink sink)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void Update()
        {
            try
            {
                var transitions = _source.DrainEngagementTransitions();
                for (int i = 0; i < transitions.Count; i++)
                {
                    _sink.Write(transitions[i]);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("EngagementEventRecorder.Update failed", ex);
            }
        }
    }
}
