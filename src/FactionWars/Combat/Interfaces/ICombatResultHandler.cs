using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service interface for processing completed combat encounters
    /// and updating zone state accordingly.
    /// </summary>
    public interface ICombatResultHandler
    {
        /// <summary>
        /// Processes a completed combat encounter and updates zone state.
        /// </summary>
        /// <param name="encounter">The completed combat encounter to process.</param>
        /// <returns>A result indicating the outcome of processing.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if encounter is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if encounter is still in progress.</exception>
        CombatProcessingResult ProcessCombatResult(CombatEncounter encounter);
    }
}
