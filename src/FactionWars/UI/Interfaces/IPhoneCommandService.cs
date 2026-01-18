using FactionWars.UI.Models;
using System.Collections.Generic;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Service for managing phone commands.
    /// Provides registration, retrieval, and execution of phone commands.
    /// </summary>
    public interface IPhoneCommandService
    {
        /// <summary>
        /// Registers a new phone command.
        /// </summary>
        /// <param name="command">The command to register.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if command is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if a command with the same ID is already registered.</exception>
        void RegisterCommand(PhoneCommand command);

        /// <summary>
        /// Unregisters a phone command by its ID.
        /// </summary>
        /// <param name="commandId">The ID of the command to unregister.</param>
        /// <returns>True if the command was found and removed, false otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if commandId is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if commandId is empty or whitespace.</exception>
        bool UnregisterCommand(string commandId);

        /// <summary>
        /// Gets a registered command by its ID.
        /// </summary>
        /// <param name="commandId">The ID of the command to retrieve.</param>
        /// <returns>The command if found, null otherwise.</returns>
        PhoneCommand? GetCommand(string commandId);

        /// <summary>
        /// Gets all registered commands.
        /// </summary>
        /// <returns>All registered commands.</returns>
        IEnumerable<PhoneCommand> GetRegisteredCommands();

        /// <summary>
        /// Gets only enabled commands.
        /// </summary>
        /// <returns>All enabled commands.</returns>
        IEnumerable<PhoneCommand> GetEnabledCommands();

        /// <summary>
        /// Executes a command by its ID.
        /// </summary>
        /// <param name="commandId">The ID of the command to execute.</param>
        void ExecuteCommand(string commandId);

        /// <summary>
        /// Checks if a command is registered.
        /// </summary>
        /// <param name="commandId">The ID of the command to check.</param>
        /// <returns>True if registered, false otherwise.</returns>
        bool IsCommandRegistered(string commandId);

        /// <summary>
        /// Enables or disables a command.
        /// </summary>
        /// <param name="commandId">The ID of the command.</param>
        /// <param name="enabled">Whether to enable or disable the command.</param>
        void SetCommandEnabled(string commandId, bool enabled);

        /// <summary>
        /// Registers all standard faction commands.
        /// </summary>
        void RegisterAllFactionCommands();
    }
}
