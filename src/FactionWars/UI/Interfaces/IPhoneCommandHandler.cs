using FactionWars.UI.Models;

namespace FactionWars.UI.Interfaces
{
    /// <summary>
    /// Handles the execution of phone commands.
    /// Abstraction allows for different implementations (e.g., real game integration vs mock for testing).
    /// </summary>
    public interface IPhoneCommandHandler
    {
        /// <summary>
        /// Handles the execution of a phone command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        void HandleCommand(PhoneCommand command);
    }
}
