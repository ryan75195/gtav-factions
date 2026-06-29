using System;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>Persists squad engagement phase-change events. Writes MUST be safe before
    /// <see cref="SetSaveFile"/> is called — implementations buffer until the save folder is known.</summary>
    public interface IEngagementEventSink : IDisposable
    {
        void Write(EngagementTransition e);

        void SetSaveFile(string saveFilename);
    }
}
