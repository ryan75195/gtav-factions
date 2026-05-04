using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages followers (bodyguards) in the game world.
    /// Coordinates between the domain-layer FollowerService and ScriptHookV game interactions.
    /// Handles spawning, despawning, and tracking follower peds.
    /// </summary>
    public class FollowerManager : IFollowerManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IFollowerService _followerService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IVehicleSeatPriorityService _seatPriorityService;
        private readonly Dictionary<DefenderTier, string> _modelsByTier;

        /// <summary>
        /// Raised when a follower dies in combat.
        /// </summary>
        public event EventHandler<Follower>? FollowerDied;

        /// <summary>
        /// Creates a new FollowerManager.
        /// </summary>
        /// <param name="gameBridge">The game bridge for game interactions.</param>
        /// <param name="followerService">The domain-layer follower service.</param>
        /// <param name="pedSpawningService">Service for spawning peds.</param>
        /// <param name="defenderTierService">Service for defender tier configurations.</param>
        /// <param name="pedBlipService">Service for managing ped blips on the minimap.</param>
        /// <param name="seatPriorityService">Service for coordinated vehicle seat assignment.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public FollowerManager(FollowerManagerDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _followerService = dependencies.FollowerService ?? throw new ArgumentNullException(nameof(dependencies.FollowerService));
            _pedSpawningService = dependencies.PedSpawningService ?? throw new ArgumentNullException(nameof(dependencies.PedSpawningService));
            _defenderTierService = dependencies.DefenderTierService ?? throw new ArgumentNullException(nameof(dependencies.DefenderTierService));
            _pedBlipService = dependencies.PedBlipService ?? throw new ArgumentNullException(nameof(dependencies.PedBlipService));
            _seatPriorityService = dependencies.SeatPriorityService ?? throw new ArgumentNullException(nameof(dependencies.SeatPriorityService));

            _modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Basic, "g_m_y_lost_01" },
                { DefenderTier.Medium, "g_m_y_lost_02" },
                { DefenderTier.Heavy, "g_m_y_lost_03" }
            };
        }

        public FollowerManager(params object?[] dependencies)
            : this(new FollowerManagerDependencies
            {
                GameBridge = (IGameBridge?)dependencies[0],
                FollowerService = (IFollowerService?)dependencies[1],
                PedSpawningService = (IPedSpawningService?)dependencies[2],
                DefenderTierService = (IDefenderTierService?)dependencies[3],
                PedBlipService = (IPedBlipService?)dependencies[4],
                SeatPriorityService = (IVehicleSeatPriorityService?)dependencies[5]
            })
        {
        }

        /// <summary>
        /// Sets the ped model to use for a specific defender tier.
        /// </summary>
        /// <param name="tier">The tier to configure.</param>
        /// <param name="modelName">The ped model name to use.</param>
        public void SetModelForTier(DefenderTier tier, string modelName)
        {
            _modelsByTier[tier] = modelName;
        }

        /// <summary>
        /// Recruits a new follower for the specified faction.
        /// Creates the follower in the domain layer and spawns the ped in the game world.
        /// Deducts the cost from the player's GTA V cash.
        /// </summary>
        /// <param name="factionId">The faction to recruit the follower for.</param>
        /// <param name="tier">The quality tier of the follower.</param>
        /// <returns>A result indicating success or failure with the recruited follower.</returns>
        /// <exception cref="ArgumentNullException">Thrown if factionId is null.</exception>
        public FollowerRecruitResult RecruitFollower(string factionId, DefenderTier tier)
        {
            if (string.IsNullOrEmpty(factionId))
                throw new ArgumentNullException(nameof(factionId));

            // Get cost for this tier
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var cost = tierConfig.Cost;

            // Check if player has enough money
            var playerMoney = _gameBridge.GetPlayerMoney();
            if (playerMoney < cost)
            {
                return FollowerRecruitResult.Failed(FollowerRecruitFailureReason.InsufficientFunds);
            }

            // First, recruit in the domain layer
            var recruitResult = _followerService.Recruit(factionId, tier);
            if (!recruitResult.Success)
            {
                return recruitResult;
            }

            var follower = recruitResult.Follower!;

            // Check if we can spawn a ped
            if (!_pedSpawningService.CanSpawn())
            {
                // Can't spawn, dismiss the follower we just created
                _followerService.DismissFollower(follower.Id);
                return FollowerRecruitResult.Failed(FollowerRecruitFailureReason.SpawnFailed);
            }

            // Get spawn position near player
            var playerPos = _gameBridge.GetPlayerPosition();
            var spawnPos = CalculateFollowerSpawnPosition(playerPos);

            // Get the model for this tier
            var modelName = _modelsByTier.TryGetValue(tier, out var model) ? model : "g_m_y_lost_01";

            // Spawn the ped
            var pedHandle = _pedSpawningService.SpawnPed(modelName, spawnPos, factionId, null);
            if (!pedHandle.IsValid)
            {
                // Spawn failed, dismiss the follower
                _followerService.DismissFollower(follower.Id);
                return FollowerRecruitResult.Failed(FollowerRecruitFailureReason.SpawnFailed);
            }

            // Update the follower with the ped handle
            follower.SetPedHandle(pedHandle.Handle);

            // Deduct money from player (only after successful spawn)
            _gameBridge.AddPlayerMoney(-cost);

            // Make the ped follow the player FIRST (sets up ped group, clears tasks)
            _gameBridge.SetPedAsFollower(pedHandle.Handle);

            // Configure combat stats AFTER follower setup (health must be set last to persist)
            ConfigureFollowerCombat(pedHandle.Handle, tierConfig);

            // Create a yellow blip on the minimap for this follower
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Yellow);

            return FollowerRecruitResult.Succeeded(follower);
        }

        /// <summary>
        /// Dismisses a specific follower by ID.
        /// Despawns the ped and removes the follower from the service.
        /// </summary>
        /// <param name="followerId">The ID of the follower to dismiss.</param>
        /// <returns>True if the follower was found and dismissed, false otherwise.</returns>
        public bool DismissFollower(Guid followerId)
        {
            var follower = _followerService.GetFollowerById(followerId);
            if (follower == null)
            {
                return false;
            }

            // Despawn the ped if it has a valid handle
            if (follower.PedHandle >= 0)
            {
                _pedBlipService.RemoveBlipForPed(follower.PedHandle);
                _gameBridge.DeletePed(follower.PedHandle);
            }

            // Remove from service
            return _followerService.DismissFollower(followerId);
        }

        /// <summary>
        /// Dismisses all followers belonging to a faction.
        /// Called when player switches characters.
        /// </summary>
        /// <param name="factionId">The faction to dismiss all followers for.</param>
        public void DismissAllFollowers(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                return;
            }

            // Get all followers and despawn their peds
            var followers = _followerService.GetFollowers(factionId);
            foreach (var follower in followers)
            {
                if (follower.PedHandle >= 0)
                {
                    _pedBlipService.RemoveBlipForPed(follower.PedHandle);
                    _gameBridge.DeletePed(follower.PedHandle);
                }
            }

            // Remove all from service
            _followerService.DismissAllFollowers(factionId);
        }

        /// <summary>
        /// Updates follower state. Should be called each game tick.
        /// Checks for follower deaths and handles cleanup.
        /// Also manages vehicle enter/exit behavior with coordinated seat assignment.
        /// </summary>
        /// <param name="factionId">The faction to update followers for.</param>
        public void Update(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                return;
            }

            var followers = _followerService.GetFollowers(factionId);

            var playerInVehicle = _gameBridge.IsPlayerInVehicle();
            var playerVehicle = playerInVehicle ? _gameBridge.GetPlayerVehicle() : -1;
            var aliveFollowerHandles = GetAliveFollowerHandles(followers);

            if (playerInVehicle && playerVehicle >= 0)
                AssignFollowersToVehicle(aliveFollowerHandles, playerVehicle);
            else
                ExitFollowersFromVehicles(aliveFollowerHandles);
        }

        private List<int> GetAliveFollowerHandles(IEnumerable<Follower> followers)
        {
            var aliveFollowerHandles = new List<int>();

            foreach (var follower in followers)
            {
                if (follower.PedHandle < 0)
                {
                    continue;
                }

                // Check if the ped is dead
                if (!_gameBridge.IsPedAlive(follower.PedHandle))
                {
                    // Handle death - remove blip, delete ped and notify service
                    _pedBlipService.RemoveBlipForPed(follower.PedHandle);
                    _gameBridge.DeletePed(follower.PedHandle);
                    _followerService.HandleFollowerDeath(follower.Id);

                    // Raise event
                    FollowerDied?.Invoke(this, follower);
                    continue;
                }

                aliveFollowerHandles.Add(follower.PedHandle);
            }

            return aliveFollowerHandles;
        }

        private void AssignFollowersToVehicle(List<int> aliveFollowerHandles, int playerVehicle)
        {
            var prioritizedSeats = _seatPriorityService.GetPrioritizedFreeSeats(playerVehicle);
            var nearbyFollowers = _seatPriorityService.FilterFollowersByProximity(
                aliveFollowerHandles.ToArray(), playerVehicle, 15f);

            var seatIndex = 0;
            foreach (var pedHandle in nearbyFollowers)
            {
                if (seatIndex >= prioritizedSeats.Length)
                    break;

                var inVehicle = _gameBridge.IsPedInVehicle(pedHandle);
                var tryingToEnter = _gameBridge.IsPedTryingToEnterVehicle(pedHandle);

                if (!inVehicle && !tryingToEnter)
                {
                    _gameBridge.TaskPedEnterVehicle(pedHandle, playerVehicle, prioritizedSeats[seatIndex]);
                    seatIndex++;
                }
                else if (inVehicle)
                {
                    seatIndex++;
                }
            }
        }

        private void ExitFollowersFromVehicles(IEnumerable<int> aliveFollowerHandles)
        {
            foreach (var pedHandle in aliveFollowerHandles)
            {
                if (_gameBridge.IsPedInVehicle(pedHandle))
                    _gameBridge.TaskPedLeaveVehicle(pedHandle);
            }
        }

        /// <summary>
        /// Gets the current number of followers for a faction.
        /// </summary>
        /// <param name="factionId">The faction to count followers for.</param>
        /// <returns>The number of active followers.</returns>
        public int GetFollowerCount(string factionId)
        {
            return _followerService.GetFollowerCount(factionId);
        }

        /// <summary>
        /// Gets the maximum number of followers allowed.
        /// </summary>
        /// <returns>The maximum follower limit.</returns>
        public int GetMaxFollowers()
        {
            return _followerService.GetMaxFollowers();
        }

        /// <summary>
        /// Gets all followers belonging to a faction.
        /// </summary>
        /// <param name="factionId">The faction to get followers for.</param>
        /// <returns>A read-only list of followers.</returns>
        public IReadOnlyList<Follower> GetFollowers(string factionId)
        {
            return _followerService.GetFollowers(factionId);
        }

        /// <summary>
        /// Gets the cost to recruit a follower of the specified tier.
        /// </summary>
        /// <param name="tier">The tier to get cost for.</param>
        /// <returns>The cost in dollars.</returns>
        public int GetRecruitCost(DefenderTier tier)
        {
            var config = _defenderTierService.GetTierConfig(tier);
            return config.Cost;
        }

        /// <summary>
        /// Checks if a new follower can be recruited for the specified faction.
        /// Does not check if player has sufficient funds.
        /// </summary>
        /// <param name="factionId">The faction to check.</param>
        /// <returns>True if recruitment is possible, false otherwise.</returns>
        public bool CanRecruit(string factionId)
        {
            // Check if below max followers
            if (_followerService.GetFollowerCount(factionId) >= _followerService.GetMaxFollowers())
            {
                return false;
            }

            // Check if ped pool can spawn
            return _pedSpawningService.CanSpawn();
        }

        /// <summary>
        /// Checks if a new follower of the specified tier can be recruited for the faction,
        /// including whether the player has sufficient funds.
        /// </summary>
        /// <param name="factionId">The faction to check.</param>
        /// <param name="tier">The tier of follower to recruit.</param>
        /// <returns>True if recruitment is possible and player has funds, false otherwise.</returns>
        public bool CanRecruitWithCost(string factionId, DefenderTier tier)
        {
            // First check basic recruitment constraints
            if (!CanRecruit(factionId))
            {
                return false;
            }

            // Check if player has enough money
            var tierConfig = _defenderTierService.GetTierConfig(tier);
            var playerMoney = _gameBridge.GetPlayerMoney();
            return playerMoney >= tierConfig.Cost;
        }

        /// <summary>
        /// Configures a follower's combat attributes based on their tier.
        /// Sets weapon, accuracy, armor, health, and combat behavior.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to configure.</param>
        /// <param name="tierConfig">The tier configuration to apply.</param>
        private void ConfigureFollowerCombat(int pedHandle, DefenderTierConfig tierConfig)
        {
            // Give pistol first as secondary weapon for drive-by shooting from vehicles
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");

            // Give tier-appropriate weapon last so it becomes the equipped/primary weapon
            _gameBridge.GivePedWeapon(pedHandle, tierConfig.Weapon);

            // Set shooting accuracy
            _gameBridge.SetPedAccuracy(pedHandle, tierConfig.Accuracy);

            // Set armor based on tier
            _gameBridge.SetPedArmor(pedHandle, tierConfig.Armor);

            // Set health based on tier
            _gameBridge.SetPedHealth(pedHandle, tierConfig.Health);

            // Configure combat behavior - followers should take cover and fight armed enemies
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Elite tier uses RPG - prevent AI from switching to pistol (AI prefers pistol to avoid self-damage)
            if (tierConfig.Tier == DefenderTier.Elite)
            {
                _gameBridge.SetPedCanSwitchWeapons(pedHandle, false);
            }
        }

        /// <summary>
        /// Calculates a spawn position for a follower near the player.
        /// </summary>
        /// <param name="playerPos">The player's current position.</param>
        /// <returns>A position slightly offset from the player.</returns>
        private Vector3 CalculateFollowerSpawnPosition(Vector3 playerPos)
        {
            // Spawn slightly behind and to the side of the player
            return new Vector3(
                playerPos.X + 2f,
                playerPos.Y + 2f,
                playerPos.Z);
        }
    }
}
