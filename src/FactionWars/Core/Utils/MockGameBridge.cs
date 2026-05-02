using System;
using System.Collections.Generic;
using System.IO;
using FactionWars.Core.Interfaces;

namespace FactionWars.Core.Utils
{
    /// <summary>
    /// Mock implementation of IGameBridge for unit testing.
    /// Provides full control over game state and behavior verification.
    /// </summary>
    public class MockGameBridge : IGameBridge
    {
        private readonly Dictionary<int, PedState> _peds = new Dictionary<int, PedState>();
        private readonly Dictionary<int, BlipState> _blips = new Dictionary<int, BlipState>();
        private readonly List<string> _notifications = new List<string>();
        private readonly List<int> _blipsCreated = new List<int>();
        private readonly List<int> _blipsDeleted = new List<int>();
        private readonly Dictionary<int, BlipColor> _blipColors = new Dictionary<int, BlipColor>();
        private readonly Dictionary<int, int> _blipSprites = new Dictionary<int, int>();

        private int _nextPedHandle = 1;
        private int _nextBlipHandle = 1;
        private int _nextPedBlipHandle = 5000;

        private bool _isPlayerFreeAiming;
        private int _entityPlayerIsAimingAt;

        /// <summary>
        /// Gets or sets the player position to return from GetPlayerPosition.
        /// </summary>
        public Vector3 PlayerPosition { get; set; } = Vector3.Zero;

        /// <summary>
        /// Gets or sets the game time to return from GetGameTime.
        /// </summary>
        public int GameTime { get; set; } = 0;

        /// <summary>
        /// Gets or sets the player character model to return from GetPlayerCharacterModel.
        /// </summary>
        public string PlayerCharacterModel { get; set; } = "player_zero";

        /// <summary>
        /// Gets or sets the player heading (direction facing) in degrees to return from GetPlayerHeading.
        /// 0 = North, 90 = East, 180 = South, 270 = West.
        /// </summary>
        public float PlayerHeading { get; set; } = 0f;

        /// <summary>
        /// Gets or sets whether the player is dead.
        /// </summary>
        public bool IsPlayerDeadValue { get; set; } = false;

        /// <summary>
        /// Gets or sets the player's money amount.
        /// </summary>
        public int PlayerMoney { get; set; } = 0;

        /// <summary>
        /// Gets or sets the player's wanted level returned from GetWantedLevel.
        /// </summary>
        public int WantedLevel { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether the player has been damaged by a ped.
        /// Cleared by ConsumePlayerDamagedByPedFlag().
        /// </summary>
        public bool PlayerDamagedByPed { get; set; } = false;

        /// <summary>
        /// Gets or sets the player ped handle returned from GetPlayerPedHandle.
        /// Defaults to 1 so test code doesn't have to set it up to use rally tasks.
        /// </summary>
        public int PlayerPedHandle { get; set; } = 1;

        /// <summary>
        /// Gets or sets the total play time in seconds.
        /// </summary>
        public long TotalPlayTimeSeconds { get; set; } = 0;

        /// <summary>When true, GetTotalPlayTimeSeconds() returns null to simulate a failed stat read.</summary>
        public bool SimulateTotalPlayTimeReadFailure { get; set; } = false;

        public int ActiveCharacterIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of completed missions.
        /// </summary>
        public int CompletedMissionCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the in-game clock time in minutes.
        /// </summary>
        public int InGameClockMinutes { get; set; } = 0;

        /// <summary>
        /// Gets the last help text displayed via DisplayHelpText.
        /// </summary>
        public string? LastHelpText { get; private set; }

        /// <summary>
        /// Gets the list of notifications shown.
        /// </summary>
        public IReadOnlyList<string> Notifications => _notifications;

        /// <summary>
        /// Gets the count of notifications shown.
        /// </summary>
        public int NotificationCount => _notifications.Count;

        /// <summary>
        /// Gets the list of blip handles that were created.
        /// </summary>
        public IReadOnlyList<int> BlipsCreated => _blipsCreated;

        /// <summary>
        /// Gets the list of blip handles that were deleted.
        /// </summary>
        public IReadOnlyList<int> BlipsDeleted => _blipsDeleted;

        /// <summary>
        /// Gets the dictionary of blip handle to color mappings.
        /// </summary>
        public IReadOnlyDictionary<int, BlipColor> BlipColors => _blipColors;

        public Vector3 GetPlayerPosition() => PlayerPosition;

        public int CreatePed(string modelName, Vector3 position)
        {
            var handle = _nextPedHandle++;
            _peds[handle] = new PedState
            {
                ModelName = modelName,
                Position = position,
                IsAlive = true,
                RelationshipGroup = string.Empty
            };
            return handle;
        }

        public void DeletePed(int pedHandle)
        {
            _peds.Remove(pedHandle);
        }

        public bool IsPedAlive(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) && ped.IsAlive;
        }

        public bool DoesPedExist(int pedHandle)
        {
            // Mirrors DOES_ENTITY_EXIST: true for both alive and dead peds; false only
            // when the ped record is gone (streamed out / explicitly deleted).
            return _peds.ContainsKey(pedHandle);
        }

        public void SetPedRelationshipGroup(int pedHandle, string groupName)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.RelationshipGroup = groupName;
            }
        }

        public int CreateBlip(Vector3 position)
        {
            var handle = _nextBlipHandle++;
            _blips[handle] = new BlipState
            {
                Position = position,
                Color = BlipColor.White
            };
            _blipsCreated.Add(handle);
            return handle;
        }

        public void DeleteBlip(int blipHandle)
        {
            _blips.Remove(blipHandle);
            _blipsDeleted.Add(blipHandle);
        }

        public int CreateRadiusBlip(Vector3 center, float radius)
        {
            var handle = CreateBlip(center);
            return handle;
        }

        public void SetBlipAlpha(int blipHandle, int alpha)
        {
            // Mock: no-op
        }

        public void SetBlipColor(int blipHandle, BlipColor color)
        {
            if (_blips.TryGetValue(blipHandle, out var blip))
            {
                blip.Color = color;
            }
            _blipColors[blipHandle] = color;
        }

        public void SetBlipSprite(int blipHandle, int spriteId)
        {
            if (_blips.TryGetValue(blipHandle, out var blip))
            {
                blip.Sprite = spriteId;
            }
            _blipSprites[blipHandle] = spriteId;
        }

        public void SetBlipName(int blipHandle, string name)
        {
            if (_blips.TryGetValue(blipHandle, out var blip))
            {
                blip.Name = name;
            }
        }

        /// <summary>
        /// Gets the name that was set for a blip (for testing).
        /// </summary>
        public string GetBlipName(int blipHandle)
        {
            return _blips.TryGetValue(blipHandle, out var blip) ? blip.Name : string.Empty;
        }

        /// <summary>
        /// Gets the sprite ID that was set for a blip (for testing).
        /// </summary>
        public int GetBlipSprite(int blipHandle)
        {
            return _blipSprites.TryGetValue(blipHandle, out var sprite) ? sprite : 0;
        }

        public void ShowNotification(string message)
        {
            _notifications.Add(message);
        }

        public int GetGameTime() => GameTime;

        public bool RevivePed(int pedHandle)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.IsAlive = true;
                return true;
            }
            return false;
        }

        public void SetPedPosition(int pedHandle, Vector3 position)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Position = position;
            }
        }

        public bool SetPedModel(int pedHandle, string modelName)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.ModelName = modelName;
                return true;
            }
            return false;
        }

        public string GetPlayerCharacterModel() => PlayerCharacterModel;

        public float GetPlayerHeading() => PlayerHeading;

        public bool IsPlayerDead() => IsPlayerDeadValue;

        public int GetWantedLevel() => WantedLevel;

        public bool ConsumePlayerDamagedByPedFlag()
        {
            var was = PlayerDamagedByPed;
            PlayerDamagedByPed = false;
            return was;
        }

        public int GetPlayerPedHandle() => PlayerPedHandle;

        public int GetPlayerMoney() => PlayerMoney;

        public long? GetTotalPlayTimeSeconds() => SimulateTotalPlayTimeReadFailure ? (long?)null : TotalPlayTimeSeconds;

        public int GetActiveCharacterIndex() => ActiveCharacterIndex;

        public int GetCompletedMissionCount() => CompletedMissionCount;

        public int GetInGameClockMinutes() => InGameClockMinutes;

        public void AddPlayerMoney(int amount)
        {
            PlayerMoney += amount;
        }

        public void SetPlayerMoney(int amount)
        {
            PlayerMoney = amount;
        }

        private readonly List<string> _playerWeapons = new List<string>();

        /// <summary>
        /// Gets the list of weapons the player has been given (for testing).
        /// </summary>
        public IReadOnlyList<string> PlayerWeapons => _playerWeapons;

        /// <summary>
        /// Gets whether all player weapons have been removed since last reset.
        /// </summary>
        public bool WereAllPlayerWeaponsRemoved { get; private set; }

        public void RemoveAllPlayerWeapons()
        {
            _playerWeapons.Clear();
            _playerWeaponsWithAmmo.Clear();
            WereAllPlayerWeaponsRemoved = true;
        }

        public void GivePlayerWeapon(string weaponName, int ammo)
        {
            // Store weapon with ammo for GetPlayerWeapons
            _playerWeaponsWithAmmo[weaponName] = ammo;
            _playerWeapons.Add(weaponName);
        }

        private readonly Dictionary<string, int> _playerWeaponsWithAmmo = new Dictionary<string, int>();

        public Dictionary<string, int> GetPlayerWeapons()
        {
            return new Dictionary<string, int>(_playerWeaponsWithAmmo);
        }

        public void ConfigurePlayerSettings()
        {
            // Mock: No actual game settings to configure
        }

        public bool IsPlayerFreeAiming() => _isPlayerFreeAiming;

        public void SetPlayerFreeAiming(bool aiming) => _isPlayerFreeAiming = aiming;

        public int GetEntityPlayerIsAimingAt() => _entityPlayerIsAimingAt;

        public void SetEntityPlayerIsAimingAt(int entityHandle) => _entityPlayerIsAimingAt = entityHandle;

        public void DisplayHelpText(string text) => LastHelpText = text;

        private readonly List<int> _followingPeds = new List<int>();
        private readonly Dictionary<int, VehicleState> _vehicles = new Dictionary<int, VehicleState>();
        private readonly Dictionary<int, int> _pedsInVehicles = new Dictionary<int, int>(); // pedHandle -> vehicleHandle
        private readonly HashSet<int> _pedsTryingToEnterVehicle = new HashSet<int>();
        private readonly Dictionary<int, int> _vehicleBlips = new Dictionary<int, int>(); // vehicleHandle -> blipHandle
        private int _nextVehicleHandle = 1000;
        private int _nextVehicleBlipHandle = 9000;

        /// <summary>
        /// Gets or sets whether the player is in a vehicle.
        /// </summary>
        public bool IsPlayerInVehicleValue { get; set; } = false;

        /// <summary>
        /// Gets or sets the vehicle handle the player is currently in.
        /// </summary>
        public int PlayerVehicleHandle { get; set; } = -1;

        /// <summary>
        /// Gets the list of ped handles that have been set to follow the player.
        /// </summary>
        public IReadOnlyList<int> FollowingPeds => _followingPeds;

        public void SetPedAsFollower(int pedHandle)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                _followingPeds.Add(pedHandle);
            }
        }

        public bool IsPlayerInVehicle() => IsPlayerInVehicleValue;

        public int GetPlayerVehicle() => PlayerVehicleHandle;

        public bool IsPedInVehicle(int pedHandle)
        {
            return _pedsInVehicles.ContainsKey(pedHandle);
        }

        public bool IsPedTryingToEnterVehicle(int pedHandle)
        {
            return _pedsTryingToEnterVehicle.Contains(pedHandle);
        }

        public int[] GetVehicleFreeSeats(int vehicleHandle)
        {
            if (!_vehicles.TryGetValue(vehicleHandle, out var vehicle))
            {
                return Array.Empty<int>();
            }

            return vehicle.GetFreeSeats();
        }

        public void TaskPedEnterVehicle(int pedHandle, int vehicleHandle, int seatIndex)
        {
            if (!_peds.ContainsKey(pedHandle) || !_vehicles.ContainsKey(vehicleHandle))
            {
                return;
            }

            // Mark ped as in vehicle and occupy seat
            _pedsInVehicles[pedHandle] = vehicleHandle;
            _vehicles[vehicleHandle].OccupySeat(seatIndex, pedHandle);
        }

        public void TaskPedLeaveVehicle(int pedHandle)
        {
            if (_pedsInVehicles.TryGetValue(pedHandle, out var vehicleHandle))
            {
                _pedsInVehicles.Remove(pedHandle);
                if (_vehicles.TryGetValue(vehicleHandle, out var vehicle))
                {
                    vehicle.FreeSeatByPed(pedHandle);
                }
            }
        }

        public void GivePedWeapon(int pedHandle, string weaponName)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Weapon = weaponName;
            }
        }

        public void SetPedCanSwitchWeapons(int pedHandle, bool canSwitch)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.CanSwitchWeapons = canSwitch;
            }
        }

        /// <summary>
        /// Gets whether a ped can switch weapons (for testing purposes).
        /// </summary>
        public bool GetPedCanSwitchWeapons(int pedHandle)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                return ped.CanSwitchWeapons;
            }
            return true; // Default
        }

        public void SetPedAccuracy(int pedHandle, float accuracy)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Accuracy = accuracy;
            }
        }

        public void SetPedArmor(int pedHandle, int armor)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Armor = armor;
            }
        }

        public void SetPedHealth(int pedHandle, int health)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Health = health;
            }
        }

        public void SetPedCombatAttributes(int pedHandle, bool canUseCover, bool willFightArmedPeds)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.CanUseCover = canUseCover;
                ped.WillFightArmedPeds = willFightArmedPeds;
            }
        }

        /// <summary>
        /// Gets or sets the current waypoint position, or null if no waypoint is set.
        /// </summary>
        public Vector3? WaypointPosition { get; private set; }

        /// <summary>
        /// Gets whether a waypoint is currently set.
        /// </summary>
        public bool HasWaypointSet => WaypointPosition.HasValue;

        public void SetWaypoint(Vector3 position)
        {
            WaypointPosition = position;
        }

        public void ClearWaypoint()
        {
            WaypointPosition = null;
        }

        public void SetPedToAttackPlayer(int pedHandle)
        {
            // Mock implementation - track that ped was set to attack
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.IsAttackingPlayer = true;
            }
        }

        public int CreateBlipForPed(int pedHandle)
        {
            return _nextPedBlipHandle++;
        }

        private readonly Dictionary<int, WanderState> _wanderingPeds = new Dictionary<int, WanderState>();

        public void TaskPedWanderInArea(int pedHandle, Vector3 center, float radius)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                // Clear other task states when assigning wander
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
                _wanderingPeds[pedHandle] = new WanderState
                {
                    Center = center,
                    Radius = radius,
                    IsSprinting = false
                };
            }
        }

        public void TaskPedWanderInAreaSprinting(int pedHandle, Vector3 center, float radius)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                // Clear other task states when assigning wander
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
                _wanderingPeds[pedHandle] = new WanderState
                {
                    Center = center,
                    Radius = radius,
                    IsSprinting = true
                };
            }
        }

        /// <summary>
        /// Gets whether a ped is currently wandering.
        /// </summary>
        public bool IsPedWandering(int pedHandle) => _wanderingPeds.ContainsKey(pedHandle);

        /// <summary>
        /// Gets whether a ped is wandering and sprinting.
        /// </summary>
        public bool IsPedWanderingSprinting(int pedHandle)
        {
            return _wanderingPeds.TryGetValue(pedHandle, out var state) && state.IsSprinting;
        }

        /// <summary>
        /// Gets the wander center position for a ped.
        /// </summary>
        public Vector3? GetPedWanderCenter(int pedHandle)
        {
            return _wanderingPeds.TryGetValue(pedHandle, out var state) ? state.Center : (Vector3?)null;
        }

        /// <summary>
        /// Gets the wander radius for a ped.
        /// </summary>
        public float? GetPedWanderRadius(int pedHandle)
        {
            return _wanderingPeds.TryGetValue(pedHandle, out var state) ? state.Radius : (float?)null;
        }

        private class WanderState
        {
            public Vector3 Center { get; set; }
            public float Radius { get; set; }
            public bool IsSprinting { get; set; }
        }

        private readonly Dictionary<int, float> _combatTargetingPeds = new Dictionary<int, float>();

        public void TaskCombatHatedTargetsAroundPed(int pedHandle, float radius)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                // In GTA V, every TASK_X call is a primary-task replacement: the new task
                // wipes the previous one. Mirror that here so tests catch double-tasking
                // bugs (e.g. issuing TaskGoToEntity then TaskCombatHatedTargetsAroundPed
                // back-to-back leaves only the second active in-game).
                _wanderingPeds.Remove(pedHandle);
                _pedsFacingPosition.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
                _combatTargetingPeds[pedHandle] = radius;
            }
        }

        /// <summary>
        /// Gets whether a ped is actively seeking combat targets.
        /// </summary>
        public bool IsPedCombatTargeting(int pedHandle) => _combatTargetingPeds.ContainsKey(pedHandle);

        /// <summary>
        /// Gets the combat targeting radius for a ped.
        /// </summary>
        public float? GetPedCombatTargetingRadius(int pedHandle)
        {
            return _combatTargetingPeds.TryGetValue(pedHandle, out var radius) ? radius : (float?)null;
        }

        private readonly Dictionary<int, GoToEntityState> _goToEntityPeds = new Dictionary<int, GoToEntityState>();

        private class GoToEntityState
        {
            public int TargetEntityHandle { get; set; }
            public float StoppingRange { get; set; }
        }

        public void TaskGoToEntity(int pedHandle, int targetEntityHandle, float stoppingRange)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                // In GTA V, every TASK_X call is a primary-task replacement.
                _wanderingPeds.Remove(pedHandle);
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
                _goToEntityPeds[pedHandle] = new GoToEntityState
                {
                    TargetEntityHandle = targetEntityHandle,
                    StoppingRange = stoppingRange
                };
            }
        }

        /// <summary>Gets whether a ped is currently going to an entity.</summary>
        public bool IsPedGoingToEntity(int pedHandle) => _goToEntityPeds.ContainsKey(pedHandle);

        /// <summary>Gets the target entity handle for a go-to-entity task.</summary>
        public int? GetGoToEntityTarget(int pedHandle)
            => _goToEntityPeds.TryGetValue(pedHandle, out var state) ? state.TargetEntityHandle : (int?)null;

        /// <summary>Gets the stopping range for a go-to-entity task.</summary>
        public float? GetGoToEntityStoppingRange(int pedHandle)
            => _goToEntityPeds.TryGetValue(pedHandle, out var state) ? state.StoppingRange : (float?)null;

        private readonly Dictionary<int, FollowEntityState> _followEntityPeds = new Dictionary<int, FollowEntityState>();

        private class FollowEntityState
        {
            public int TargetEntityHandle { get; set; }
            public Vector3 Offset { get; set; }
            public float MoveBlendRatio { get; set; }
            public float StoppingRadius { get; set; }
            public bool PersistFollowing { get; set; }
        }

        public void TaskFollowToOffsetFromEntity(int pedHandle, int targetEntityHandle, Vector3 offset, float moveBlendRatio, float stoppingRadius, bool persistFollowing)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                // In GTA V, every TASK_X call is a primary-task replacement.
                _wanderingPeds.Remove(pedHandle);
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds[pedHandle] = new FollowEntityState
                {
                    TargetEntityHandle = targetEntityHandle,
                    Offset = offset,
                    MoveBlendRatio = moveBlendRatio,
                    StoppingRadius = stoppingRadius,
                    PersistFollowing = persistFollowing,
                };
            }
        }

        /// <summary>Gets whether a ped is currently following an entity.</summary>
        public bool IsPedFollowingEntity(int pedHandle) => _followEntityPeds.ContainsKey(pedHandle);

        /// <summary>Gets the target entity handle for a follow task.</summary>
        public int? GetFollowEntityTarget(int pedHandle)
            => _followEntityPeds.TryGetValue(pedHandle, out var state) ? state.TargetEntityHandle : (int?)null;

        /// <summary>Gets the stopping radius for a follow task.</summary>
        public float? GetFollowEntityStoppingRadius(int pedHandle)
            => _followEntityPeds.TryGetValue(pedHandle, out var state) ? state.StoppingRadius : (float?)null;

        public void SetPedAsFriendly(int pedHandle)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                // In mock, set to FRIENDLY_DEFENDERS group (separate from PLAYER group)
                // This allows independent wandering behavior while still being friendly to player
                ped.RelationshipGroup = "FRIENDLY_DEFENDERS";
            }
        }

        public float GetGroundZ(float x, float y, float z)
        {
            // Mock just returns input Z
            return z;
        }

        public Vector3 GetSafeCoordForPed(Vector3 position)
        {
            // Mock just returns the position with original Z (simulating ground level)
            return position;
        }

        public void SetPedAsHostileWanderer(int pedHandle)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.RelationshipGroup = "DEFENDER_ENEMIES";
                ped.IsAttackingPlayer = true;
            }
        }

        public string GetScriptsDirectory()
        {
            return Path.GetTempPath();
        }

        // Additional helper methods for testing

        /// <summary>
        /// Checks if a ped exists (regardless of alive state).
        /// </summary>
        public bool PedExists(int pedHandle) => _peds.ContainsKey(pedHandle);

        /// <summary>
        /// Kills a ped (sets IsAlive to false but keeps the ped).
        /// </summary>
        public void KillPed(int pedHandle)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.IsAlive = false;
            }
        }

        /// <summary>
        /// Sets a ped as dead. Alias for KillPed.
        /// </summary>
        public void SetPedDead(int pedHandle) => KillPed(pedHandle);

        /// <summary>
        /// Gets all currently spawned ped handles.
        /// </summary>
        public List<int> GetSpawnedPeds() => new List<int>(_peds.Keys);

        /// <summary>
        /// Gets the relationship group of a ped.
        /// </summary>
        public string GetPedRelationshipGroup(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.RelationshipGroup : string.Empty;
        }

        /// <summary>
        /// Gets the position of a ped.
        /// </summary>
        public Vector3 GetPedPosition(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Position : Vector3.Zero;
        }

        private readonly HashSet<int> _clearedPeds = new HashSet<int>();
        private readonly Dictionary<int, Vector3> _pedsFacingPosition = new Dictionary<int, Vector3>();

        public void ClearPedTasks(int pedHandle)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                _clearedPeds.Add(pedHandle);
                _wanderingPeds.Remove(pedHandle);
                _pedsFacingPosition.Remove(pedHandle);
                _combatTargetingPeds.Remove(pedHandle);
                _goToEntityPeds.Remove(pedHandle);
                _followEntityPeds.Remove(pedHandle);
            }
        }

        public void TaskPedTurnToFacePosition(int pedHandle, Vector3 position)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                _pedsFacingPosition[pedHandle] = position;
            }
        }

        private readonly Dictionary<int, float> _pedSeeingRanges = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _pedHearingRanges = new Dictionary<int, float>();

        public void SetPedSeeingRange(int pedHandle, float range)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                _pedSeeingRanges[pedHandle] = range;
            }
        }

        public void SetPedHearingRange(int pedHandle, float range)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                _pedHearingRanges[pedHandle] = range;
            }
        }

        /// <summary>
        /// Gets the seeing range set for a ped.
        /// </summary>
        public float GetPedSeeingRange(int pedHandle)
        {
            return _pedSeeingRanges.TryGetValue(pedHandle, out var range) ? range : 70f; // Default ~70m
        }

        /// <summary>
        /// Gets the hearing range set for a ped.
        /// </summary>
        public float GetPedHearingRange(int pedHandle)
        {
            return _pedHearingRanges.TryGetValue(pedHandle, out var range) ? range : 60f; // Default ~60m
        }

        /// <summary>
        /// Checks if a ped has had its tasks cleared.
        /// </summary>
        public bool WerePedTasksCleared(int pedHandle) => _clearedPeds.Contains(pedHandle);

        /// <summary>
        /// Checks if a ped is facing a position.
        /// </summary>
        public bool IsPedFacingPosition(int pedHandle) => _pedsFacingPosition.ContainsKey(pedHandle);

        /// <summary>
        /// Gets the position a ped is facing.
        /// </summary>
        public Vector3? GetPedFacingPosition(int pedHandle)
        {
            return _pedsFacingPosition.TryGetValue(pedHandle, out var pos) ? pos : (Vector3?)null;
        }

        /// <summary>
        /// Gets the model name of a ped.
        /// </summary>
        public string GetPedModel(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.ModelName : string.Empty;
        }

        /// <summary>
        /// Gets the weapon of a ped.
        /// </summary>
        public string GetPedWeapon(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Weapon : string.Empty;
        }

        /// <summary>
        /// Gets the accuracy of a ped.
        /// </summary>
        public float GetPedAccuracy(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Accuracy : 0.5f;
        }

        /// <summary>
        /// Gets the armor of a ped.
        /// </summary>
        public int GetPedArmor(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Armor : 0;
        }

        /// <summary>
        /// Gets the health of a ped.
        /// </summary>
        public int GetPedHealth(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Health : 100;
        }

        /// <summary>
        /// Gets whether a ped can use cover.
        /// </summary>
        public bool GetPedCanUseCover(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) && ped.CanUseCover;
        }

        /// <summary>
        /// Gets whether a ped will fight armed peds.
        /// </summary>
        public bool GetPedWillFightArmedPeds(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) && ped.WillFightArmedPeds;
        }

        /// <summary>
        /// Checks if a blip exists.
        /// </summary>
        public bool BlipExists(int blipHandle) => _blips.ContainsKey(blipHandle);

        /// <summary>
        /// Gets the color of a blip.
        /// </summary>
        public BlipColor GetBlipColor(int blipHandle)
        {
            return _blips.TryGetValue(blipHandle, out var blip) ? blip.Color : BlipColor.White;
        }

        /// <summary>
        /// Advances the game time by the specified amount.
        /// </summary>
        public void AdvanceGameTime(int milliseconds)
        {
            GameTime += milliseconds;
        }

        /// <summary>
        /// Resets all mock state to initial values.
        /// </summary>
        public void Reset()
        {
            _peds.Clear();
            _blips.Clear();
            _notifications.Clear();
            _blipsCreated.Clear();
            _blipsDeleted.Clear();
            _blipColors.Clear();
            _followingPeds.Clear();
            _vehicles.Clear();
            _pedsInVehicles.Clear();
            _wanderingPeds.Clear();
            _nextPedHandle = 1;
            _nextBlipHandle = 1;
            _nextVehicleHandle = 1000;
            PlayerPosition = Vector3.Zero;
            GameTime = 0;
            PlayerCharacterModel = "player_zero";
            PlayerHeading = 0f;
            IsPlayerDeadValue = false;
            PlayerMoney = 0;
            IsPlayerInVehicleValue = false;
            PlayerVehicleHandle = -1;
            WaypointPosition = null;
            _isPlayerFreeAiming = false;
            _entityPlayerIsAimingAt = 0;
            LastHelpText = null;
            _playerWeapons.Clear();
            _playerWeaponsWithAmmo.Clear();
            WereAllPlayerWeaponsRemoved = false;
        }

        /// <summary>
        /// Creates a vehicle and returns its handle. For testing purposes.
        /// </summary>
        /// <param name="totalSeats">Total number of seats including driver.</param>
        /// <returns>The vehicle handle.</returns>
        public int CreateTestVehicle(int totalSeats = 4)
        {
            var handle = _nextVehicleHandle++;
            _vehicles[handle] = new VehicleState(totalSeats);
            return handle;
        }

        /// <summary>
        /// Creates a vehicle at the specified position (IGameBridge implementation).
        /// </summary>
        public int CreateVehicle(string modelName, Vector3 position)
        {
            var handle = _nextVehicleHandle++;
            _vehicles[handle] = new VehicleState(4, modelName, position);
            return handle;
        }

        /// <summary>
        /// Creates a blip attached to a vehicle.
        /// </summary>
        public int CreateBlipForVehicle(int vehicleHandle)
        {
            if (!_vehicles.ContainsKey(vehicleHandle))
            {
                return -1;
            }
            var blipHandle = _nextVehicleBlipHandle++;
            _vehicleBlips[vehicleHandle] = blipHandle;
            _blips[blipHandle] = new BlipState
            {
                Position = _vehicles[vehicleHandle].Position,
                Color = BlipColor.Yellow
            };
            _blipsCreated.Add(blipHandle);
            return blipHandle;
        }

        /// <summary>
        /// Gets the count of spawned vehicles (for testing).
        /// </summary>
        public int GetSpawnedVehicleCount() => _vehicles.Count;

        /// <summary>
        /// Gets the count of vehicle blips (for testing).
        /// </summary>
        public int GetVehicleBlipCount() => _vehicleBlips.Count;

        /// <summary>
        /// Gets the nearest road position (mock returns position with Z=0 to simulate road level).
        /// </summary>
        public Vector3 GetNearestRoadPosition(Vector3 position)
        {
            // Mock simulates finding a nearby road at ground level
            return new Vector3(position.X + 5, position.Y, 0f);
        }

        /// <summary>
        /// Gets the model name of a vehicle.
        /// </summary>
        public string GetVehicleModelName(int vehicleHandle)
        {
            if (_vehicles.TryGetValue(vehicleHandle, out var vehicle))
            {
                return vehicle.ModelName;
            }
            return string.Empty;
        }

        /// <inheritdoc />
        public int GetVehicleClass(int vehicleHandle)
        {
            if (_vehicles.TryGetValue(vehicleHandle, out var state))
                return state.VehicleClass;
            return -1;
        }

        /// <inheritdoc />
        public bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex)
        {
            if (_vehicles.TryGetValue(vehicleHandle, out var state))
                return state.TurretSeats.Contains(seatIndex);
            return false;
        }

        /// <inheritdoc />
        public Vector3 GetVehiclePosition(int vehicleHandle)
        {
            if (_vehicles.TryGetValue(vehicleHandle, out var state))
                return state.Position;
            return Vector3.Zero;
        }

        /// <summary>
        /// Sets the vehicle class for testing.
        /// </summary>
        public void SetVehicleClass(int vehicleHandle, int vehicleClass)
        {
            if (_vehicles.TryGetValue(vehicleHandle, out var state))
                state.VehicleClass = vehicleClass;
        }

        /// <summary>
        /// Marks a seat as a turret seat for testing.
        /// </summary>
        public void SetSeatAsTurret(int vehicleHandle, int seatIndex)
        {
            if (_vehicles.TryGetValue(vehicleHandle, out var state))
                state.TurretSeats.Add(seatIndex);
        }

        /// <summary>
        /// Sets the vehicle position for testing.
        /// </summary>
        public void SetVehiclePosition(int vehicleHandle, Vector3 position)
        {
            if (_vehicles.TryGetValue(vehicleHandle, out var state))
                state.Position = position;
        }

        /// <summary>
        /// Sets up the player vehicle with a specific model name (for testing).
        /// </summary>
        public int SetPlayerInVehicleWithModel(string modelName, int totalSeats = 4)
        {
            var handle = _nextVehicleHandle++;
            _vehicles[handle] = new VehicleState(totalSeats, modelName, PlayerPosition);
            IsPlayerInVehicleValue = true;
            PlayerVehicleHandle = handle;
            _vehicles[handle].OccupySeat(0, -1); // Driver seat
            return handle;
        }

        /// <summary>
        /// Sets up a vehicle for the player with the specified number of seats.
        /// </summary>
        /// <param name="totalSeats">Total number of seats including driver.</param>
        /// <returns>The vehicle handle.</returns>
        public int SetPlayerInVehicle(int totalSeats = 4)
        {
            var handle = _nextVehicleHandle++;
            _vehicles[handle] = new VehicleState(totalSeats, "", PlayerPosition);
            IsPlayerInVehicleValue = true;
            PlayerVehicleHandle = handle;
            // Occupy driver seat (index 0) by player
            _vehicles[handle].OccupySeat(0, -1); // -1 for player
            return handle;
        }

        /// <summary>
        /// Puts a ped in a vehicle at a specific seat.
        /// </summary>
        public void PutPedInVehicle(int pedHandle, int vehicleHandle, int seatIndex)
        {
            _pedsInVehicles[pedHandle] = vehicleHandle;
            if (_vehicles.TryGetValue(vehicleHandle, out var vehicle))
            {
                vehicle.OccupySeat(seatIndex, pedHandle);
            }
        }

        private readonly HashSet<int> _pressedControls = new HashSet<int>();
        private readonly HashSet<int> _justPressedControls = new HashSet<int>();

        /// <summary>
        /// Simulates a control being held down (for testing).
        /// </summary>
        public void SetControlPressed(int control, bool pressed)
        {
            if (pressed)
                _pressedControls.Add(control);
            else
                _pressedControls.Remove(control);
        }

        /// <summary>
        /// Simulates a control being just pressed this frame (for testing).
        /// </summary>
        public void SetControlJustPressed(int control, bool justPressed)
        {
            if (justPressed)
                _justPressedControls.Add(control);
            else
                _justPressedControls.Remove(control);
        }

        public bool IsControlPressed(int control) => _pressedControls.Contains(control);

        public bool IsControlJustPressed(int control) => _justPressedControls.Contains(control);

        private class PedState
        {
            public string ModelName { get; set; } = string.Empty;
            public Vector3 Position { get; set; }
            public bool IsAlive { get; set; }
            public string RelationshipGroup { get; set; } = string.Empty;
            public string Weapon { get; set; } = string.Empty;
            public float Accuracy { get; set; } = 0.5f;
            public int Armor { get; set; } = 0;
            public int Health { get; set; } = 100;
            public bool CanUseCover { get; set; } = false;
            public bool WillFightArmedPeds { get; set; } = false;
            public bool IsAttackingPlayer { get; set; } = false;
            public bool CanSwitchWeapons { get; set; } = true; // Default: can switch weapons
        }

        private class BlipState
        {
            public Vector3 Position { get; set; }
            public BlipColor Color { get; set; }
            public int Sprite { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class VehicleState
        {
            private readonly Dictionary<int, int> _occupiedSeats = new Dictionary<int, int>(); // seatIndex -> pedHandle (-1 for player)
            private readonly int _totalSeats;

            public string ModelName { get; }
            public Vector3 Position { get; set; }
            public int VehicleClass { get; set; } = 0; // Default to Compacts
            public HashSet<int> TurretSeats { get; } = new HashSet<int>();

            public VehicleState(int totalSeats, string modelName = "", Vector3 position = default)
            {
                _totalSeats = totalSeats;
                ModelName = modelName;
                Position = position;
            }

            public int[] GetFreeSeats()
            {
                var freeSeats = new List<int>();
                // Seat 0 is driver, 1+ are passengers
                for (int i = 0; i < _totalSeats; i++)
                {
                    if (!_occupiedSeats.ContainsKey(i))
                    {
                        freeSeats.Add(i);
                    }
                }
                return freeSeats.ToArray();
            }

            public void OccupySeat(int seatIndex, int pedHandle)
            {
                if (seatIndex >= 0 && seatIndex < _totalSeats)
                {
                    _occupiedSeats[seatIndex] = pedHandle;
                }
            }

            public void FreeSeatByPed(int pedHandle)
            {
                var seatToRemove = -1;
                foreach (var kvp in _occupiedSeats)
                {
                    if (kvp.Value == pedHandle)
                    {
                        seatToRemove = kvp.Key;
                        break;
                    }
                }
                if (seatToRemove >= 0)
                {
                    _occupiedSeats.Remove(seatToRemove);
                }
            }
        }
    }
}
