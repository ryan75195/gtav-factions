namespace FactionWars.Combat.Models
{
    /// <summary>
    /// The kind of action a ped should be performing this tick. The reconciler dedups on
    /// <see cref="PedIntent.Kind"/> + <see cref="PedIntent.Discriminator"/> so an unchanged
    /// intent is never re-issued to the engine (avoiding per-frame task thrash).
    /// </summary>
    public enum PedIntentKind
    {
        Idle,
        FollowPlayer,
        GuardArea,
        CombatTarget,
        AdvanceOnTarget,
        RegroupOnPlayer,
        SeekHatedTargets,
        WanderArea,
        GoToCoord,
        LeaveVehicle
    }
}
