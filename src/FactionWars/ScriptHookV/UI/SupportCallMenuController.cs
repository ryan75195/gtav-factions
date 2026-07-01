using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Managers.Interfaces;
using FactionWars.ScriptHookV.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using System;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Controller for the Support-call submenu. Lets the player call a purchased support squad
    /// into their current zone.
    /// </summary>
    public class SupportCallMenuController
    {
        /// <summary>
        /// Menu ID for the support-call menu.
        /// </summary>
        public const string MenuId = "support_call_menu";

        /// <summary>
        /// Item ID for owned-count display.
        /// </summary>
        public const string OwnedDisplayItemId = "owned_display";

        /// <summary>
        /// Item ID for the call-support-squad option.
        /// </summary>
        public const string CallItemId = "call_support_squad";

        /// <summary>
        /// Item ID for back button.
        /// </summary>
        public const string BackItemId = "back";

        private readonly IMenuProvider _menuProvider;
        private readonly ISupportPackageService _supportPackageService;
        private readonly ISupportSquadManager _supportSquadManager;
        private readonly ITerritoryEvents _territory;
        private readonly IPlayerContext _playerContext;
        private readonly IGameBridge _gameBridge;

        /// <summary>
        /// Event raised when the user selects the back option.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Creates a new SupportCallMenuController with the specified dependencies.
        /// </summary>
        public SupportCallMenuController(SupportCallMenuControllerDependencies dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _menuProvider = dependencies.MenuProvider ?? throw new ArgumentNullException(nameof(dependencies.MenuProvider));
            _supportPackageService = dependencies.SupportPackageService ?? throw new ArgumentNullException(nameof(dependencies.SupportPackageService));
            _supportSquadManager = dependencies.SupportSquadManager ?? throw new ArgumentNullException(nameof(dependencies.SupportSquadManager));
            _territory = dependencies.Territory ?? throw new ArgumentNullException(nameof(dependencies.Territory));
            _playerContext = dependencies.PlayerContext ?? throw new ArgumentNullException(nameof(dependencies.PlayerContext));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));

            _menuProvider.ItemSelected += OnItemSelected;
        }

        public SupportCallMenuController(params object?[] dependencies)
            : this(new SupportCallMenuControllerDependencies
            {
                MenuProvider = (IMenuProvider?)dependencies[0],
                SupportPackageService = (ISupportPackageService?)dependencies[1],
                SupportSquadManager = (ISupportSquadManager?)dependencies[2],
                Territory = (ITerritoryEvents?)dependencies[3],
                PlayerContext = (IPlayerContext?)dependencies[4],
                GameBridge = (IGameBridge?)dependencies[5]
            })
        {
        }

        /// <summary>
        /// Shows the support-call menu.
        /// </summary>
        public void Show()
        {
            var menu = new MenuDefinition(MenuId, "Support", "Call Reinforcements");
            var factionId = _playerContext.CurrentFactionId;
            var owned = factionId != null ? _supportPackageService.GetOwnedCount(factionId) : 0;

            AddOwnedItem(menu, owned);
            AddCallItem(menu, owned);

            menu.AddItem(new MenuItem(BackItemId, "Back", "Return to squad hub"));

            _menuProvider.ShowMenu(menu);
        }

        private static void AddOwnedItem(MenuDefinition menu, int owned)
        {
            var ownedItem = new MenuItem(OwnedDisplayItemId, $"Support Squads owned: {owned}", "Squads ready to call in");
            ownedItem.IsEnabled = false;
            menu.AddItem(ownedItem);
        }

        private void AddCallItem(MenuDefinition menu, int owned)
        {
            var callItem = new MenuItem(
                CallItemId,
                "Call Support Squad",
                "Bring the FBI SUV of 8 allies into your current zone");
            callItem.IsEnabled = owned > 0 && !_supportSquadManager.HasActiveSquad && _territory.CurrentZone != null;
            menu.AddItem(callItem);
        }

        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != MenuId) return;

            if (e.ItemId == CallItemId)
            {
                CallSupportSquad();
                return;
            }

            if (e.ItemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CallSupportSquad()
        {
            var factionId = _playerContext.CurrentFactionId;
            var zone = _territory.CurrentZone;

            // Spawn first, consume the owned package only once the squad actually exists - a
            // failed spawn (e.g. CreateVehicle returning -1) must never burn a paid-for package.
            if (factionId != null && zone != null && !_supportSquadManager.HasActiveSquad
                && _supportPackageService.GetOwnedCount(factionId) > 0
                && _supportSquadManager.CallSupportSquad(zone))
            {
                // Safe to ignore the return: GetOwnedCount > 0 was checked above and nothing mutates the owned count in between, so this consume always succeeds.
                _supportPackageService.TryConsume(factionId);
                _gameBridge.ShowNotification("~g~Support squad inbound!");
            }

            Show();
        }
    }
}
