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
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages enemy attackers that spawn when the player enters their own zone that is under attack.
    /// Attackers are spawned based on the active battle's attacker troops and engage the player on sight.
    /// </summary>
    public class BattleAttackerManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IActiveBattleManager _activeBattleManager;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private string _playerFactionId;

        private readonly Dictionary<DefenderTier, string> _modelsByTier;
        private readonly Dictionary<string, Dictionary<int, DefenderTier>> _spawnedPedTierByZone;
        private readonly Dictionary<string, Dictionary<DefenderTier, int>> _despawnedAttackersByZone;
        private string? _currentBattleZoneId;

        private const float MinSpawnRadiusFraction = 0.3f;  // Min 30% of zone radius

        /// <summary>
        /// Maximum number of enemy attackers that can be spawned at once per zone.
        /// </summary>
        public const int MaxSpawnedAttackers = 12;

        /// <summary>
        /// Creates a new BattleAttackerManager instance.
        /// </summary>
        public BattleAttackerManager(
            IGameBridge gameBridge,
            IActiveBattleManager activeBattleManager,
            IPedSpawningService pedSpawningService,
            IPedDespawnService pedDespawnService,
            IDefenderTierService defenderTierService,
            IPedBlipService pedBlipService,
            IZoneService zoneService,
            string playerFactionId)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _activeBattleManager = activeBattleManager ?? throw new ArgumentNullException(nameof(activeBattleManager));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _pedDespawnService = pedDespawnService ?? throw new ArgumentNullException(nameof(pedDespawnService));
            _defenderTierService = defenderTierService ?? throw new ArgumentNullException(nameof(defenderTierService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));

            // Enemy faction ped models (hostile attackers)
            _modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_famca_01" },
                { DefenderTier.Medium, "g_m_y_famdnf_01" },
                { DefenderTier.Heavy, "g_m_y_famfor_01" }
            };

            _spawnedPedTierByZone = new Dictionary<string, Dictionary<int, DefenderTier>>();
            _despawnedAttackersByZone = new Dictionary<string, Dictionary<DefenderTier, int>>();
        }

        /// <summary>
        /// Called when the player enters a zone. If the zone is the player's zone and there's
        /// an active battle where player is defender, spawns enemy attackers.
        /// </summary>
        /// <param name="zone">The zone that was entered.</param>
        public void OnPlayerZoneEntered(Zone zone)
        {
            if (zone == null) return;

            FileLogger.Combat($"BattleAttackerManager: OnPlayerZoneEntered called for zone {zone.Id}, playerFactionId={_playerFactionId}");

            // Get battle for this zone
            var battle = _activeBattleManager.GetBattleForZone(zone.Id);
            if (battle == null)
            {
                FileLogger.Combat($"BattleAttackerManager: No active battle found for zone {zone.Id}");
                return;
            }

            FileLogger.Combat($"BattleAttackerManager: Found battle - Attacker={battle.AttackerFactionId}, Defender={battle.DefenderFactionId}, AttackerTroops={battle.TotalAttackerTroops}, DefenderTroops={battle.TotalDefenderTroops}");

            // Only spawn attackers if player is the defender (their zone is being attacked)
            if (battle.DefenderFactionId != _playerFactionId)
            {
                FileLogger.Combat($"BattleAttackerManager: Player ({_playerFactionId}) is not the defender ({battle.DefenderFactionId}), skipping");
                return;
            }

            FileLogger.Combat($"BattleAttackerManager: Player entered defended zone {zone.Id} under attack by {battle.AttackerFactionId}");

            _currentBattleZoneId = zone.Id;

            // Restore any previously despawned attackers to the battle
            // This handles the case where player exits, background sim depletes troops, then player re-enters
            RestoreDespawnedAttackersToBattle(zone.Id, battle);

            // Initialize tracking for this zone
            if (!_spawnedPedTierByZone.ContainsKey(zone.Id))
            {
                _spawnedPedTierByZone[zone.Id] = new Dictionary<int, DefenderTier>();
            }

            var totalSpawned = 0;
            var random = new Random();

            // Log per-tier values for debugging
            battle.AttackerTroops.TryGetValue(DefenderTier.Heavy, out var heavyCount);
            battle.AttackerTroops.TryGetValue(DefenderTier.Medium, out var mediumCount);
            battle.AttackerTroops.TryGetValue(DefenderTier.Basic, out var basicCount);
            FileLogger.Combat($"BattleAttackerManager: Per-tier attacker counts (after restore) - Heavy={heavyCount}, Medium={mediumCount}, Basic={basicCount}");

            // Spawn attackers based on battle.AttackerTroops
            foreach (DefenderTier tier in new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (!battle.AttackerTroops.TryGetValue(tier, out var count) || count <= 0) continue;

                var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
                var tierConfig = _defenderTierService.GetTierConfig(tier);

                FileLogger.Combat($"BattleAttackerManager: Attempting to spawn {count} {tier} attackers (totalSpawned={totalSpawned}, max={MaxSpawnedAttackers})");
                for (int i = 0; i < count && totalSpawned < MaxSpawnedAttackers; i++)
                {
                    if (!_pedSpawningService.CanSpawn())
                    {
                        FileLogger.Combat($"BattleAttackerManager: CanSpawn() returned false, breaking");
                        break;
                    }

                    var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
                    var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, battle.AttackerFactionId, zone.Id);
                    if (!pedHandle.IsValid)
                    {
                        FileLogger.Combat($"BattleAttackerManager: SpawnPed returned invalid handle");
                        continue;
                    }

                    // Configure as hostile attacker
                    ConfigureAttacker(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
                    _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);

                    // Track ped with its tier
                    _spawnedPedTierByZone[zone.Id][pedHandle.Handle] = tier;
                    totalSpawned++;
                }
            }

            FileLogger.Combat($"BattleAttackerManager: Spawned {totalSpawned} enemy attackers in {zone.Id}");
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns all attackers in that zone.
        /// Tracks despawned attackers so they can be restored if player re-enters.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnPlayerZoneExited(Zone zone)
        {
            if (zone == null) return;

            FileLogger.Combat($"BattleAttackerManager: Player exited zone {zone.Id}");

            if (_currentBattleZoneId == zone.Id)
                _currentBattleZoneId = null;

            if (!_spawnedPedTierByZone.TryGetValue(zone.Id, out var pedTiers)) return;

            // Track despawned attackers by tier so we can restore them if player re-enters
            // These attackers were despawned (not killed) so they should still exist for the battle
            TrackDespawnedAttackers(zone.Id, pedTiers);

            foreach (var pedHandle in pedTiers.Keys)
            {
                _pedBlipService.RemoveBlipForPed(pedHandle);
                _pedDespawnService.DespawnPed(pedHandle);
            }
            _spawnedPedTierByZone.Remove(zone.Id);
        }

        /// <summary>
        /// Despawns all attackers across all zones.
        /// </summary>
        public void DespawnAllAttackers()
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
            _currentBattleZoneId = null;
        }

        /// <summary>
        /// Gets the number of spawned attackers for a specific zone.
        /// </summary>
        public int GetSpawnedAttackerCount(string zoneId)
        {
            return _spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers) ? pedTiers.Count : 0;
        }

        /// <summary>
        /// Gets the number of spawned attackers of a specific tier in a zone.
        /// </summary>
        public int GetSpawnedCountByTier(string zoneId, DefenderTier tier)
        {
            if (!_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
                return 0;

            return pedTiers.Values.Count(t => t == tier);
        }

        /// <summary>
        /// Sets the player's faction ID for determining which zones the player is defending.
        /// Called when the player switches characters.
        /// </summary>
        /// <param name="factionId">The new player faction ID.</param>
        public void SetPlayerFaction(string factionId)
        {
            _playerFactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        }

        /// <summary>
        /// Updates attacker state. Should be called each game tick.
        /// Checks for deaths and reports kills to battle manager.
        /// </summary>
        public void Update()
        {
            if (_currentBattleZoneId == null) return;

            var deadPeds = new List<(string zoneId, int pedHandle, DefenderTier tier)>();

            // Check all spawned attackers for death
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

            // Process each dead attacker
            foreach (var (zoneId, pedHandle, tier) in deadPeds)
            {
                HandleAttackerDeath(zoneId, pedHandle, tier);
            }
        }

        /// <summary>
        /// Handles the death of an enemy attacker.
        /// </summary>
        private void HandleAttackerDeath(string zoneId, int pedHandle, DefenderTier tier)
        {
            FileLogger.Combat($"BattleAttackerManager: Attacker died in {zoneId}, tier={tier}");

            // Remove from tracking
            if (_spawnedPedTierByZone.TryGetValue(zoneId, out var pedTiers))
            {
                pedTiers.Remove(pedHandle);
            }

            // Remove blip and delete ped
            _pedBlipService.RemoveBlipForPed(pedHandle);
            _pedDespawnService.DespawnPed(pedHandle);

            // Report kill to active battle manager
            var battle = _activeBattleManager.GetBattleForZone(zoneId);
            if (battle != null && battle.IsPlayerPresent)
            {
                _activeBattleManager.ReportTroopKilled(zoneId, battle.AttackerFactionId, tier);
            }

            // Try to spawn replacement from remaining battle troops
            TrySpawnReplacement(zoneId, tier, battle);
        }

        /// <summary>
        /// Tries to spawn a replacement attacker from remaining battle troops.
        /// </summary>
        private bool TrySpawnReplacement(string zoneId, DefenderTier preferredTier, ActiveBattle? battle)
        {
            if (battle == null) return false;

            var currentSpawned = GetSpawnedAttackerCount(zoneId);
            if (currentSpawned >= MaxSpawnedAttackers) return false;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return false;

            // Try preferred tier first
            if (battle.AttackerTroops.TryGetValue(preferredTier, out var allocatedCount))
            {
                var spawnedOfTier = GetSpawnedCountByTier(zoneId, preferredTier);

                if (allocatedCount > spawnedOfTier)
                {
                    SpawnSingleAttacker(zoneId, preferredTier, battle.AttackerFactionId, zone);
                    FileLogger.Combat($"BattleAttackerManager: Spawned replacement {preferredTier} in {zoneId}");
                    return true;
                }
            }

            // Try other tiers (highest first)
            foreach (var tier in new[] { DefenderTier.Heavy, DefenderTier.Medium, DefenderTier.Basic })
            {
                if (tier == preferredTier) continue;

                if (battle.AttackerTroops.TryGetValue(tier, out allocatedCount))
                {
                    var spawnedOfTier = GetSpawnedCountByTier(zoneId, tier);

                    if (allocatedCount > spawnedOfTier)
                    {
                        SpawnSingleAttacker(zoneId, tier, battle.AttackerFactionId, zone);
                        FileLogger.Combat($"BattleAttackerManager: Spawned replacement {tier} in {zoneId}");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Spawns a single enemy attacker.
        /// </summary>
        private void SpawnSingleAttacker(string zoneId, DefenderTier tier, string attackerFactionId, Zone zone)
        {
            if (!_pedSpawningService.CanSpawn()) return;

            var model = _modelsByTier.TryGetValue(tier, out var m) ? m : "g_m_y_famca_01";
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var random = new Random();

            var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius, random);
            var pedHandle = _pedSpawningService.SpawnPed(model, spawnPos, attackerFactionId, zoneId);

            if (!pedHandle.IsValid) return;

            ConfigureAttacker(pedHandle.Handle, tierConfig, zone.Center, zone.Radius);
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Red);

            if (!_spawnedPedTierByZone.ContainsKey(zoneId))
            {
                _spawnedPedTierByZone[zoneId] = new Dictionary<int, DefenderTier>();
            }
            _spawnedPedTierByZone[zoneId][pedHandle.Handle] = tier;
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses the zone's full radius for spawn area and navmesh-based safe coordinates
        /// to avoid spawning on rooftops.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius, Random random)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var minRadius = zoneRadius * MinSpawnRadiusFraction;
            var distance = minRadius + (float)(random.NextDouble() * (zoneRadius - minRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            // Use navmesh-based safe coordinate to avoid rooftop spawns
            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Configures an enemy attacker's combat attributes and behavior.
        /// </summary>
        private void ConfigureAttacker(int pedHandle, DefenderTierConfig tierConfig, Vector3 zoneCenter, float wanderRadius)
        {
            // Give weapons
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Set as hostile wanderer - will engage player and followers on sight
            _gameBridge.SetPedAsHostileWanderer(pedHandle);

            // CRITICAL: Task to attack player immediately so they engage right away
            _gameBridge.SetPedToAttackPlayer(pedHandle);
        }

        /// <summary>
        /// Tracks despawned attackers by tier so they can be restored if player re-enters.
        /// </summary>
        private void TrackDespawnedAttackers(string zoneId, Dictionary<int, DefenderTier> pedTiers)
        {
            if (!_despawnedAttackersByZone.ContainsKey(zoneId))
            {
                _despawnedAttackersByZone[zoneId] = new Dictionary<DefenderTier, int>
                {
                    { DefenderTier.Basic, 0 },
                    { DefenderTier.Medium, 0 },
                    { DefenderTier.Heavy, 0 }
                };
            }

            // Count attackers by tier that are being despawned
            foreach (var tier in pedTiers.Values)
            {
                _despawnedAttackersByZone[zoneId][tier]++;
            }

            var basic = _despawnedAttackersByZone[zoneId][DefenderTier.Basic];
            var medium = _despawnedAttackersByZone[zoneId][DefenderTier.Medium];
            var heavy = _despawnedAttackersByZone[zoneId][DefenderTier.Heavy];
            FileLogger.Combat($"BattleAttackerManager: Tracked despawned attackers in {zoneId} - Basic={basic}, Medium={medium}, Heavy={heavy}");
        }

        /// <summary>
        /// Restores previously despawned attackers to the battle's attacker troop counts.
        /// This fixes the bug where background simulation depletes troops while player is away,
        /// causing no attackers to spawn when player re-enters.
        /// </summary>
        private void RestoreDespawnedAttackersToBattle(string zoneId, ActiveBattle battle)
        {
            if (!_despawnedAttackersByZone.TryGetValue(zoneId, out var despawnedCounts))
            {
                return;
            }

            foreach (var tier in new[] { DefenderTier.Basic, DefenderTier.Medium, DefenderTier.Heavy })
            {
                var count = despawnedCounts[tier];
                if (count > 0)
                {
                    battle.AddAttackerTroops(tier, count);
                    FileLogger.Combat($"BattleAttackerManager: Restored {count} {tier} attackers to battle in {zoneId}");
                }
            }

            // Clear the despawned tracking for this zone since we've restored them
            _despawnedAttackersByZone.Remove(zoneId);
        }
    }
}
