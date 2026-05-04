using System;
using System.Collections.Generic;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Interfaces
{
    public interface IFactionSnapshotBuilder
    {
        IReadOnlyList<FactionSnapshot> Build(DateTime timestamp, long playTimeSeconds);
    }
}
