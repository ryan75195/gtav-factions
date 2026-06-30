using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// Tests for BattleAttackerManager, which manages enemy attackers
    /// that spawn when the player enters a zone under attack.
    /// </summary>
    public class BattleAttackerManagerTests
    {
        private MockGameBridge _gameBridge = null!;
        private Mock<IZoneBattleManager> _zoneBattleManagerMock = null!;
        private Mock<IPedSpawningService> _pedSpawningServiceMock = null!;
        private Mock<IPedDespawnService> _pedDespawnServiceMock = null!;
        private Mock<IDefenderRoleService> _defenderRoleServiceMock = null!;
        private Mock<IPedBlipService> _pedBlipServiceMock = null!;
        private Mock<IZoneService> _zoneServiceMock = null!;
        private Mock<IFactionService> _factionServiceMock = null!;
        private BattleAttackerManager _manager = null!;

        private const string PlayerFactionId = "michael";
        private const string EnemyFactionId = "ballas";
        private const string TestZoneId = "zone_1";

        private void SetupManager()
        {
            _gameBridge = new MockGameBridge();
            _zoneBattleManagerMock = new Mock<IZoneBattleManager>();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _pedDespawnServiceMock = new Mock<IPedDespawnService>();
            _defenderRoleServiceMock = new Mock<IDefenderRoleService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _factionServiceMock = new Mock<IFactionService>();

            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, Vector3, string, string?>((model, position, factionId, zoneId) =>
                {
                    var handle = _gameBridge.CreatePed(model, position);
                    return new PedHandle(handle, factionId, position, model, zoneId);
                });

            _defenderRoleServiceMock.Setup(d => d.GetRoleConfig(It.IsAny<DefenderRole>()))
                .Returns(new DefenderRoleConfig(DefenderRole.Grunt, 200, 100, 0, "weapon_pistol", 0.5f, 1.0f));

            _pedBlipServiceMock.Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            _manager = new BattleAttackerManager(
                _gameBridge,
                _zoneBattleManagerMock.Object,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _defenderRoleServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                _factionServiceMock.Object,
                PlayerFactionId,
                CombatantStatsProviderFactory.Create(new CombatantsConfig()));
        }

        [Fact]
        public void GetHostilePedHandles_ReturnsSpawnedAttackerHandles()
        {
            SetupManager();
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = PlayerFactionId;
            var battle = new ZoneBattle(
                EnemyFactionId,
                PlayerFactionId,
                TestZoneId,
                new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 2 } },
                new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 1 } });
            _zoneBattleManagerMock.Setup(b => b.GetBattleForZone(TestZoneId)).Returns(battle);

            _manager.OnPlayerZoneEntered(zone);

            var handles = _manager.GetHostilePedHandles();
            Assert.NotEmpty(handles);
        }
    }
}
