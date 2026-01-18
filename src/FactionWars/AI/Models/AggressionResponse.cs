using System;
using System.Collections.Generic;

namespace FactionWars.AI.Models
{
    /// <summary>
    /// Represents the AI faction's response to aggression.
    /// Contains the type of response and specific decisions to execute.
    /// </summary>
    public class AggressionResponse
    {
        /// <summary>
        /// The type of response being taken.
        /// </summary>
        public AggressionResponseType ResponseType { get; }

        /// <summary>
        /// The decisions to execute as part of this response.
        /// </summary>
        public IReadOnlyList<AIDecision> Decisions { get; }

        /// <summary>
        /// The current threat level that triggered this response (0-1).
        /// </summary>
        public float ThreatLevel { get; }

        /// <summary>
        /// Creates a new aggression response.
        /// </summary>
        /// <param name="responseType">The type of response.</param>
        /// <param name="decisions">The decisions to execute.</param>
        /// <param name="threatLevel">The threat level that triggered the response.</param>
        /// <exception cref="ArgumentNullException">Thrown if decisions is null.</exception>
        public AggressionResponse(AggressionResponseType responseType, IReadOnlyList<AIDecision> decisions, float threatLevel)
        {
            if (decisions == null)
                throw new ArgumentNullException(nameof(decisions));

            ResponseType = responseType;
            Decisions = decisions;
            ThreatLevel = Math.Max(0f, Math.Min(1f, threatLevel));
        }

        /// <summary>
        /// Creates a "no response" aggression response.
        /// </summary>
        public static AggressionResponse NoResponse => new AggressionResponse(
            AggressionResponseType.None,
            Array.Empty<AIDecision>(),
            0f);

        public override string ToString()
        {
            return $"AggressionResponse: {ResponseType} with {Decisions.Count} decisions (threat: {ThreatLevel:P0})";
        }
    }
}
