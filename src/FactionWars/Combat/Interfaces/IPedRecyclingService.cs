using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using System.Collections.Generic;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service interface for recycling peds instead of destroying and recreating them.
    /// Recycling repurposes existing game entities with new faction/zone assignments,
    /// which is more efficient than full delete/create cycles.
    /// </summary>
    public interface IPedRecyclingService
    {
        /// <summary>
        /// Recycles a dead ped for reuse by a new faction.
        /// The ped is revived, moved to a new position, and assigned to a new faction/zone.
        /// </summary>
        /// <param name="pedHandle">The handle of the dead ped to recycle.</param>
        /// <param name="newFactionId">The new faction this ped will belong to.</param>
        /// <param name="newPosition">The new position to place the ped.</param>
        /// <param name="newZoneId">Optional new zone ID for the ped.</param>
        /// <returns>A new PedHandle with updated metadata if successful, null if recycling failed.</returns>
        PedHandle? RecyclePed(PedHandle pedHandle, string newFactionId, Vector3 newPosition, string? newZoneId);

        /// <summary>
        /// Recycles a ped by its handle value.
        /// </summary>
        /// <param name="handle">The handle of the ped to recycle.</param>
        /// <param name="newFactionId">The new faction this ped will belong to.</param>
        /// <param name="newPosition">The new position to place the ped.</param>
        /// <param name="newZoneId">Optional new zone ID for the ped.</param>
        /// <returns>A new PedHandle with updated metadata if successful, null if recycling failed.</returns>
        PedHandle? RecyclePed(int handle, string newFactionId, Vector3 newPosition, string? newZoneId);

        /// <summary>
        /// Gets all peds that are eligible for recycling.
        /// A ped is recyclable if it is dead or marked for deletion.
        /// </summary>
        /// <returns>An enumerable of recyclable peds.</returns>
        IEnumerable<PedHandle> GetRecyclablePeds();

        /// <summary>
        /// Gets the count of peds that are eligible for recycling.
        /// </summary>
        /// <returns>The number of recyclable peds.</returns>
        int GetRecyclableCount();

        /// <summary>
        /// Checks if there are any peds available for recycling.
        /// </summary>
        /// <returns>True if at least one ped is recyclable.</returns>
        bool HasRecyclablePeds();

        /// <summary>
        /// Attempts to get a ped for recycling, preferring dead peds first.
        /// Does not actually recycle the ped - just selects a candidate.
        /// </summary>
        /// <returns>A recyclable ped if one exists, null otherwise.</returns>
        PedHandle? GetNextRecyclableCandidate();

        /// <summary>
        /// Recycles all dead peds for use by a specific faction in a zone.
        /// Each recycled ped is placed at the specified position.
        /// </summary>
        /// <param name="newFactionId">The faction to assign recycled peds to.</param>
        /// <param name="newPosition">The position to place recycled peds.</param>
        /// <param name="newZoneId">Optional zone ID for the recycled peds.</param>
        /// <param name="maxCount">Maximum number of peds to recycle.</param>
        /// <returns>A list of recycled ped handles.</returns>
        IList<PedHandle> RecycleDeadPeds(string newFactionId, Vector3 newPosition, string? newZoneId, int maxCount);

        /// <summary>
        /// Marks a ped as recycled (used for tracking purposes).
        /// </summary>
        /// <param name="pedHandle">The ped to mark as recycled.</param>
        /// <returns>True if the ped was successfully marked.</returns>
        bool MarkAsRecycled(PedHandle pedHandle);
    }
}
