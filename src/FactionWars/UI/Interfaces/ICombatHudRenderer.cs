using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Interface for rendering combat HUD elements.
    /// Abstracts the actual rendering implementation (NativeUI, custom draw calls, etc.).
    /// </summary>
    public interface ICombatHudRenderer
    {
        /// <summary>
        /// Gets whether the combat HUD is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Renders the combat HUD with the provided data.
        /// </summary>
        /// <param name="data">The combat HUD data to display.</param>
        void RenderCombatHud(CombatHudData data);

        /// <summary>
        /// Hides the combat HUD.
        /// </summary>
        void HideCombatHud();
    }
}
