using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Combat
{
    /// <inheritdoc />
    public sealed class SniperDeploymentService : ISniperDeploymentService
    {
        public const float SidearmThresholdMeters = 15f;
        private const string Rifle = "WEAPON_SNIPERRIFLE";
        private const string Sidearm = "weapon_pistol";

        private readonly IPerchResolver _perchResolver;
        private readonly IGameBridge _gameBridge;
        private readonly Dictionary<int, string> _lastWeapon = new Dictionary<int, string>();

        public SniperDeploymentService(IPerchResolver perchResolver, IGameBridge gameBridge)
        {
            _perchResolver = perchResolver;
            _gameBridge = gameBridge;
        }

        public void DeployIfSniper(int pedHandle, DefenderRoleConfig roleConfig, Vector3 zoneCenter)
        {
            if (roleConfig.Role != DefenderRole.Sniper)
                return;

            var perch = _perchResolver.Resolve(
                zoneCenter,
                PerchSampling.DefaultSearchRadius,
                PerchSampling.DefaultSampleCount,
                (x, y) => _gameBridge.GetGroundZ(x, y, zoneCenter.Z + PerchSampling.ProbeHeight));

            _gameBridge.SetPedPosition(pedHandle, perch);
            _gameBridge.TaskGuardArea(pedHandle, perch, PerchSampling.GuardRadius);
            _lastWeapon[pedHandle] = Rifle;
            FileLogger.AI($"SniperDeployment: ped {pedHandle} perched at ({perch.X:F1},{perch.Y:F1},{perch.Z:F1})");
        }

        public void UpdateCloseDefense(int sniperHandle, IReadOnlyList<Vector3> threatPositions)
        {
            if (threatPositions == null) threatPositions = System.Array.Empty<Vector3>();
            var sniperPos = _gameBridge.GetPedPosition(sniperHandle);
            bool threatClose = false;
            foreach (var threat in threatPositions)
            {
                if (sniperPos.DistanceTo(threat) <= SidearmThresholdMeters)
                {
                    threatClose = true;
                    break;
                }
            }

            var desired = threatClose ? Sidearm : Rifle;
            if (_lastWeapon.TryGetValue(sniperHandle, out var current) && current == desired)
                return;

            _lastWeapon[sniperHandle] = desired;
            _gameBridge.SetPedActiveWeapon(sniperHandle, desired);
        }
    }
}
