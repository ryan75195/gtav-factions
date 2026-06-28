using System;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    /// <summary>
    /// Destination for behavior samples. Mirrors the strategic telemetry sink's lifecycle:
    /// buffer until the save directory is known via <see cref="SetSaveFile"/>, then append.
    /// </summary>
    public interface IBehaviorTraceSink : IDisposable
    {
        void Write(BehaviorSampleRow row);

        void SetSaveFile(string saveFilename);
    }
}
