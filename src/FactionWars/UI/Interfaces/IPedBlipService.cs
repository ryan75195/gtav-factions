using FactionWars.Core.Interfaces;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service for managing minimap blips attached to peds.
    /// Tracks blip handles and provides lifecycle management.
    /// </summary>
    public interface IPedBlipService
    {
        /// <summary>
        /// Creates a blip for a ped with the specified color.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to attach blip to.</param>
        /// <param name="color">Color of the blip.</param>
        /// <returns>Handle to the created blip, or -1 if creation failed.</returns>
        int CreateBlipForPed(int pedHandle, BlipColor color);

        /// <summary>
        /// Removes the blip for a ped.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped whose blip to remove.</param>
        void RemoveBlipForPed(int pedHandle);

        /// <summary>
        /// Checks if a blip exists for a ped.
        /// </summary>
        /// <param name="pedHandle">Handle of the ped to check.</param>
        /// <returns>True if a blip is tracked for this ped.</returns>
        bool HasBlipForPed(int pedHandle);

        /// <summary>
        /// Removes all tracked blips.
        /// </summary>
        void RemoveAllBlips();
    }
}
