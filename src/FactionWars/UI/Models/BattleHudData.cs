namespace FactionWars.UI.Models
{
    /// <summary>
    /// Data model for the battle HUD display showing active AI battles.
    /// </summary>
    public class BattleHudData
    {
        /// <summary>
        /// The zone name where the battle is occurring.
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// The name of the attacking faction.
        /// </summary>
        public string AttackerName { get; }

        /// <summary>
        /// Total troop count for the attacker.
        /// </summary>
        public int AttackerTroops { get; }

        /// <summary>
        /// The name of the defending faction.
        /// </summary>
        public string DefenderName { get; }

        /// <summary>
        /// Total troop count for the defender.
        /// </summary>
        public int DefenderTroops { get; }

        /// <summary>
        /// Current battle index (1-based) for display.
        /// </summary>
        public int CurrentBattleIndex { get; }

        /// <summary>
        /// Total number of active battles.
        /// </summary>
        public int TotalBattles { get; }

        /// <summary>
        /// Gets whether there are multiple battles active.
        /// </summary>
        public bool HasMultipleBattles => TotalBattles > 1;

        public BattleHudData(
            string zoneName,
            string attackerName,
            int attackerTroops,
            string defenderName,
            int defenderTroops,
            int currentBattleIndex,
            int totalBattles)
        {
            ZoneName = zoneName;
            AttackerName = attackerName;
            AttackerTroops = attackerTroops;
            DefenderName = defenderName;
            DefenderTroops = defenderTroops;
            CurrentBattleIndex = currentBattleIndex;
            TotalBattles = totalBattles;
        }
    }
}
