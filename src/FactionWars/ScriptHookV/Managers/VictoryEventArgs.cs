using System;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Event arguments for victory events.
    /// </summary>
    public class VictoryEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the faction that achieved victory.
        /// </summary>
        public string WinningFactionId { get; }

        /// <summary>
        /// The display name of the faction that achieved victory.
        /// </summary>
        public string WinningFactionName { get; }

        /// <summary>
        /// Creates new victory event arguments.
        /// </summary>
        /// <param name="winningFactionId">The winning faction's ID.</param>
        /// <param name="winningFactionName">The winning faction's display name.</param>
        public VictoryEventArgs(string winningFactionId, string winningFactionName)
        {
            WinningFactionId = winningFactionId;
            WinningFactionName = winningFactionName;
        }
    }
}
