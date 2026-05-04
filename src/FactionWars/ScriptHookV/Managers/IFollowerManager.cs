using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public interface IFollowerManager
    {
        FollowerRecruitResult RecruitFollower(string factionId, DefenderTier tier);
    }
}
