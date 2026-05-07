using FactionWars.Core.Models;
using FactionWars.Persistence.Models;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public interface IFollowerManager
    {
        FollowerRecruitResult RecruitFollower(string factionId, DefenderTier tier);

        void RestoreFollowers(string factionId, IEnumerable<SavedFollowerState> followers, int vehicleHandle);
    }
}
