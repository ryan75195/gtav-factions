using FactionWars.Core.Models;
using FactionWars.Persistence.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public interface IFollowerManager
    {
        FollowerRecruitResult RecruitFollower(string factionId, DefenderRole tier);

        bool DismissFollower(Guid followerId);

        void RestoreFollowers(string factionId, IEnumerable<SavedFollowerState> followers, int vehicleHandle);
    }
}
