using FactionWars.Core.Models;
using System;

namespace FactionWars.ScriptHookV.Managers
{
    public interface IFollowerManager
    {
        FollowerRecruitResult RecruitFollower(string factionId, DefenderTier tier);

        bool DismissFollower(Guid followerId);
    }
}
