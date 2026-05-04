using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.UI.Models;
using System;
using System.Linq;

namespace FactionWars.ScriptHookV.UI
{
    public partial class ArmyMenuController
    {
        private void ShowFollowerListMenu()
        {
            var factionId = _playerContext.CurrentFactionId;
            var followers = factionId != null
                ? _followerService.GetFollowers(factionId)
                : Array.Empty<Follower>();

            var menu = new MenuDefinition(FollowerListMenuId, "Followers", "Manage your bodyguards");

            if (followers.Count == 0)
            {
                var noFollowersItem = new MenuItem(
                    NoFollowersItemId,
                    "No followers",
                    "Recruit followers from the Army menu");
                noFollowersItem.IsEnabled = false;
                menu.AddItem(noFollowersItem);
            }
            else
            {
                foreach (var follower in followers)
                {
                    var followerItem = new MenuItem(
                        $"follower_{follower.Id}",
                        $"{follower.Tier} Follower",
                        $"Recruited {FormatServiceTime(follower.GetServiceTime())} ago");
                    menu.AddItem(followerItem);
                }
            }

            // Back navigation
            var backItem = new MenuItem(
                FollowerListBackItemId,
                "Back",
                "Return to army menu");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Shows the follower detail menu.
        /// </summary>
        private void ShowFollowerDetailMenu(Guid followerId)
        {
            _selectedFollowerId = followerId;
            var follower = _followerService.GetFollowerById(followerId);

            if (follower == null)
            {
                ShowFollowerListMenu();
                return;
            }

            var menu = new MenuDefinition(FollowerDetailMenuId, $"{follower.Tier} Follower", "Follower details");

            // Follower info (disabled display items)
            var tierItem = new MenuItem(
                "tier_info",
                $"Tier: {follower.Tier}",
                "Combat tier determines equipment and effectiveness");
            tierItem.IsEnabled = false;
            menu.AddItem(tierItem);

            var serviceItem = new MenuItem(
                "service_info",
                $"Service time: {FormatServiceTime(follower.GetServiceTime())}",
                "Time since recruitment");
            serviceItem.IsEnabled = false;
            menu.AddItem(serviceItem);

            // Dismiss option
            var dismissItem = new MenuItem(
                DismissFollowerItemId,
                "Dismiss Follower",
                "Send this follower away (no refund)");
            menu.AddItem(dismissItem);

            // Back navigation
            var backItem = new MenuItem(
                FollowerDetailBackItemId,
                "Back",
                "Return to follower list");
            menu.AddItem(backItem);

            _menuProvider.ShowMenu(menu);
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
    }
}
