using System;

namespace FactionWars.Telemetry.Interfaces
{
    public interface ITelemetryService : IDisposable
    {
        void Update(float deltaTimeSeconds);
        void Tick();
    }
}
