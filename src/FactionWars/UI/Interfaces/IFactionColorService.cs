using FactionWars.Factions.Models;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service for managing faction color assignments.
    /// Provides predefined colors for each faction type and conversion utilities.
    /// </summary>
    public interface IFactionColorService
    {
        /// <summary>
        /// Gets the faction color for a specific faction type.
        /// </summary>
        /// <param name="factionType">The faction type.</param>
        /// <returns>The color associated with the faction.</returns>
        FactionColor GetColorForFactionType(FactionType factionType);

        /// <summary>
        /// Gets the boundary color enum value for a specific faction type.
        /// </summary>
        /// <param name="factionType">The faction type.</param>
        /// <returns>The boundary color enum value.</returns>
        BoundaryColor GetBoundaryColorForFactionType(FactionType factionType);

        /// <summary>
        /// Gets the neutral color for unowned/contested zones.
        /// </summary>
        /// <returns>The neutral faction color.</returns>
        FactionColor GetNeutralColor();

        /// <summary>
        /// Gets the neutral boundary color enum value.
        /// </summary>
        /// <returns>The neutral boundary color.</returns>
        BoundaryColor GetNeutralBoundaryColor();

        /// <summary>
        /// Gets the faction color associated with a boundary color enum value.
        /// </summary>
        /// <param name="boundaryColor">The boundary color enum.</param>
        /// <returns>The corresponding faction color.</returns>
        FactionColor GetFactionColorForBoundaryColor(BoundaryColor boundaryColor);

        /// <summary>
        /// Gets a faction color with a custom alpha value.
        /// </summary>
        /// <param name="factionType">The faction type.</param>
        /// <param name="alpha">The alpha value (0-255).</param>
        /// <returns>The faction color with modified alpha.</returns>
        FactionColor GetColorWithAlpha(FactionType factionType, int alpha);

        /// <summary>
        /// Attempts to determine the faction type from a color.
        /// </summary>
        /// <param name="color">The color to check.</param>
        /// <param name="factionType">The matched faction type if found.</param>
        /// <returns>True if the color matches a known faction, false otherwise.</returns>
        bool TryGetFactionTypeFromColor(FactionColor color, out FactionType factionType);
    }
}
