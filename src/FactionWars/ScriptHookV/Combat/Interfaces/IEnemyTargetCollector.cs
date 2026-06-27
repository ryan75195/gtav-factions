using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Combat.Interfaces
{
    /// <summary>
    /// Reads live ped positions and returns those hostile handles within a radius of a centre.
    /// </summary>
    public interface IEnemyTargetCollector
    {
        IReadOnlyList<EnemyTarget> Collect(IReadOnlyList<int> hostileHandles, Vector3 center, float radius);
    }
}
