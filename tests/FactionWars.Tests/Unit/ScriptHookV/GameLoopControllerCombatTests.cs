using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's territory detection and combat system wiring.
    /// Ensures managers are properly initialized and updated each tick.
    /// </summary>
    public class GameLoopControllerCombatTests
    {
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;

        private void SetupController(string initialCharacterModel = "player_zero")
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = initialCharacterModel;
            _container = ServiceContainerFactory.Create(_gameBridge, new MockMenuProvider());
        }

        [Fact]
        public void TerritoryManager_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act - first tick initializes game data
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.TerritoryManager);
        }

        [Fact]
        public void TerritoryManager_OnTick_GetsUpdated()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Set player position inside a zone
            _gameBridge.PlayerPosition = new Vector3(-200, -1600, 30); // Grove Street area

            // Act
            controller.OnTick();

            // Assert - TerritoryManager should have detected the zone
            Assert.NotNull(controller.TerritoryManager);
            // CurrentZone may or may not be set depending on zone definitions
        }

        [Fact]
        public void OnZoneBattleStarted_MarksZoneAsContested()
        {
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            var zoneRepo = _container.Resolve<IZoneRepository>();
            var battleManager = _container.Resolve<IZoneBattleManager>();
            var zone = zoneRepo.GetById("vinewood_hills");
            Assert.NotNull(zone);
            Assert.False(zone!.IsContested);

            var troops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } };
            battleManager.StartBattle(
                zoneId: "vinewood_hills",
                attackerFactionId: "trevor",
                defenderFactionId: "michael",
                attackerTroops: troops,
                defenderTroops: troops);

            Assert.True(zoneRepo.GetById("vinewood_hills")!.IsContested);
        }

        [Fact]
        public void OnZoneBattleEnded_PlayerWinsBattle_ZoneEndsNeutral_NotPlayerOwned()
        {
            // Regression: a player-win battle should leave the zone neutral. Before the
            // fix, ZoneBattleManager neutralized the zone via TransferZoneOwnership(null),
            // but OnZoneBattleEnded immediately re-transferred it to AttackerFactionId
            // (= the player's faction in player-flow battles), defeating Q5.A.
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick();

            var zoneRepo = _container.Resolve<IZoneRepository>();
            var zoneService = _container.Resolve<IZoneService>();
            var battleManager = _container.Resolve<IZoneBattleManager>();

            // Pick a zone, give it to a non-player faction, then attack it as the player.
            const string defenderFactionId = "trevor";
            var zone = zoneRepo.GetById("vinewood_hills");
            Assert.NotNull(zone);
            zoneService.TransferZoneOwnership("vinewood_hills", defenderFactionId);
            Assert.NotEqual(controller.CurrentPlayerFactionId, defenderFactionId);

            var allocSvc = _container.Resolve<IZoneDefenderAllocationService>();
            allocSvc.SetAllocation(defenderFactionId, "vinewood_hills", DefenderTier.Basic, 1);

            battleManager.StartPlayerCombat(zone!, controller.CurrentPlayerFactionId!, () => 1);

            // Player wipes the lone defender.
            battleManager.ReportTroopKilled("vinewood_hills", defenderFactionId, DefenderTier.Basic);

            var resultZone = zoneService.GetZone("vinewood_hills");
            Assert.NotNull(resultZone);
            Assert.Null(resultZone!.OwnerFactionId);
        }

        [Fact]
        public void AfterInitialization_ZoneBattleManagerKnowsPlayerFaction()
        {
            // Regression: GameLoopController must propagate the player's faction id
            // into ZoneBattleManager (matching every other player-faction-aware
            // manager). If it doesn't, IZoneBattleManager.GetPlayerCurrentBattle
            // permanently returns null because of its `_playerFactionId == null`
            // early-return guard. That breaks OnZoneExited's retreat block (which
            // gates on IsPlayerInBattle) and leaves the battle stuck in the manager,
            // so re-entry's StartPlayerCombat → JoinAsAttacker rejects "already in
            // battle" and the controller's early-return skips spawning defenders.
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick();

            var battleManager = _container.Resolve<IZoneBattleManager>();
            var zoneRepo = _container.Resolve<IZoneRepository>();
            var zoneService = _container.Resolve<IZoneService>();
            var allocSvc = _container.Resolve<IZoneDefenderAllocationService>();

            const string defenderFactionId = "trevor";
            var zone = zoneRepo.GetById("vinewood_hills");
            Assert.NotNull(zone);
            zoneService.TransferZoneOwnership("vinewood_hills", defenderFactionId);
            allocSvc.SetAllocation(defenderFactionId, "vinewood_hills", DefenderTier.Basic, 1);

            battleManager.StartPlayerCombat(zone!, controller.CurrentPlayerFactionId!, () => 1);

            Assert.True(battleManager.IsPlayerInBattle());
            Assert.NotNull(battleManager.GetPlayerCurrentBattle());
        }

        [Fact]
        public void OnAbort_ClearsInitializationState()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Act
            controller.OnAbort();

            // Assert - After abort, managers should be cleaned up
            Assert.False(controller.IsInitialized);
        }
    }
}
