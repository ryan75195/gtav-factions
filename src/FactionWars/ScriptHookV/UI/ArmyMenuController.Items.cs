using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV.UI
{
    public partial class ArmyMenuController
    {
        private void AddPurchaseItems(MenuDefinition menu, string? factionId)
        {
            var basicCost = _purchaseService.GetTroopCost(DefenderRole.Grunt);
            var mediumCost = _purchaseService.GetTroopCost(DefenderRole.Gunner);
            var heavyCost = _purchaseService.GetTroopCost(DefenderRole.Rifleman);

            var canPurchaseBasic = factionId != null && _purchaseService.CanAfford(DefenderRole.Grunt, 1);
            var canPurchaseMedium = factionId != null && _purchaseService.CanAfford(DefenderRole.Gunner, 1);
            var canPurchaseHeavy = factionId != null && _purchaseService.CanAfford(DefenderRole.Rifleman, 1);

            var purchaseBasicItem = new MenuItem(
                PurchaseBasicItemId,
                $"Buy Basic Troop (${basicCost})",
                "Pistol, no armor");
            purchaseBasicItem.IsEnabled = canPurchaseBasic;
            menu.AddItem(purchaseBasicItem);

            var purchaseMediumItem = new MenuItem(
                PurchaseMediumItemId,
                $"Buy Medium Troop (${mediumCost})",
                "SMG, light armor");
            purchaseMediumItem.IsEnabled = canPurchaseMedium;
            menu.AddItem(purchaseMediumItem);

            var purchaseHeavyItem = new MenuItem(
                PurchaseHeavyItemId,
                $"Buy Heavy Troop (${heavyCost})",
                "Carbine, full armor");
            purchaseHeavyItem.IsEnabled = canPurchaseHeavy;
            menu.AddItem(purchaseHeavyItem);
        }

        private void AddRecruitmentItems(MenuDefinition menu, string? factionId)
        {
            var basicCost = _purchaseService.GetTroopCost(DefenderRole.Grunt);
            var mediumCost = _purchaseService.GetTroopCost(DefenderRole.Gunner);
            var heavyCost = _purchaseService.GetTroopCost(DefenderRole.Rifleman);
            var followerCount = factionId != null ? _followerService.GetFollowerCount(factionId) : 0;
            var maxFollowers = _followerService.GetMaxFollowers();

            var followerSummaryItem = new MenuItem(
                FollowerSummaryItemId,
                $"Followers: {followerCount}/{maxFollowers}",
                "Bodyguards that fight with you");
            followerSummaryItem.IsEnabled = false;
            menu.AddItem(followerSummaryItem);

            var canRecruit = factionId != null && followerCount < maxFollowers;

            var recruitBasicItem = new MenuItem(
                RecruitBasicItemId,
                $"Recruit Basic Follower (${basicCost})",
                "Spawns a basic bodyguard");
            recruitBasicItem.IsEnabled = canRecruit && _purchaseService.CanAfford(DefenderRole.Grunt, 1);
            menu.AddItem(recruitBasicItem);

            var recruitMediumItem = new MenuItem(
                RecruitMediumItemId,
                $"Recruit Medium Follower (${mediumCost})",
                "Spawns a medium bodyguard");
            recruitMediumItem.IsEnabled = canRecruit && _purchaseService.CanAfford(DefenderRole.Gunner, 1);
            menu.AddItem(recruitMediumItem);

            var recruitHeavyItem = new MenuItem(
                RecruitHeavyItemId,
                $"Recruit Heavy Follower (${heavyCost})",
                "Spawns a heavy bodyguard");
            recruitHeavyItem.IsEnabled = canRecruit && _purchaseService.CanAfford(DefenderRole.Rifleman, 1);
            menu.AddItem(recruitHeavyItem);
        }

        private static void AddArmyNavigationItems(MenuDefinition menu)
        {
            var manageFollowersItem = new MenuItem(
                ManageFollowersItemId,
                "Manage Followers",
                "View and dismiss followers");
            menu.AddItem(manageFollowersItem);

            var backItem = new MenuItem(
                BackItemId,
                "Back",
                "Return to main menu");
            menu.AddItem(backItem);
        }

        /// <summary>
        /// Shows the follower list menu.
        /// </summary>
    }
}
