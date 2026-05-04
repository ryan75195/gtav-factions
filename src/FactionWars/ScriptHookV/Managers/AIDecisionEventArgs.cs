using System;
using FactionWars.AI.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Event arguments for AI decision events.
    /// </summary>
    public class AIDecisionEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the faction that made the decision.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// The decision that was made.
        /// </summary>
        public AIDecision Decision { get; }

        /// <summary>
        /// Creates new AI decision event arguments.
        /// </summary>
        /// <param name="factionId">The faction ID.</param>
        /// <param name="decision">The decision made.</param>
        public AIDecisionEventArgs(string factionId, AIDecision decision)
        {
            FactionId = factionId;
            Decision = decision;
        }
    }
}
