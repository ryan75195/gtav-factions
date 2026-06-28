using FactionWars.Core.Models;

namespace FactionWars.Telemetry.Models
{
    /// <summary>
    /// A ped the behavior sampler should observe: its handle plus the kind/role context the
    /// owning manager already holds. Pure data; the sampler reads live state via the game bridge.
    /// </summary>
    public readonly struct TrackedCombatant
    {
        public TrackedCombatant(int handle, CombatantKind kind, DefenderRole role)
        {
            Handle = handle;
            Kind = kind;
            Role = role;
        }

        public int Handle { get; }

        public CombatantKind Kind { get; }

        public DefenderRole Role { get; }
    }
}
