using FactionWars.Combat.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>Role → engage range table. Values are starting points, tunable here.</summary>
    public sealed class EngageRangeProvider : IEngageRangeProvider
    {
        private const float Fallback = 30f;

        public float For(DefenderRole role)
        {
            switch (role)
            {
                case DefenderRole.Sniper: return 80f;
                case DefenderRole.Rocketeer: return 45f;
                case DefenderRole.Rifleman: return 45f;
                case DefenderRole.Gunner: return 35f;
                case DefenderRole.Grunt: return 18f;
                default: return Fallback;
            }
        }
    }
}
