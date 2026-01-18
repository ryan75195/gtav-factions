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
    }
}
