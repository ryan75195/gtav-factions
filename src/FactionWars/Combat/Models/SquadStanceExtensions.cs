namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Cycle helper for <see cref="SquadStance"/>.
    /// </summary>
    public static class SquadStanceExtensions
    {
        public static SquadStance Next(this SquadStance stance)
        {
            switch (stance)
            {
                case SquadStance.Escort: return SquadStance.HoldArea;
                case SquadStance.HoldArea: return SquadStance.SearchAndDestroy;
                default: return SquadStance.Escort;
            }
        }
    }
}
