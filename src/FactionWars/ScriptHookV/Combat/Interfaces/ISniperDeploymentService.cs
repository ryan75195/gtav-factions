using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Combat.Interfaces
{
    /// <summary>Positions snipers on high ground and manages their close-range sidearm swap.</summary>
    public interface ISniperDeploymentService
    {
        /// <summary>Perches and guard-tasks the ped if its role is Sniper; otherwise no-op.</summary>
        void DeployIfSniper(int pedHandle, DefenderRoleConfig roleConfig, Vector3 zoneCenter);

        /// <summary>Swaps the sniper to a sidearm when a threat is within range, else the rifle.</summary>
        void UpdateCloseDefense(int sniperHandle, IReadOnlyList<Vector3> threatPositions);

        /// <summary>Drops cached weapon state for any sniper not in <paramref name="activeSniperHandles"/>
        /// (e.g. boarded a vehicle, which holsters the rifle, or died). Without this the de-dup skips a
        /// returning sniper because its cached weapon still matches the desired one, leaving the rifle
        /// holstered after a vehicle exit. Call each tick with the currently on-foot snipers.</summary>
        void RetainOnly(IReadOnlyList<int> activeSniperHandles);
    }
}
