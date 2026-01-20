using System.Collections.Generic;
using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Renderer interface for the event feed display.
    /// Implementations handle the actual drawing of world events in the bottom-left of the screen.
    /// </summary>
    public interface IEventFeedRenderer
    {
        /// <summary>
        /// Gets whether the event feed is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Gets the maximum number of entries to display at once.
        /// </summary>
        int MaxDisplayCount { get; }

        /// <summary>
        /// Renders the event feed with the specified entries.
        /// </summary>
        /// <param name="entries">The entries to display, ordered from newest to oldest.</param>
        void Render(IReadOnlyList<EventFeedEntry> entries);

        /// <summary>
        /// Hides the event feed.
        /// </summary>
        void Hide();
    }
}
