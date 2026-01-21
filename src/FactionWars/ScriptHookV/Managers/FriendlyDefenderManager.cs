using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages friendly defenders that spawn when the player enters their own territory.
    /// Defenders patrol the zone independently (NOT as followers) and despawn when player exits.
    /// </summary>
    public class FriendlyDefenderManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private string _playerFactionId;

        private readonly Dictionary<DefenderTier, string> _modelsByTier;
        private readonly Dictionary<string, List<int>> _spawnedPedsByZone; // zoneId -> pedHandles

        private const float WanderRadius = 40f;

        /// <summary>
        /// Creates a new FriendlyDefenderManager instance.
        /// </summary>
        /// <param name="gameBridge">The game bridge for native function calls.</param>
        /// <param name="allocationService">Service for getting zone defender allocations.</param>
        /// <param name="pedSpawningService">Service for spawning peds.</param>
        /// <param name="defenderTierService">Service for defender tier configurations.</param>
        /// <param name="pedBlipService">Service for managing ped blips.</param>
        /// <param name="playerFactionId">The player's current faction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public FriendlyDefenderManager(
            IGameBridge gameBridge,
            IZoneDefenderAllocationService allocationService,
            IPedSpawningService pedSpawningService,
            IDefenderTierService defenderTierService,
            IPedBlipService pedBlipService,
            string playerFactionId)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _defenderTierService = defenderTierService ?? throw new ArgumentNullException(nameof(defenderTierService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));

            _modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_lost_01" },
                { DefenderTier.Medium, "g_m_y_lost_02" },
                { DefenderTier.Heavy, "g_m_y_lost_03" }
            };

            _spawnedPedsByZone = new Dictionary<string, List<int>>();
        }

        /// <summary>
        /// Sets the player's current faction and despawns all existing defenders.
        /// </summary>
        /// <param name="factionId">The new faction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if factionId is null or empty.</exception>
        public void SetPlayerFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                throw new ArgumentNullException(nameof(factionId));
            DespawnAllDefenders();
            _playerFactionId = factionId;
        }

        /// <summary>
        /// Called when the player enters a zone. Spawns friendly defenders if the zone
        /// belongs to the player's faction.
        /// </summary>
        /// <param name="zone">The zone that was entered.</param>
        public void OnZoneEntered(Zone zone)
        {
            if (zone == null) return;
            if (zone.OwnerFactionId != _playerFactionId) return;

            var allocation = _allocationService.GetAllocation(_playerFactionId, zone.Id);
            if (allocation == null) return;

            var spawnedPeds = new List<int>();

            foreach (DefenderTier tier in Enum.GetValues(typeof(DefenderTier)))
            {
                var count = allocation.GetTroopCount(tier);
                if (count <= 0) continue;

                var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_lost_01";
                var tierConfig = _defenderTierService.GetTierConfig(tier);

                for (int i = 0; i < count; i++)
                {
                    if (!_pedSpawningService.CanSpawn()) break;

                    var spawnPos = CalculateSpawnPosition(zone.Center, i, count);
                    var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, _playerFactionId, zone.Id);
                    if (!pedHandle.IsValid) continue;

                    // Set friendly relationship with player BEFORE configuring combat
                    _gameBridge.SetPedAsFriendly(pedHandle.Handle);
                    ConfigureDefenderCombat(pedHandle.Handle, tierConfig);
                    _gameBridge.TaskPedWanderInArea(pedHandle.Handle, zone.Center, WanderRadius);
                    _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.LightBlue);
                    spawnedPeds.Add(pedHandle.Handle);
                }
            }

            if (spawnedPeds.Count > 0)
                _spawnedPedsByZone[zone.Id] = spawnedPeds;
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns all friendly defenders
        /// that were spawned for that zone.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnZoneExited(Zone zone)
        {
            if (zone == null) return;
            if (!_spawnedPedsByZone.TryGetValue(zone.Id, out var peds)) return;

            foreach (var pedHandle in peds)
            {
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _gameBridge.DeletePed(pedHandle);
            }
            _spawnedPedsByZone.Remove(zone.Id);
        }

        /// <summary>
        /// Despawns all friendly defenders across all zones.
        /// </summary>
        public void DespawnAllDefenders()
        {
            foreach (var zonePeds in _spawnedPedsByZone.Values)
            {
                foreach (var pedHandle in zonePeds)
                {
                    _pedBlipService.RemoveBlipForPed(pedHandle);
                    _gameBridge.DeletePed(pedHandle);
                }
            }
            _spawnedPedsByZone.Clear();
        }

        /// <summary>
        /// Gets the number of spawned defenders for a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>The number of spawned defenders, or 0 if none.</returns>
        public int GetSpawnedDefenderCount(string zoneId)
        {
            return _spawnedPedsByZone.TryGetValue(zoneId, out var peds) ? peds.Count : 0;
        }

        /// <summary>
        /// Calculates the spawn position for a defender based on the zone center and
        /// the defender's index in the spawn sequence.
        /// </summary>
        private Vector3 CalculateSpawnPosition(Vector3 center, int index, int totalCount)
        {
            var angle = (2 * Math.PI * index) / Math.Max(totalCount, 1);
            var distance = 30f + (index % 3) * 10f;
            return new Vector3(
                center.X + (float)(Math.Cos(angle) * distance),
                center.Y + (float)(Math.Sin(angle) * distance),
                center.Z);
        }

        /// <summary>
        /// Configures a defender's combat attributes based on their tier configuration.
        /// </summary>
        private void ConfigureDefenderCombat(int pedHandle, DefenderTierConfig tierConfig)
        {
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);
            // Give pistol as secondary weapon for drive-by shooting
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);
        }
    }
}
