using System;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Represents a command that can be executed via the in-game phone.
    /// </summary>
    public class PhoneCommand
    {
        private string _category = string.Empty;

        /// <summary>
        /// Unique identifier for this phone command.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Display name shown to the user.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of what the command does.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Whether this command is currently enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Category for grouping commands (e.g., "Faction", "Territory").
        /// </summary>
        public string Category
        {
            get => _category;
            set => _category = value ?? string.Empty;
        }

        /// <summary>
        /// Creates a new phone command.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="name">Display name.</param>
        /// <param name="description">Optional description.</param>
        /// <exception cref="ArgumentNullException">Thrown if id or name is null.</exception>
        /// <exception cref="ArgumentException">Thrown if id or name is empty or whitespace.</exception>
        public PhoneCommand(string id, string name, string description = "")
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty or whitespace.", nameof(name));

            Id = id;
            Name = name;
            Description = description ?? string.Empty;
            IsEnabled = true;
        }

        /// <summary>
        /// Determines equality based on Id.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is PhoneCommand other)
            {
                return Id == other.Id;
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code based on Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
