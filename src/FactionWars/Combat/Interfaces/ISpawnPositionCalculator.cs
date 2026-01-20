using System.Collections.Generic;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Service interface for calculating spawn positions for peds.
    /// Provides methods to calculate positions behind the player for natural spawning.
    /// </summary>
    public interface ISpawnPositionCalculator
    {
        /// <summary>
        /// Calculates a position behind the player at the specified distance.
        /// </summary>
        /// <param name="distance">Distance behind the player in meters.</param>
        /// <returns>A Vector3 position behind the player.</returns>
        Vector3 CalculateBehindPlayer(float distance);

        /// <summary>
        /// Calculates a natural spawn position behind the player.
        /// Uses a default distance range for natural-feeling spawns.
        /// </summary>
        /// <returns>A Vector3 position behind the player at a natural distance.</returns>
        Vector3 CalculateNaturalSpawnPosition();

        /// <summary>
        /// Calculates multiple spread-out spawn positions behind the player.
        /// Positions are distributed to avoid overlapping peds.
        /// </summary>
        /// <param name="count">Number of spawn positions to calculate.</param>
        /// <returns>A list of Vector3 positions behind the player.</returns>
        IList<Vector3> CalculateNaturalSpawnPositions(int count);

        /// <summary>
        /// Calculates multiple spawn positions spread around a center point.
        /// Used for immediate spawning of all defenders at once.
        /// </summary>
        /// <param name="center">Center point to spread around.</param>
        /// <param name="count">Number of positions to calculate.</param>
        /// <param name="minRadius">Minimum distance from center.</param>
        /// <param name="maxRadius">Maximum distance from center.</param>
        /// <returns>List of spawn positions.</returns>
        IList<Vector3> CalculateSpreadPositions(Vector3 center, int count, float minRadius, float maxRadius);
    }
}
