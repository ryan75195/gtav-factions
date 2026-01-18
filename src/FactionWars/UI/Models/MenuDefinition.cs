using System;
using System.Collections.Generic;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Represents the definition of a menu, including its items and metadata.
    /// </summary>
    public class MenuDefinition
    {
        private readonly List<MenuItem> _items;

        /// <summary>
        /// Unique identifier for this menu.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Title displayed at the top of the menu.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Optional subtitle displayed below the title.
        /// </summary>
        public string Subtitle { get; }

        /// <summary>
        /// Read-only collection of menu items.
        /// </summary>
        public IReadOnlyList<MenuItem> Items => _items.AsReadOnly();

        /// <summary>
        /// Creates a new menu definition.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="title">Menu title.</param>
        /// <param name="subtitle">Optional subtitle.</param>
        /// <exception cref="ArgumentNullException">Thrown if id or title is null.</exception>
        /// <exception cref="ArgumentException">Thrown if id or title is empty.</exception>
        public MenuDefinition(string id, string title, string subtitle = "")
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));
            if (title == null)
                throw new ArgumentNullException(nameof(title));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty or whitespace.", nameof(title));

            Id = id;
            Title = title;
            Subtitle = subtitle ?? string.Empty;
            _items = new List<MenuItem>();
        }

        /// <summary>
        /// Adds an item to the menu.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if item is null.</exception>
        public void AddItem(MenuItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _items.Add(item);
        }

        /// <summary>
        /// Removes all items from the menu.
        /// </summary>
        public void ClearItems()
        {
            _items.Clear();
        }

        /// <summary>
        /// Gets an item by its ID.
        /// </summary>
        /// <param name="itemId">The item ID to find.</param>
        /// <returns>The item if found, null otherwise.</returns>
        public MenuItem? GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            return _items.Find(i => i.Id == itemId);
        }
    }
}
