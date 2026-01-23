using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class BattleAttackerManagerTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IZoneBattleManager> _battleManagerMock;
        private readonly Mock<IPedSpawningService> _pedSpawningMock;
        private readonly Mock<IPedDespawnService> _pedDespawnMock;
        private readonly Mock<IDefenderTierService> _tierServiceMock;
        private readonly Mock<IPedBlipService> _blipServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;

        public BattleAttackerManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _battleManagerMock = new Mock<IZoneBattleManager>();
            _pedSpawningMock = new Mock<IPedSpawningService>();
            _pedDespawnMock = new Mock<IPedDespawnService>();
            _tierServiceMock = new Mock<IDefenderTierService>();
            _blipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();

            _tierServiceMock.Setup(t => t.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 100, 100, 0, "weapon_pistol", 50, 1.0f));
        }

        [Fact]
        public void OnPlayerZoneEntered_WithActiveBattle_AsDefender_ShouldSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new PedHandle(100));
            _gameBridgeMock.Setup(g => g.GetGroundZ(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>())).Returns(0f);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert - should spawn up to MaxSpawnedAttackers (or total attackers, whichever is less)
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"),
                Times.Exactly(5));
        }

        [Fact]
        public void OnPlayerZoneEntered_NoBattle_ShouldNotSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns((ZoneBattle?)null);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void OnPlayerZoneEntered_PlayerIsAttacker_ShouldNotSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "enemy" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ZoneBattle("player", "enemy", "downtown", attackerTroops, defenderTroops, "player");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert - should NOT spawn because player is attacker, not defender
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void Update_WhenAttackerDiesAndBattleHasReserveTroops_ShouldSpawnReplacement()
        {
            // Arrange
            // Battle has 15 Basic attackers total, but only 12 are spawned (MaxSpawnedAttackers)
            // When one dies, a replacement should spawn from the remaining 3 reserves
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 15 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 10 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");
            battle.IsPlayerPresent = true;

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _zoneServiceMock.Setup(z => z.GetZone("downtown")).Returns(zone);

            // Set up mock to actually decrement battle troops when ReportTroopKilled is called
            _battleManagerMock.Setup(b => b.ReportTroopKilled("downtown", "enemy", It.IsAny<DefenderTier>()))
                .Callback<string, string, DefenderTier>((zoneId, factionId, tier) => battle.RemoveAttackerTroop(tier));

            // Spawn peds with unique handles (100-111)
            int pedHandle = 100;
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(() => new PedHandle(pedHandle++));
            _gameBridgeMock.Setup(g => g.GetSafeCoordForPed(It.IsAny<Vector3>())).Returns(new Vector3(0, 0, 0));

            var manager = CreateManager("player");
            manager.OnPlayerZoneEntered(zone);

            // 12 attackers should be spawned (MaxSpawnedAttackers)
            Assert.Equal(12, manager.GetSpawnedAttackerCount("downtown"));

            // Simulate one attacker (ped 100) dying
            _gameBridgeMock.Setup(g => g.IsPedAlive(100)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.Is<int>(h => h > 100 && h <= 111))).Returns(true);

            // Reset invocation count to track replacement spawn
            _pedSpawningMock.Invocations.Clear();

            // Act - Update should detect death and spawn replacement
            manager.Update();

            // Assert - replacement should spawn (one new SpawnPed call)
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"),
                Times.AtLeastOnce,
                "Replacement attacker should spawn when battle has reserve troops");

            // Spawned count should remain at 12 (11 alive + 1 replacement)
            // Note: Actually after death processing it will be 11 (dead removed) + 1 (replacement) = 12
            Assert.Equal(12, manager.GetSpawnedAttackerCount("downtown"));
        }

        [Fact]
        public void Update_WhenAttackerDiesAndBattleHasExactlyMaxTroops_ShouldNotSpawnReplacement()
        {
            // Arrange
            // When battle has exactly 12 Basic attackers (= MaxSpawnedAttackers) and all are spawned,
            // there's no reserve. When one dies, no replacement should spawn.
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 12 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 10 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");
            battle.IsPlayerPresent = true;

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _zoneServiceMock.Setup(z => z.GetZone("downtown")).Returns(zone);

            // Set up mock to actually decrement battle troops when ReportTroopKilled is called
            _battleManagerMock.Setup(b => b.ReportTroopKilled("downtown", "enemy", It.IsAny<DefenderTier>()))
                .Callback<string, string, DefenderTier>((zoneId, factionId, tier) => battle.RemoveAttackerTroop(tier));

            // Spawn peds with unique handles (100-111)
            int pedHandle = 100;
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(() => new PedHandle(pedHandle++));
            _gameBridgeMock.Setup(g => g.GetSafeCoordForPed(It.IsAny<Vector3>())).Returns(new Vector3(0, 0, 0));

            var manager = CreateManager("player");
            manager.OnPlayerZoneEntered(zone);

            // All 12 attackers should be spawned
            Assert.Equal(12, manager.GetSpawnedAttackerCount("downtown"));

            // Simulate one attacker (ped 100) dying
            _gameBridgeMock.Setup(g => g.IsPedAlive(100)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.Is<int>(h => h > 100 && h <= 111))).Returns(true);

            // Reset invocation count to track replacement spawn
            _pedSpawningMock.Invocations.Clear();

            // Act - Update should detect death
            manager.Update();

            // Assert - NO replacement should spawn when all troops were spawned and one died
            // The attacker is truly dead (no reserve), so count goes from 12 to 11
            Assert.Equal(11, manager.GetSpawnedAttackerCount("downtown"));

            // Verify NO new spawn attempt (because there's no reserve)
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"),
                Times.Never,
                "No replacement should spawn when battle has no reserve troops");
        }

        [Fact]
        public void OnPlayerZoneEntered_AfterExitAndReenter_WithBackgroundSimDepletion_ShouldSpawnAttackers()
        {
            // Arrange
            // This test reproduces the bug where:
            // 1. Player enters zone, 5 attackers spawn
            // 2. Player exits zone, attackers despawn
            // 3. Background simulation runs and depletes battle.AttackerTroops
            // 4. Player re-enters zone
            // BUG: No attackers spawn because battle.AttackerTroops is now 0
            // EXPECTED: Attackers should spawn because player never killed them

            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);

            int pedHandle = 100;
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(() => new PedHandle(pedHandle++));
            _gameBridgeMock.Setup(g => g.GetSafeCoordForPed(It.IsAny<Vector3>())).Returns(new Vector3(0, 0, 0));

            var manager = CreateManager("player");

            // Act 1: Player enters zone - attackers spawn
            manager.OnPlayerZoneEntered(zone);
            Assert.Equal(5, manager.GetSpawnedAttackerCount("downtown"));

            // Act 2: Player exits zone - attackers despawn
            manager.OnPlayerZoneExited(zone);
            Assert.Equal(0, manager.GetSpawnedAttackerCount("downtown"));

            // Simulate background battle depleting attacker troops (this happens when player is away)
            // The background simulation would call battle.RemoveAttackerTroop() repeatedly
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);

            // Now battle.AttackerTroops[Basic] == 0 due to background sim
            Assert.Equal(0, battle.TotalAttackerTroops);

            // Reset spawn tracking for re-entry
            _pedSpawningMock.Invocations.Clear();
            pedHandle = 200; // New handles for re-spawned attackers

            // Act 3: Player re-enters zone
            manager.OnPlayerZoneEntered(zone);

            // Assert: Attackers SHOULD spawn because:
            // - The previous physical attackers were only despawned, not killed
            // - The background simulation's troop depletion should be restored/ignored for spawning
            // - Player should face the same number of attackers they would have faced before exiting
            Assert.True(manager.GetSpawnedAttackerCount("downtown") > 0,
                "Attackers should spawn when re-entering zone even if background sim depleted troops. " +
                "The player never killed these attackers - they were only despawned on zone exit.");
        }

        private BattleAttackerManager CreateManager(string playerFactionId)
        {
            return new BattleAttackerManager(
                _gameBridgeMock.Object,
                _battleManagerMock.Object,
                _pedSpawningMock.Object,
                _pedDespawnMock.Object,
                _tierServiceMock.Object,
                _blipServiceMock.Object,
                _zoneServiceMock.Object,
                playerFactionId);
        }
    }
}
