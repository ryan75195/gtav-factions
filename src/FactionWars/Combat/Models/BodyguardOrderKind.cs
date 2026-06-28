namespace FactionWars.Combat.Models
{
    /// <summary>
    /// What a single bodyguard should do this tick, independent of any native call.
    /// </summary>
    public enum BodyguardOrderKind
    {
        FollowPlayer,
        HoldAtPoint,
        SeekInRadius,
        AttackTarget,
        AdvanceOnTarget
    }
}
