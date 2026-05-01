namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Abstraction over GTA V native function calls.
    /// Enables unit testing by allowing mock implementations.
    /// </summary>
    public interface IGameBridge
    {
        /// <summary>
        /// Gets the current player's position in world coordinates.
        /// </summary>
        Vector3 GetPlayerPosition();

        /// <summary>
        /// Creates a ped (pedestrian/NPC) at the specified position.
        /// </summary>
        /// <param name="modelName">The model name or hash of the ped.</param>
        /// <param name="position">World position to spawn the ped.</param>
        /// <returns>Handle to the created ped, or -1 if creation failed.</returns>
        int CreatePed(string modelName, Vector3 position);

        /// <summary>
        /// Deletes a ped from the world.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to delete.</param>
        void DeletePed(int pedHandle);

        /// <summary>
        /// Checks if a ped is still alive.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to check.</param>
        /// <returns>True if the ped exists and is alive.</returns>
        bool IsPedAlive(int pedHandle);

        /// <summary>
        /// Sets the relationship group for a ped, affecting combat behavior.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="groupName">Name of the relationship group.</param>
        void SetPedRelationshipGroup(int pedHandle, string groupName);

        /// <summary>
        /// Creates a blip on the map at the specified position.
        /// </summary>
        /// <param name="position">World position for the blip.</param>
        /// <returns>Handle to the created blip.</returns>
        int CreateBlip(Vector3 position);

        /// <summary>
        /// Deletes a blip from the map.
        /// </summary>
        /// <param name="blipHandle">Handle of the blip to delete.</param>
        void DeleteBlip(int blipHandle);

        /// <summary>
        /// Creates a translucent filled-circle blip on the minimap and pause map,
        /// rendered as a radius around the given world position.
        /// </summary>
        /// <param name="center">World position at the centre of the radius.</param>
        /// <param name="radius">Radius in world units.</param>
        /// <returns>Handle to the created blip, or -1 on failure.</returns>
        int CreateRadiusBlip(Vector3 center, float radius);

        /// <summary>
        /// Sets the color of a blip.
        /// </summary>
        /// <param name="blipHandle">Handle of the blip.</param>
        /// <param name="color">Color to set.</param>
        void SetBlipColor(int blipHandle, BlipColor color);

        /// <summary>
        /// Sets the alpha (opacity) of a blip. Range 0 (invisible) to 255 (fully opaque).
        /// Useful for radius blips where the default opacity hides the underlying map.
        /// </summary>
        void SetBlipAlpha(int blipHandle, int alpha);

        /// <summary>
        /// Sets the sprite (icon) of a blip.
        /// </summary>
        /// <param name="blipHandle">Handle of the blip.</param>
        /// <param name="spriteId">Sprite ID (e.g., 84 for skull and crossbones).</param>
        void SetBlipSprite(int blipHandle, int spriteId);

        /// <summary>
        /// Sets the name/label of a blip that appears when hovering over it on the map.
        /// </summary>
        /// <param name="blipHandle">Handle of the blip.</param>
        /// <param name="name">The name to display for the blip.</param>
        void SetBlipName(int blipHandle, string name);

        /// <summary>
        /// Shows a notification message to the player.
        /// </summary>
        /// <param name="message">Message to display.</param>
        void ShowNotification(string message);

        /// <summary>
        /// Gets the current game time in milliseconds.
        /// </summary>
        /// <returns>Game time in milliseconds.</returns>
        int GetGameTime();

        /// <summary>
        /// Revives a dead ped, restoring them to full health.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to revive.</param>
        /// <returns>True if the ped was successfully revived.</returns>
        bool RevivePed(int pedHandle);

        /// <summary>
        /// Teleports a ped to a new position.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to move.</param>
        /// <param name="position">The new position to teleport the ped to.</param>
        void SetPedPosition(int pedHandle, Vector3 position);

        /// <summary>
        /// Changes the model/appearance of a ped.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to modify.</param>
        /// <param name="modelName">The new model name to apply.</param>
        /// <returns>True if the model was changed successfully.</returns>
        bool SetPedModel(int pedHandle, string modelName);

        /// <summary>
        /// Gets the model name of the player's current character.
        /// Used to detect which protagonist (Michael, Franklin, Trevor) is active.
        /// </summary>
        /// <returns>The model name of the player character (e.g., "player_zero" for Michael).</returns>
        string GetPlayerCharacterModel();

        /// <summary>
        /// Gets the player's current heading (direction they're facing) in degrees.
        /// 0 = North, 90 = East, 180 = South, 270 = West.
        /// </summary>
        /// <returns>The player's heading in degrees (0-360).</returns>
        float GetPlayerHeading();

        /// <summary>
        /// Checks if the player character is currently dead.
        /// </summary>
        /// <returns>True if the player is dead, false if alive.</returns>
        bool IsPlayerDead();

        /// <summary>
        /// Gets the player's current money amount.
        /// </summary>
        /// <returns>The player's money in GTA V dollars.</returns>
        int GetPlayerMoney();

        /// <summary>
        /// Adds money to the player's account.
        /// </summary>
        /// <param name="amount">Amount to add (can be negative to subtract).</param>
        void AddPlayerMoney(int amount);

        /// <summary>
        /// Sets the player's money to an exact amount.
        /// </summary>
        /// <param name="amount">The exact amount to set.</param>
        void SetPlayerMoney(int amount);

        /// <summary>
        /// Gets the player's total play time in seconds, as tracked by GTA V's stats system
        /// (e.g., MP0_TOTAL_PLAYING_TIME or its single-player equivalent). Persisted in the
        /// savegame and restored exactly on load — primary key for save identification.
        /// </summary>
        /// <returns>Total seconds played, or null if the stat read failed.</returns>
        long? GetTotalPlayTimeSeconds();

        /// <summary>
        /// Returns the active SP character index: 0=Michael, 1=Franklin, 2=Trevor.
        /// Each character has an independent TOTAL_PLAYING_TIME stat, so save-load
        /// detection must scope play-time comparisons to a single character.
        /// </summary>
        int GetActiveCharacterIndex();

        /// <summary>
        /// Gets the count of completed story missions for the active character.
        /// Used as a tiebreaker for save fingerprint matching.
        /// </summary>
        /// <returns>Number of completed missions.</returns>
        int GetCompletedMissionCount();

        /// <summary>
        /// Gets the in-game wall clock as minutes-of-day (HH*60+MM, 0-1439).
        /// Used as a tiebreaker for save fingerprint matching.
        /// </summary>
        /// <returns>Minutes-of-day in [0, 1440).</returns>
        int GetInGameClockMinutes();

        /// <summary>
        /// Removes all weapons from the player.
        /// </summary>
        void RemoveAllPlayerWeapons();

        /// <summary>
        /// Gives a weapon to the player character.
        /// </summary>
        /// <param name="weaponName">The weapon name/hash (e.g., "weapon_pistol").</param>
        /// <param name="ammo">Amount of ammo to give.</param>
        void GivePlayerWeapon(string weaponName, int ammo);

        /// <summary>
        /// Configures player settings for the mod (weapon drops, etc.).
        /// Should be called once during initialization.
        /// </summary>
        void ConfigurePlayerSettings();

        /// <summary>
        /// Makes a ped follow the player as a bodyguard.
        /// Sets up the appropriate task and relationship group so the ped follows and assists in combat.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to make follow.</param>
        void SetPedAsFollower(int pedHandle);

        /// <summary>
        /// Checks if the player is currently in a vehicle.
        /// </summary>
        /// <returns>True if the player is in a vehicle, false otherwise.</returns>
        bool IsPlayerInVehicle();

        /// <summary>
        /// Gets the handle of the vehicle the player is currently in.
        /// </summary>
        /// <returns>The vehicle handle, or -1 if player is not in a vehicle.</returns>
        int GetPlayerVehicle();

        /// <summary>
        /// Checks if a ped is currently in any vehicle.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to check.</param>
        /// <returns>True if the ped is in a vehicle, false otherwise.</returns>
        bool IsPedInVehicle(int pedHandle);

        /// <summary>
        /// Checks if a ped is currently trying to enter a vehicle.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to check.</param>
        /// <returns>True if the ped is attempting to enter a vehicle, false otherwise.</returns>
        bool IsPedTryingToEnterVehicle(int pedHandle);

        /// <summary>
        /// Gets the available (free) seat indices for a vehicle.
        /// </summary>
        /// <param name="vehicleHandle">Handle of the vehicle to check.</param>
        /// <returns>An array of free seat indices (0 = driver, 1+ = passengers).</returns>
        int[] GetVehicleFreeSeats(int vehicleHandle);

        /// <summary>
        /// Gets the vehicle class (e.g., 15=helicopter, 14=boat, 8=motorcycle).
        /// </summary>
        /// <param name="vehicleHandle">Handle of the vehicle.</param>
        /// <returns>Vehicle class ID, or -1 if invalid.</returns>
        int GetVehicleClass(int vehicleHandle);

        /// <summary>
        /// Checks if a vehicle seat is a turret/gun seat.
        /// </summary>
        /// <param name="vehicleHandle">Handle of the vehicle.</param>
        /// <param name="seatIndex">The seat index to check.</param>
        /// <returns>True if the seat is a turret, false otherwise.</returns>
        bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex);

        /// <summary>
        /// Gets the position of a vehicle.
        /// </summary>
        /// <param name="vehicleHandle">Handle of the vehicle.</param>
        /// <returns>The vehicle's position, or Vector3.Zero if invalid.</returns>
        Vector3 GetVehiclePosition(int vehicleHandle);

        /// <summary>
        /// Tasks a ped to enter a specific vehicle and seat.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to task.</param>
        /// <param name="vehicleHandle">Handle of the vehicle to enter.</param>
        /// <param name="seatIndex">The seat index to enter (0 = driver, 1+ = passengers).</param>
        void TaskPedEnterVehicle(int pedHandle, int vehicleHandle, int seatIndex);

        /// <summary>
        /// Tasks a ped to leave their current vehicle.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to task.</param>
        void TaskPedLeaveVehicle(int pedHandle);

        /// <summary>
        /// Gives a weapon to a ped.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to give the weapon to.</param>
        /// <param name="weaponName">The weapon name/hash (e.g., "weapon_pistol", "weapon_smg").</param>
        void GivePedWeapon(int pedHandle, string weaponName);

        /// <summary>
        /// Sets whether a ped can switch between weapons.
        /// Set to false for RPG users to prevent AI from switching to pistol.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="canSwitch">True to allow weapon switching, false to lock current weapon.</param>
        void SetPedCanSwitchWeapons(int pedHandle, bool canSwitch);

        /// <summary>
        /// Sets a ped's shooting accuracy.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="accuracy">Accuracy value from 0.0 (worst) to 1.0 (best).</param>
        void SetPedAccuracy(int pedHandle, float accuracy);

        /// <summary>
        /// Sets a ped's armor value.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="armor">Armor value (0 = no armor, 100 = full armor).</param>
        void SetPedArmor(int pedHandle, int armor);

        /// <summary>
        /// Sets a ped's maximum and current health.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="health">Health value (typically 100-200).</param>
        void SetPedHealth(int pedHandle, int health);

        /// <summary>
        /// Configures a ped's combat behavior attributes.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="canUseCover">Whether the ped will use cover during combat.</param>
        /// <param name="willFightArmedPeds">Whether the ped will engage armed enemies.</param>
        void SetPedCombatAttributes(int pedHandle, bool canUseCover, bool willFightArmedPeds);

        /// <summary>
        /// Sets a waypoint on the map at the specified position.
        /// The player can use this for navigation but must travel there manually.
        /// </summary>
        /// <param name="position">World position for the waypoint.</param>
        void SetWaypoint(Vector3 position);

        /// <summary>
        /// Clears any currently set waypoint from the map.
        /// </summary>
        void ClearWaypoint();

        /// <summary>
        /// Makes a ped hostile to the player and tasks them to attack.
        /// Used for enemy defenders.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to make hostile.</param>
        void SetPedToAttackPlayer(int pedHandle);

        /// <summary>
        /// Creates a blip attached to a ped that follows the ped on the minimap.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to attach blip to.</param>
        /// <returns>Handle to the created blip, or -1 if creation failed.</returns>
        int CreateBlipForPed(int pedHandle);

        /// <summary>
        /// Tasks a ped to wander within a specified area at walking pace.
        /// Used for zone defenders that patrol instead of following the player.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to task.</param>
        /// <param name="center">Center point of the wander area.</param>
        /// <param name="radius">Radius of the wander area in meters.</param>
        void TaskPedWanderInArea(int pedHandle, Vector3 center, float radius);

        /// <summary>
        /// Tasks a ped to wander within a specified area at sprinting pace.
        /// Used for zone defenders actively searching for enemies during battles.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to task.</param>
        /// <param name="center">Center point of the wander area.</param>
        /// <param name="radius">Radius of the wander area in meters.</param>
        void TaskPedWanderInAreaSprinting(int pedHandle, Vector3 center, float radius);

        /// <summary>
        /// Tasks a ped to actively seek out and fight any hated targets within range.
        /// Used for friendly defenders during battles to make them engage enemies.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to task.</param>
        /// <param name="radius">The radius in which to search for enemies.</param>
        void TaskCombatHatedTargetsAroundPed(int pedHandle, float radius);

        /// <summary>
        /// Makes a ped friendly to the player by setting them to the player's relationship group.
        /// Unlike SetPedAsFollower, this does NOT make them follow the player or join the ped group.
        /// Used for friendly zone defenders who should not attack the player or followers.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to make friendly.</param>
        void SetPedAsFriendly(int pedHandle);

        /// <summary>
        /// Gets the ground Z coordinate at the specified X/Y position.
        /// Uses GTA V's GET_GROUND_Z_FOR_3D_COORD native.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Starting Z coordinate for search.</param>
        /// <returns>The ground Z coordinate, or the input Z if ground not found.</returns>
        float GetGroundZ(float x, float y, float z);

        /// <summary>
        /// Sets a ped as hostile to the player's faction but allows wandering.
        /// The ped will engage player and player's followers on sight while patrolling.
        /// </summary>
        /// <param name="pedHandle">The ped handle.</param>
        void SetPedAsHostileWanderer(int pedHandle);

        /// <summary>
        /// Gets a safe coordinate for spawning a pedestrian at the specified position.
        /// Uses GTA V's navmesh to find a position that is walkable (not on rooftops).
        /// Falls back to GetGroundZ if no safe coordinate is found.
        /// </summary>
        /// <param name="position">The desired spawn position.</param>
        /// <returns>A safe spawn position at ground level, or the original position if none found.</returns>
        Vector3 GetSafeCoordForPed(Vector3 position);

        /// <summary>
        /// Gets the path to the GTA V scripts directory where the mod is installed.
        /// </summary>
        /// <returns>The full path to the scripts directory.</returns>
        string GetScriptsDirectory();

        /// <summary>
        /// Checks if the player is currently free-aiming (aiming a weapon or in aim mode).
        /// </summary>
        /// <returns>True if the player is free-aiming.</returns>
        bool IsPlayerFreeAiming();

        /// <summary>
        /// Gets the entity handle that the player is currently aiming at.
        /// Returns 0 if not aiming at any entity.
        /// </summary>
        /// <returns>Entity handle, or 0 if not aiming at an entity.</returns>
        int GetEntityPlayerIsAimingAt();

        /// <summary>
        /// Displays help text at the bottom of the screen (like "Press E to...").
        /// </summary>
        /// <param name="text">The text to display. Supports GTA text formatting.</param>
        void DisplayHelpText(string text);

        /// <summary>
        /// Gets the world position of a ped.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <returns>The ped's current position, or Vector3.Zero if invalid.</returns>
        Vector3 GetPedPosition(int pedHandle);

        /// <summary>
        /// Clears all tasks from a ped, stopping any current activity.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        void ClearPedTasks(int pedHandle);

        /// <summary>
        /// Makes a ped turn to face a specific position and remain idle.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="position">Position to face.</param>
        void TaskPedTurnToFacePosition(int pedHandle, Vector3 position);

        /// <summary>
        /// Sets a ped's seeing range (how far they can visually detect enemies).
        /// Default is around 70 meters. Set higher for zone-wide visibility during battles.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="range">The seeing range in meters.</param>
        void SetPedSeeingRange(int pedHandle, float range);

        /// <summary>
        /// Sets a ped's hearing range (how far they can detect enemies by sound).
        /// Default is around 60 meters. Set higher for zone-wide awareness during battles.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped.</param>
        /// <param name="range">The hearing range in meters.</param>
        void SetPedHearingRange(int pedHandle, float range);

        /// <summary>
        /// Creates a vehicle at the specified position.
        /// </summary>
        /// <param name="modelName">The model name of the vehicle (e.g., "insurgent", "buzzard").</param>
        /// <param name="position">World position to spawn the vehicle.</param>
        /// <returns>Handle to the created vehicle, or -1 if creation failed.</returns>
        int CreateVehicle(string modelName, Vector3 position);

        /// <summary>
        /// Creates a blip attached to a vehicle that follows the vehicle on the minimap.
        /// </summary>
        /// <param name="vehicleHandle">Handle of the vehicle to attach blip to.</param>
        /// <returns>Handle to the created blip, or -1 if creation failed.</returns>
        int CreateBlipForVehicle(int vehicleHandle);

        /// <summary>
        /// Gets the nearest road position to the specified location.
        /// Uses vehicle node pathfinding to find a valid road surface.
        /// </summary>
        /// <param name="position">The reference position to search from.</param>
        /// <returns>The nearest road position, or the input position if no road found.</returns>
        Vector3 GetNearestRoadPosition(Vector3 position);

        /// <summary>
        /// Gets the model name of a vehicle.
        /// </summary>
        /// <param name="vehicleHandle">Handle of the vehicle.</param>
        /// <returns>The model name (e.g., "insurgent", "buzzard"), or empty string if invalid.</returns>
        string GetVehicleModelName(int vehicleHandle);

        /// <summary>
        /// Gets all weapons the player currently has along with their ammo counts.
        /// </summary>
        /// <returns>A dictionary mapping weapon names to ammo counts.</returns>
        System.Collections.Generic.Dictionary<string, int> GetPlayerWeapons();

        /// <summary>
        /// Checks if a GTA V control (gamepad/keyboard) is currently pressed.
        /// </summary>
        /// <param name="control">The GTA V control ID (e.g., 175 for D-pad Right).</param>
        /// <returns>True if the control is currently held down.</returns>
        bool IsControlPressed(int control);

        /// <summary>
        /// Checks if a GTA V control was just pressed this frame.
        /// </summary>
        /// <param name="control">The GTA V control ID.</param>
        /// <returns>True if the control was just pressed.</returns>
        bool IsControlJustPressed(int control);
    }
}
