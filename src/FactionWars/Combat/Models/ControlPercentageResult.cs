namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents the result of a control percentage calculation for a combat encounter.
    /// Contains the calculated percentages for both attacker and defender based on ped counts.
    /// </summary>
    public class ControlPercentageResult
    {
        /// <summary>
        /// The percentage of zone control held by the attacking faction (0-100).
        /// </summary>
        public float AttackerPercentage { get; }

        /// <summary>
        /// The percentage of zone control held by the defending faction (0-100).
        /// </summary>
        public float DefenderPercentage { get; }

        /// <summary>
        /// The total number of peds used in the calculation.
        /// </summary>
        public int TotalPeds { get; }

        /// <summary>
        /// Creates a new control percentage result.
        /// </summary>
        /// <param name="attackerPercentage">The attacker's control percentage.</param>
        /// <param name="defenderPercentage">The defender's control percentage.</param>
        /// <param name="totalPeds">The total number of peds.</param>
        public ControlPercentageResult(float attackerPercentage, float defenderPercentage, int totalPeds)
        {
            AttackerPercentage = attackerPercentage;
            DefenderPercentage = defenderPercentage;
            TotalPeds = totalPeds;
        }
    }
}
