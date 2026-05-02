namespace FactionWars.Combat.Models
{
    /// <summary>
    /// The role a participant plays in a <see cref="ZoneBattle"/>.
    /// A battle has exactly one Defender and one or more Attackers.
    /// </summary>
    public enum BattleRole
    {
        Defender,
        Attacker
    }
}
