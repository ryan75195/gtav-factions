using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Event args for when a defender dies.
    /// </summary>
    public class DefenderDiedEventArgs : EventArgs
    {
        public string ZoneId { get; }
        public int PedHandle { get; }
        public DefenderTier Tier { get; }

        public DefenderDiedEventArgs(string zoneId, int pedHandle, DefenderTier tier)
        {
            ZoneId = zoneId;
            PedHandle = pedHandle;
            Tier = tier;
        }
    }

    /// <summary>
    /// Event args for when a territory is lost (all defenders died).
    /// </summary>
    public class TerritoryLostEventArgs : EventArgs
    {
        public string ZoneId { get; }

        public TerritoryLostEventArgs(string zoneId)
        {
            ZoneId = zoneId;
        }
    }

    /// <summary>
    /// Manages friendly defenders that spawn when the player enters their own territory.
    /// Defenders patrol the zone independently (NOT as followers) and despawn when player exits.
    /// Supports death detection, replacement spawning from reserve, and territory loss.
    /// </summary>
    public class FriendlyDefenderManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private string _playerFactionId;

        private readonly Dictionary<DefenderTier, string> _modelsByTier;
        private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone; // zoneId -> (pedHandle -> tier)
        private readonly HashSet<string> _zonesInBattle; // Track zones with active battles
        private string? _currentZoneId; // Track the zone the player is currently in
        private readonly Random _random = new Random();

        /// <summary>
        /// Minimum spawn radius as a fraction of zone radius (30%).
        /// </summary>
        private const float MinSpawnRadiusFraction = 0.3f;

        /// <summary>
        /// Maximum number of defenders that can be spawned at once per zone.
        /// Additional allocated troops are held in reserve.
        /// </summary>
        public const int MaxSpawnedDefenders = 12;

        /// <summary>
        /// Raised when a defender dies.
        /// </summary>
        public event EventHandler<DefenderDiedEventArgs>? DefenderDied;

        /// <summary>
        /// Raised when all defenders in a zone die and the territory is lost.
        /// </summary>
        public event EventHandler<TerritoryLostEventArgs>? TerritoryLost;

        /// <summary>
        /// Creates a new FriendlyDefenderManager instance.
        /// </summary>
        /// <param name="gameBridge">The game bridge for native function calls.</param>
        /// <param name="allocationService">Service for getting zone defender allocations.</param>
        /// <param name="pedSpawningService">Service for spawning peds.</param>
        /// <param name="defenderTierService">Service for defender tier configurations.</param>
        /// <param name="pedBlipService">Service for managing ped blips.</param>
        /// <param name="zoneService">Service for zone operations.</param>
        /// <param name="playerFactionId">The player's current faction ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
        public FriendlyDefenderManager(
            IGameBridge gameBridge,
            IZoneDefenderAllocationService allocationService,
            IPedSpawningService pedSpawningService,
            IPedDespawnService pedDespawnService,
            IDefenderTierService defenderTierService,
            IPedBlipService pedBlipService,
            IZoneService zoneService,
            string playerFactionId)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _allocationService = allocationService ?? throw new ArgumentNullException(nameof(allocationService));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _pedDespawnService = pedDespawnService ?? throw new ArgumentNullException(nameof(pedDespawnService));
            _defenderTierService = defenderTierService ?? throw new ArgumentNullException(nameof(defenderTierService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));

            _modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_lost_01" },
                { DefenderTier.Medium, "g_m_y_lost_02" },
                { DefenderTier.Heavy, "g_m_y_lost_03" }
            };

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderTier>>();
            _zonesInBattle = new HashSet<string>();
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
        /// belongs to the player's faction. Respects MaxSpawnedDefenders limit.
        /// </summary>
        /// <param name="zone">The zone that was entered.</param>
        public void OnZoneEntered(Zone zone)
        {
            if (zone == null) return;

            // Track current zone for immediate spawning when allocating
            _currentZoneId = zone.Id;

            if (zone.OwnerFactionId != _playerFactionId) return;

            var allocation = _allocationService.GetAllocation(_playerFactionId, zone.Id);
            if (allocation == null) return;

            // Initialize tracking for this zone
            if (!_spawnedPedTierByZone.ContainsKey(zone.Id))
            {
                _spawnedPedTierByZone[zone.Id] = new Dictionary<int, DefenderTier>();
            }

            var totalSpawned = 0;

            foreach (DefenderTier tier in Enum.GetValues(typeof(DefenderTier)))
            {
                var count = allocation.GetTroopCount(tier);
                if (count <= 0) continue;

                var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_lost_01";
                var tierConfig = _defenderTierService.GetTierConfig(tier);

                for (int i = 0; i < count && totalSpawned < MaxSpawnedDefenders; i++)
                {
                    if (!_pedSpawningService.CanSpawn()) break;

                    var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius);
                    var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, _playerFactionId, zone.Id);
                    if (!pedHandle.IsValid) continue;

                    // Set friendly relationship with player
                    _gameBridge.SetPedAsFriendly(pedHandle.Handle);
                    ConfigureDefenderCombat(pedHandle.Handle, tierConfig);
                    // Wander using zone radius for full coverage
                    _gameBridge.TaskPedWanderInArea(pedHandle.Handle, zone.Center, zone.Radius);
                    _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.LightBlue);

                    // Track ped with its tier
                    _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                    totalSpawned++;
                }
            }
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns all friendly defenders
        /// that were spawned for that zone.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnZoneExited(Zone zone)
        {
            if (zone == null) return;

            // Clear current zone tracking
            if (_currentZoneId == zone.Id)
                _currentZoneId = null;

            if (!_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers)) return;

            foreach (var pedHandle in pedTiers.Keys)
            {
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.DespawnPed(pedHandle);
            }
            _spawnedPedTierByZone.Remove(zone.Id);
        }

        /// <summary>
        /// Called when a battle starts in a zone. Switches friendly defenders to sprinting
        /// wander mode so they actively search for and engage enemies.
        /// </summary>
        /// <param name="zoneId">The zone where the battle started.</param>
        public void OnBattleStarted(string zoneId)
        {
            _zonesInBattle.Add(zoneId);

            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers)) return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return;

            foreach (var pedHandle in pedTiers.Keys)
            {
                // Switch to sprinting wander for faster enemy engagement
                _gameBridge.TaskPedWanderInAreaSprinting(pedHandle, zone.Center, zone.Radius);
            }
        }

        /// <summary>
        /// Called when a battle ends in a zone. Switches friendly defenders back to walking
        /// wander mode for peaceful patrol.
        /// </summary>
        /// <param name="zoneId">The zone where the battle ended.</param>
        public void OnBattleEnded(string zoneId)
        {
            _zonesInBattle.Remove(zoneId);

            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers)) return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return;

            foreach (var pedHandle in pedTiers.Keys)
            {
                // Switch back to walking wander for peaceful patrol
                _gameBridge.TaskPedWanderInArea(pedHandle, zone.Center, zone.Radius);
            }
        }

        /// <summary>
        /// Despawns all friendly defenders across all zones.
        /// </summary>
        public void DespawnAllDefenders()
        {
            foreach (var zonePedTiers in _spawnedPedTierByZone.Values)
            {
                foreach (var pedHandle in zonePedTiers.Keys)
                {
                    _pedBlipService.RemoveBlipForPed(pedHandle);
                    _pedDespawnService.DespawnPed(pedHandle);
                }
            }
            _spawnedPedTierByZone.Clear();
            _zonesInBattle.Clear();
        }

        /// <summary>
        /// Gets the number of spawned defenders for a specific zone.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <returns>The number of spawned defenders, or 0 if none.</returns>
        public int GetSpawnedDefenderCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <summary>
        /// Gets the number of spawned defenders of a specific tier in a zone.
        /// </summary>
        /// <param name="zoneId">The zone ID.</param>
        /// <param name="tier">The defender tier to count.</param>
        /// <returns>The number of spawned defenders of that tier.</returns>
        public int GetSpawnedCountByTier(string zoneId, DefenderTier tier)
        {
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                return 0;

            return pedTiers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Updates defender state. Should be called each game tick.
        /// Checks for defender deaths, handles cleanup, spawns replacements, and triggers territory loss.
        /// </summary>
        public void Update()
        {
            var deadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();

            // Check all spawned defenders for death
            foreach (var kvp in _spawnedPedTierByZone)
            {
                var zoneId = kvp.Key;
                var pedTiers = kvp.Value;

                foreach (var pedKvp in pedTiers)
                {
                    var pedHandle = pedKvp.Key;
                    var tier = pedKvp.Value;

                    if (!_gameBridge.IsPedAlive(pedHandle))
                    {
                        deadPeds.Add((zoneId, pedHandle, tier));
                    }
                }
            }

            // Process each dead defender
            foreach (var (zoneId, pedHandle, tier) in deadPeds)
            {
                HandleDefenderDeath(zoneId, pedHandle, tier);
            }
        }

        /// <summary>
        /// Handles the death of a defender, including blip removal, allocation decrement,
        /// replacement spawning, and territory loss detection.
        /// </summary>
        private void HandleDefenderDeath(string zoneId, int pedHandle, DefenderTier tier)
        {
            // Remove from tracking
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            // Remove blip
            _pedBlipService.RemoveBlipForPed(pedHandle);

            // Delete the ped entity
            _gameBridge.DeletePed(pedHandle);

            var allocation = _allocationService.GetAllocation(_playerFactionId, zoneId);

            // ALWAYS decrement allocation when a defender dies (regardless of who killed them)
            // This must happen BEFORE checking for reserves, otherwise we create "phantom reserves"
            // where the spawned count decreases but allocation doesn't, making it look like reserves exist
            if (allocation != null)
            {
                allocation.RemoveTroops(tier, 1);
            }

            // Raise defender died event
            DefenderDied?.Invoke(this, new DefenderDiedEventArgs(zoneId, pedHandle, tier));

            // Try to spawn replacement from actual reserve (allocated > spawned means real reserves exist)
            TrySpawnReplacementFromReserve(zoneId, tier, allocation);

            // Check for territory loss (no spawned defenders AND no reserve)
            if (IsAllDefendersDead(zoneId, allocation))
            {
                HandleTerritoryLost(zoneId);
            }
        }

        /// <summary>
        /// Checks if all defenders are dead (no spawned and no reserve).
        /// </summary>
        private bool IsAllDefendersDead(string zoneId, ZoneDefenderAllocation? allocation)
        {
            var spawnedCount = GetSpawnedDefenderCount(zoneId);
            if (spawnedCount > 0) return false;

            if (allocation == null) return true;

            return allocation.TotalTroops == 0;
        }

        /// <summary>
        /// Handles territory loss when all defenders die.
        /// </summary>
        private void HandleTerritoryLost(string zoneId)
        {
            // Transfer zone to neutral
            _zoneService.TransferZoneOwnership(zoneId, null);

            // Raise event
            TerritoryLost?.Invoke(this, new TerritoryLostEventArgs(zoneId));
        }

        /// <summary>
        /// Tries to spawn a replacement defender from the reserve for a specific tier.
        /// Returns true if a replacement was spawned.
        /// </summary>
        private bool TrySpawnReplacementFromReserve(string zoneId, DefenderTier preferredTier, ZoneDefenderAllocation? allocation)
        {
            if (allocation == null) return false;

            var currentSpawned = GetSpawnedDefenderCount(zoneId);
            if (currentSpawned >= MaxSpawnedDefenders) return false;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return false;

            // First try the same tier as the one that died
            var allocatedCount = allocation.GetTroopCount(preferredTier);
            var spawnedOfTier = GetSpawnedCountByTier(zoneId, preferredTier);

            if (allocatedCount > spawnedOfTier)
            {
                SpawnSingleDefender(zoneId, preferredTier, zone.Center, zone.Radius);
                return true;
            }

            // If no reserve of that tier, try other tiers (highest first)
            var tiersToTry = new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic };
            foreach (var tier in tiersToTry)
            {
                if (tier == preferredTier) continue; // Already tried

                allocatedCount = allocation.GetTroopCount(tier);
                spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleDefender(zoneId, tier, zone.Center, zone.Radius);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Spawns a single defender of the specified tier.
        /// Uses sprinting wander if a battle is active in the zone.
        /// </summary>
        private void SpawnSingleDefender(string zoneId, DefenderTier tier, Vector3 center, float zoneRadius)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_lost_01";
            var tierConfig = _defenderTierService.GetTierConfig(tier);

            var spawnPos = CalculateRandomSpawnPosition(center, zoneRadius);
            var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, _playerFactionId, zoneId);

            if (!pedHandle.IsValid) return;

            // Set friendly relationship with player
            _gameBridge.SetPedAsFriendly(pedHandle.Handle);
            ConfigureDefenderCombat(pedHandle.Handle, tierConfig);

            // Use sprinting wander if battle is active, otherwise walking wander
            if (_zonesInBattle.Contains(zoneId))
            {
                _gameBridge.TaskPedWanderInAreaSprinting(pedHandle.Handle, center, zoneRadius);
            }
            else
            {
                _gameBridge.TaskPedWanderInArea(pedHandle.Handle, center, zoneRadius);
            }

            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.LightBlue);

            // Track ped with its tier
            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
            {
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderTier>();
            }
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
        }

        /// <summary>
        /// Handles when troops are allocated to a zone. If the player is currently
        /// in that zone (and it's their own zone), spawns the new defenders immediately.
        /// Respects MaxSpawnedDefenders limit.
        /// </summary>
        /// <param name="factionId">The faction that allocated troops.</param>
        /// <param name="zoneId">The zone troops were allocated to.</param>
        /// <param name="tier">The tier of troops allocated.</param>
        /// <param name="count">The number of troops allocated.</param>
        /// <param name="zoneCenter">The center of the zone for spawn positioning.</param>
        /// <param name="zoneRadius">The radius of the zone for spawn distribution.</param>
        public void OnTroopsAllocated(string factionId, string zoneId, DefenderTier tier, int count, Vector3 zoneCenter, float zoneRadius)
        {
            // Only spawn if this is the player's faction and they're in this zone
            if (factionId != _playerFactionId) return;
            if (_currentZoneId != zoneId) return;

            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_lost_01";
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var inBattle = _zonesInBattle.Contains(zoneId);

            // Get existing spawned peds for this zone to calculate spawn positions
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var existingPedTiers))
            {
                existingPedTiers = new Dictionary<int, DefenderTier>();
                _spawnedPedTierByZone[zoneId] = existingPedTiers;
            }

            for (int i = 0; i < count && existingPedTiers.Count < MaxSpawnedDefenders; i++)
            {
                if (!_pedSpawningService.CanSpawn()) break;

                var spawnPos = CalculateRandomSpawnPosition(zoneCenter, zoneRadius);
                var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, _playerFactionId, zoneId);
                if (!pedHandle.IsValid) continue;

                // Set friendly relationship with player
                _gameBridge.SetPedAsFriendly(pedHandle.Handle);
                ConfigureDefenderCombat(pedHandle.Handle, tierConfig);

                // Use sprinting wander if battle is active, otherwise walking wander
                if (inBattle)
                {
                    _gameBridge.TaskPedWanderInAreaSprinting(pedHandle.Handle, zoneCenter, zoneRadius);
                }
                else
                {
                    _gameBridge.TaskPedWanderInArea(pedHandle.Handle, zoneCenter, zoneRadius);
                }

                _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.LightBlue);

                // Track ped with its tier
                existingPedTiers[pedHandle.Handle] = tier;
            }
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses the zone's full radius for spawn area (30%-100%) and navmesh-based safe
        /// coordinates to avoid spawning on rooftops.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius)
        {
            var angle = _random.NextDouble() * 2 * Math.PI;
            var minRadius = zoneRadius * MinSpawnRadiusFraction;
            var distance = minRadius + (float)(_random.NextDouble() * (zoneRadius - minRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            // Use navmesh-based safe coordinate to avoid rooftop spawns
            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Configures a defender's combat attributes based on their tier configuration.
        /// </summary>
        private void ConfigureDefenderCombat(int pedHandle, DefenderTierConfig tierConfig)
        {
            // Give pistol first as secondary weapon for drive-by shooting
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            // Give tier-appropriate weapon last so it becomes the equipped/primary weapon
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);
        }
    }
}
