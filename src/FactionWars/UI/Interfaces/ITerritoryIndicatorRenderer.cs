using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Renderer interface for the territory indicator HUD.
    /// Implementations handle the actual drawing of the zone status display.
    /// </summary>
    public interface ITerritoryIndicatorRenderer
    {
        /// <summary>
        /// Gets whether the territory indicator is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Renders the territory indicator with the specified data.
        /// </summary>
        /// <param name="data">The territory indicator data to display.</param>
        void Render(TerritoryIndicatorData data);

        /// <summary>
        /// Hides the territory indicator.
        /// </summary>
        void Hide();
    }
}
