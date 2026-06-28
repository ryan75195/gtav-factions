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
    public partial class FollowerManager : IFollowerManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IFollowerService _followerService;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IDefenderRoleService _defenderRoleService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IVehicleSeatPriorityService _seatPriorityService;
        private readonly Dictionary<DefenderRole, string> _modelsByTier;

        /// <summary>
        /// Raised when a follower dies in combat.
        /// </summary>
        public event EventHandler<Follower>? FollowerDied;

        /// <summary>
        /// Alive on-foot bodyguard handles from the most recent <see cref="Update"/>. Empty when
        /// the player is in a vehicle. The squad stance controller owns their on-foot tasking.
        /// </summary>
        public IReadOnlyList<int> OnFootBodyguardHandles { get; private set; } = Array.Empty<int>();

        /// <summary>
        /// Alive on-foot bodyguard handles whose role is <see cref="DefenderRole.Sniper"/>, from the
        /// most recent <see cref="Update"/>. Empty when the player is in a vehicle. Used to drive the
        /// close-range sidearm switch so snipers fire a pistol in CQC instead of a scoped rifle.
        /// </summary>
        public IReadOnlyList<int> SniperBodyguardHandles { get; private set; } = Array.Empty<int>();

        /// <summary>
        /// Creates a new FollowerManager.
        /// </summary>
        /// <param name="gameBridge">The game bridge for game interactions.</param>
        /// <param name="followerService">The domain-layer follower service.</param>
        /// <param name="pedSpawningService">Service for spawning peds.</param>
        /// <param name="defenderRoleService">Service for defender tier configurations.</param>
        /// <param name="pedBlipService">Service for managing ped blips on the minimap.</param>
        /// <param name="seatPriorityService">Service for coordinated vehicle seat assignment.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public FollowerManager(FollowerManagerDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _followerService = dependencies.FollowerService ?? throw new ArgumentNullException(nameof(dependencies.FollowerService));
            _pedSpawningService = dependencies.PedSpawningService ?? throw new ArgumentNullException(nameof(dependencies.PedSpawningService));
            _defenderRoleService = dependencies.DefenderRoleService ?? throw new ArgumentNullException(nameof(dependencies.DefenderRoleService));
            _pedBlipService = dependencies.PedBlipService ?? throw new ArgumentNullException(nameof(dependencies.PedBlipService));
            _seatPriorityService = dependencies.SeatPriorityService ?? throw new ArgumentNullException(nameof(dependencies.SeatPriorityService));

            _modelsByTier = new Dictionary<DefenderRole, string>
            {
                { DefenderRole.Grunt, "g_m_y_lost_01" },
                { DefenderRole.Gunner, "g_m_y_lost_02" },
                { DefenderRole.Rifleman, "g_m_y_lost_03" }
            };
        }

        public FollowerManager(params object?[] dependencies)
            : this(new FollowerManagerDependencies
            {
                GameBridge = (IGameBridge?)dependencies[0],
                FollowerService = (IFollowerService?)dependencies[1],
                PedSpawningService = (IPedSpawningService?)dependencies[2],
                DefenderRoleService = (IDefenderRoleService?)dependencies[3],
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
        public void SetModelForTier(DefenderRole tier, string modelName)
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
        public FollowerRecruitResult RecruitFollower(string factionId, DefenderRole tier)
        {
            if (string.IsNullOrEmpty(factionId))
                throw new ArgumentNullException(nameof(factionId));

            // Get cost for this tier
            var roleConfig = _defenderRoleService.GetRoleConfig(tier);
            var cost = roleConfig.Cost;

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
            ConfigureFollowerCombat(pedHandle.Handle, roleConfig);

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
        /// <param name="boardPlayerVehicle">
        /// When true (Escort), followers board the player's vehicle to follow. When false
        /// (HoldArea / SearchAndDestroy), they stay on the ground so the player can drop them
        /// off and drive away while they lock down the area.
        /// </param>
        public void Update(string factionId, bool boardPlayerVehicle = true)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                OnFootBodyguardHandles = Array.Empty<int>();
                SniperBodyguardHandles = Array.Empty<int>();
                return;
            }

            var followers = _followerService.GetFollowers(factionId);
            var playerInVehicle = _gameBridge.IsPlayerInVehicle();
            var playerVehicle = playerInVehicle ? _gameBridge.GetPlayerVehicle() : -1;
            var aliveFollowerHandles = GetAliveFollowerHandles(followers);

            if (playerInVehicle && playerVehicle >= 0 && boardPlayerVehicle)
            {
                AssignFollowersToVehicle(aliveFollowerHandles, playerVehicle);
                OnFootBodyguardHandles = Array.Empty<int>();
                SniperBodyguardHandles = Array.Empty<int>();
            }
            else
            {
                // Player on foot, or holding/hunting: keep bodyguards on the ground for the
                // squad stance controller to task (it disembarks any still in a vehicle).
                OnFootBodyguardHandles = aliveFollowerHandles;
                SniperBodyguardHandles = FilterSniperHandles(followers, aliveFollowerHandles);
            }
        }

    }
}
