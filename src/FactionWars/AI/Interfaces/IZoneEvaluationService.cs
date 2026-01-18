using System.Collections.Generic;
using FactionWars.AI.Models;
using FactionWars.Territory.Models;

namespace FactionWars.AI.Interfaces
{
    /// <summary>
    /// Interface for zone evaluation scoring service.
    /// Calculates how attractive a zone is for capture based on multiple factors:
    /// strategic value, traits, ownership, adjacency, and combat status.
    /// </summary>
    public interface IZoneEvaluationService
    {
        /// <summary>
        /// Weight applied to strategic value in score calculation (0-1).
        /// </summary>
        float StrategicValueWeight { get; }

        /// <summary>
        /// Weight applied to zone traits in score calculation (0-1).
        /// </summary>
        float TraitWeight { get; }

        /// <summary>
        /// Weight applied to adjacency bonus in score calculation (0-1).
        /// </summary>
        float AdjacencyWeight { get; }

        /// <summary>
        /// Bonus multiplier applied to neutral zones (easier to capture).
        /// </summary>
        float NeutralZoneBonus { get; }

        /// <summary>
        /// Evaluates a zone's attractiveness as a target for capture.
        /// </summary>
        /// <param name="zone">The zone to evaluate.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>A score between 0 and 1, where higher is more attractive.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zone or context is null.</exception>
        float EvaluateZone(Zone zone, AIContext context);

        /// <summary>
        /// Gets a detailed breakdown of a zone's score components.
        /// </summary>
        /// <param name="zone">The zone to evaluate.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>Dictionary with score component names and their values.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zone or context is null.</exception>
        IDictionary<string, float> GetZoneScoreBreakdown(Zone zone, AIContext context);

        /// <summary>
        /// Ranks a collection of zones by their attractiveness for capture.
        /// </summary>
        /// <param name="zones">The zones to rank.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>List of ranked zones with scores, ordered by attractiveness descending.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zones or context is null.</exception>
        IList<RankedZone> RankZonesByAttractiveness(IEnumerable<Zone> zones, AIContext context);

        /// <summary>
        /// Gets the best attack target from a collection of zones.
        /// </summary>
        /// <param name="zones">The zones to consider.</param>
        /// <param name="context">The current AI context.</param>
        /// <returns>The best target zone, or null if no valid targets exist.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if zones or context is null.</exception>
        Zone? GetBestAttackTarget(IEnumerable<Zone> zones, AIContext context);

        /// <summary>
        /// Calculates the score contribution from zone traits.
        /// </summary>
        /// <param name="traits">The zone traits to score.</param>
        /// <returns>A score between 0 and 1 representing trait value.</returns>
        float CalculateTraitScore(ZoneTrait traits);
    }

    /// <summary>
    /// Represents a zone with its evaluation score for ranking purposes.
    /// </summary>
    public class RankedZone
    {
        /// <summary>
        /// The evaluated zone.
        /// </summary>
        public Zone Zone { get; }

        /// <summary>
        /// The calculated attractiveness score (0-1).
        /// </summary>
        public float Score { get; }

        /// <summary>
        /// Creates a new ranked zone entry.
        /// </summary>
        /// <param name="zone">The zone.</param>
        /// <param name="score">The calculated score.</param>
        public RankedZone(Zone zone, float score)
        {
            Zone = zone;
            Score = score;
        }
    }
}
