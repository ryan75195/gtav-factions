using System;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Represents a menu item that can be displayed in a game menu.
    /// </summary>
    public class MenuItem
    {
        /// <summary>
        /// Unique identifier for this menu item.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Display text shown to the user.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Optional description shown below the item.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Whether this item is currently enabled (can be selected).
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Creates a new menu item.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="text">Display text.</param>
        /// <param name="description">Optional description.</param>
        /// <exception cref="ArgumentNullException">Thrown if id or text is null.</exception>
        /// <exception cref="ArgumentException">Thrown if id or text is empty.</exception>
        public MenuItem(string id, string text, string description = "")
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty or whitespace.", nameof(text));

            Id = id;
            Text = text;
            Description = description ?? string.Empty;
            IsEnabled = true;
        }
    }
}
