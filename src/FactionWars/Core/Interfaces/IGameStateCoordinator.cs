namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Coordinates game state operations including save and load functionality.
    /// This interface abstracts the coordination between persistence and game state restoration.
    /// </summary>
    public interface IGameStateCoordinator
    {
        /// <summary>
        /// Saves the current game state to the specified slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based) to save to.</param>
        void SaveToSlot(int slotNumber);

        /// <summary>
        /// Loads game state from the specified slot.
        /// </summary>
        /// <param name="slotNumber">The slot number (0-based) to load from.</param>
        void LoadFromSlot(int slotNumber);

        /// <summary>
        /// Gets whether a save operation is currently in progress.
        /// </summary>
        bool IsSaving { get; }

        /// <summary>
        /// Gets whether a load operation is currently in progress.
        /// </summary>
        bool IsLoading { get; }
    }
}
