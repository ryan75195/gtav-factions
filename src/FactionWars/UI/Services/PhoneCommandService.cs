using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing phone commands.
    /// Provides registration, retrieval, and execution of phone commands.
    /// </summary>
    public class PhoneCommandService : IPhoneCommandService
    {
        private readonly IPhoneCommandHandler _commandHandler;
        private readonly Dictionary<string, PhoneCommand> _commands;

        /// <summary>
        /// Creates a new phone command service.
        /// </summary>
        /// <param name="commandHandler">The handler for executing commands.</param>
        /// <exception cref="ArgumentNullException">Thrown if commandHandler is null.</exception>
        public PhoneCommandService(IPhoneCommandHandler commandHandler)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _commands = new Dictionary<string, PhoneCommand>();
        }

        /// <inheritdoc/>
        public void RegisterCommand(PhoneCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (_commands.ContainsKey(command.Id))
                throw new InvalidOperationException($"A command with ID '{command.Id}' is already registered.");

            _commands[command.Id] = command;
        }

        /// <inheritdoc/>
        public bool UnregisterCommand(string commandId)
        {
            if (commandId == null)
                throw new ArgumentNullException(nameof(commandId));
            if (string.IsNullOrWhiteSpace(commandId))
                throw new ArgumentException("Command ID cannot be empty or whitespace.", nameof(commandId));

            return _commands.Remove(commandId);
        }

        /// <inheritdoc/>
        public PhoneCommand? GetCommand(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            return _commands.TryGetValue(commandId, out var command) ? command : null;
        }

        /// <inheritdoc/>
        public IEnumerable<PhoneCommand> GetRegisteredCommands()
        {
            return _commands.Values.ToList();
        }

        /// <inheritdoc/>
        public IEnumerable<PhoneCommand> GetEnabledCommands()
        {
            return _commands.Values.Where(c => c.IsEnabled).ToList();
        }

        /// <inheritdoc/>
        public void ExecuteCommand(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return;

            var command = GetCommand(commandId);
            if (command == null || !command.IsEnabled)
                return;

            _commandHandler.HandleCommand(command);
        }

        /// <inheritdoc/>
        public bool IsCommandRegistered(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
                return false;

            return _commands.ContainsKey(commandId);
        }

        /// <inheritdoc/>
        public void SetCommandEnabled(string commandId, bool enabled)
        {
            var command = GetCommand(commandId);
            if (command != null)
            {
                command.IsEnabled = enabled;
            }
        }

        /// <inheritdoc/>
        public void RegisterAllFactionCommands()
        {
            // Status command - view faction status
            RegisterCommand(new PhoneCommand(
                "faction_status",
                "Faction Status",
                "View your faction's current status")
            { Category = "Faction" });

            // Territory command - view territory info
            RegisterCommand(new PhoneCommand(
                "faction_territory",
                "Territory Info",
                "View information about your territories")
            { Category = "Faction" });

            // Resources command - view resources
            RegisterCommand(new PhoneCommand(
                "faction_resources",
                "Resources",
                "View your faction's resources")
            { Category = "Faction" });

            // Orders command - issue orders
            RegisterCommand(new PhoneCommand(
                "faction_orders",
                "Issue Orders",
                "Issue attack or defense orders")
            { Category = "Faction" });
        }
    }
}
