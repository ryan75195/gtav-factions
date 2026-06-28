namespace FactionWars.Telemetry.Models
{
    /// <summary>
    /// Which combat system owns a tracked ped. Determines friend/foe for the sampler's
    /// nearest-hostile approximation (Follower/FriendlyDefender are friendly to the player).
    /// </summary>
    public enum CombatantKind
    {
        Follower,
        FriendlyDefender,
        EnemyDefender,
        BattleAttacker
    }
}
