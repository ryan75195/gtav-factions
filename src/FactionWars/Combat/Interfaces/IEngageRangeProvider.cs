using FactionWars.Core.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Maps a defender role to the distance (metres) at which it should stop advancing
    /// and engage. Tunable in one place.</summary>
    public interface IEngageRangeProvider
    {
        float For(DefenderRole role);
    }
}
