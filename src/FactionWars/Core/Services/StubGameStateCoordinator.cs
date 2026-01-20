using FactionWars.Core.Interfaces;

namespace FactionWars.Core.Services
{
    /// <summary>
    /// Stub implementation of IGameStateCoordinator for use before full persistence is implemented.
    /// Performs no actual save/load operations.
    /// </summary>
    public class StubGameStateCoordinator : IGameStateCoordinator
    {
        /// <inheritdoc />
        public bool IsSaving => false;

        /// <inheritdoc />
        public bool IsLoading => false;

        /// <inheritdoc />
        public void SaveToSlot(int slotNumber)
        {
            // Stub - no operation
        }

        /// <inheritdoc />
        public void LoadFromSlot(int slotNumber)
        {
            // Stub - no operation
        }
    }
}
