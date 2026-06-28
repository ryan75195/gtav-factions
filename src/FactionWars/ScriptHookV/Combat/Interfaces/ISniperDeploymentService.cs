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
    }
}
