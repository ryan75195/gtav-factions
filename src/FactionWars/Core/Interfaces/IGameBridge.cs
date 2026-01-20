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
        /// Sets the color of a blip.
        /// </summary>
        /// <param name="blipHandle">Handle of the blip.</param>
        /// <param name="color">Color to set.</param>
        void SetBlipColor(int blipHandle, BlipColor color);

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
        /// Tasks a ped to wander within a specified area.
        /// Used for zone defenders that patrol instead of following the player.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to task.</param>
        /// <param name="center">Center point of the wander area.</param>
        /// <param name="radius">Radius of the wander area in meters.</param>
        void TaskPedWanderInArea(int pedHandle, Vector3 center, float radius);
    }
}
