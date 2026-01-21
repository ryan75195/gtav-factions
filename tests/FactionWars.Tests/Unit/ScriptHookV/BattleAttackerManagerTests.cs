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
        private readonly Mock<IActiveBattleManager> _battleManagerMock;
        private readonly Mock<IPedSpawningService> _pedSpawningMock;
        private readonly Mock<IDefenderTierService> _tierServiceMock;
        private readonly Mock<IPedBlipService> _blipServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;

        public BattleAttackerManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _battleManagerMock = new Mock<IActiveBattleManager>();
            _pedSpawningMock = new Mock<IPedSpawningService>();
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
            var battle = new ActiveBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, 60f, 6f);

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
            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns((ActiveBattle?)null);

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
            var battle = new ActiveBattle("player", "enemy", "downtown", attackerTroops, defenderTroops, 60f, 6f);

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert - should NOT spawn because player is attacker, not defender
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        private BattleAttackerManager CreateManager(string playerFactionId)
        {
            return new BattleAttackerManager(
                _gameBridgeMock.Object,
                _battleManagerMock.Object,
                _pedSpawningMock.Object,
                _tierServiceMock.Object,
                _blipServiceMock.Object,
                _zoneServiceMock.Object,
                playerFactionId);
        }
    }
}
