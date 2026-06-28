using System;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.UI
{
    public partial class SquadMenuController
    {
        private void HandleFollowerDetailSelection(string itemId)
        {
            switch (itemId)
            {
                case FollowerDetailBackItemId:
                    ShowFollowerListMenu();
                    break;

                case DismissFollowerItemId:
                    if (_selectedFollowerId.HasValue)
                    {
                        if (_followerManager != null)
                        {
                            _followerManager.DismissFollower(_selectedFollowerId.Value);
                        }
                        else
                        {
                            _followerService.DismissFollower(_selectedFollowerId.Value);
                        }

                        _selectedFollowerId = null;
                        ShowFollowerListMenu();
                    }
                    break;
            }
        }

        private void RecruitFollower(string factionId, DefenderRole tier)
        {
            if (_followerManager != null)
            {
                var result = _followerManager.RecruitFollower(factionId, tier);

                if (result.Success)
                {
                    _gameBridge?.ShowNotification($"~g~Recruited {tier} bodyguard");
                }
                else
                {
                    var errorMsg = result.FailureReason switch
                    {
                        FollowerRecruitFailureReason.MaxFollowersReached => "Max squad size reached",
                        FollowerRecruitFailureReason.InsufficientFunds => "Insufficient funds",
                        FollowerRecruitFailureReason.InvalidFaction => "Invalid faction",
                        FollowerRecruitFailureReason.SpawnFailed => "Spawn failed",
                        _ => "Unknown error"
                    };
                    _gameBridge?.ShowNotification($"~r~Failed: {errorMsg}");
                }
            }
            else
            {
                // Fallback to domain-only recruitment
                var purchaseResult = _purchaseService.PurchaseTroops(factionId, tier, 1);
                if (purchaseResult.Success)
                {
                    _followerService.Recruit(factionId, tier);
                }
            }
            ShowSquadMenu();
        }

        private static string FormatServiceTime(TimeSpan time)
        {
            if (time.TotalDays >= 1)
            {
                return $"{(int)time.TotalDays}d";
            }
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours}h";
            }
            if (time.TotalMinutes >= 1)
            {
                return $"{(int)time.TotalMinutes}m";
            }
            return $"{(int)time.TotalSeconds}s";
        }
    }
}
