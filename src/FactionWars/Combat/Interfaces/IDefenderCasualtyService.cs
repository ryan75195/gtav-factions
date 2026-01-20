using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service for processing defender casualties during combat.
    /// Identifies dead defenders and deducts them from zone allocations.
    /// </summary>
    public interface IDefenderCasualtyService
    {
        /// <summary>
        /// Processes all defender casualties, identifying dead defender peds,
        /// deducting them from zone allocations, and removing them from the ped pool.
        /// Should be called periodically during combat.
        /// </summary>
        /// <returns>A result containing casualty counts by tier.</returns>
        CasualtyResult ProcessCasualties();
    }
}
