using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// While a battle is active in the player's zone, evicts drivers from nearby ambient cars (they
    /// leave and flee on foot) and handbrakes the emptied cars so they stop driving through the
    /// fight. The player's vehicle and any mod/scripted (persistent) vehicle are never touched.
    /// Native vehicle access is behind <see cref="IGameBridge"/>; the eligibility logic lives in
    /// <see cref="IAmbientTrafficSuppressor"/>.
    /// </summary>
    public sealed class AmbientTrafficController
    {
        private const float ScanRadius = 80f;
        private const int ScanThrottleMs = 750;

        private readonly IGameBridge _gameBridge;
        private readonly IAmbientTrafficSuppressor _suppressor;
        private readonly Func<bool> _isBattleActiveInPlayerZone;

        private bool _scannedOnce;
        private int _lastScanMs;

        public AmbientTrafficController(IGameBridge gameBridge, IAmbientTrafficSuppressor suppressor, Func<bool> isBattleActiveInPlayerZone)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _suppressor = suppressor ?? throw new ArgumentNullException(nameof(suppressor));
            _isBattleActiveInPlayerZone = isBattleActiveInPlayerZone ?? throw new ArgumentNullException(nameof(isBattleActiveInPlayerZone));
        }

        public void Update()
        {
            if (!_isBattleActiveInPlayerZone())
            {
                return;
            }

            int now = _gameBridge.GetGameTime();
            if (_scannedOnce && now - _lastScanMs < ScanThrottleMs)
            {
                return;
            }
            _scannedOnce = true;
            _lastScanMs = now;

            EvictNearbyAmbientDrivers();
        }

        private void EvictNearbyAmbientDrivers()
        {
            var center = _gameBridge.GetPlayerPosition();
            var handles = _gameBridge.GetNearbyVehicles(center, ScanRadius);
            if (handles == null || handles.Length == 0)
            {
                return;
            }

            var snapshots = new List<VehicleSnapshot>(handles.Length);
            var vehicleByDriver = new Dictionary<int, int>();
            foreach (var handle in handles)
            {
                int driver = _gameBridge.GetVehicleDriver(handle);
                snapshots.Add(new VehicleSnapshot(handle, _gameBridge.IsVehiclePersistent(handle), driver));
                if (driver != -1)
                {
                    vehicleByDriver[driver] = handle;
                }
            }

            var drivers = _suppressor.SelectDriversToEvict(snapshots, _gameBridge.GetPlayerVehicle());
            FileLogger.AI($"AmbientTraffic: scan at ({center.X:F0},{center.Y:F0}) found {handles.Length} vehicle(s), evicting {drivers.Count} driver(s)");
            foreach (var driver in drivers)
            {
                _gameBridge.TaskPedLeaveVehicle(driver);
                if (vehicleByDriver.TryGetValue(driver, out var vehicle))
                {
                    _gameBridge.SetVehicleHandbrake(vehicle, true);
                    FileLogger.AI($"AmbientTraffic: evicted driver {driver} from vehicle {vehicle} + handbrake");
                }
            }
        }
    }
}
