using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.ScriptHookV
{
    /// <summary>
    /// Integration tests verifying that entering an enemy zone triggers combat
    /// and capturing a zone works correctly through the full flow.
    /// </summary>
    public class CombatTriggerIntegrationTests
    {
        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        private readonly MockGameBridge _gameBridge;
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly TerritoryManager _territoryManager;
        private readonly InMemoryPedPool _pedPool;
        private readonly CombatManager _combatManager;
        private readonly CombatTriggerCoordinator _combatTriggerCoordinator;

        public CombatTriggerIntegrationTests()
        {
            _gameBridge = new MockGameBridge
            {
                PlayerPosition = new Vector3(0, 0, 0),
                PlayerHeading = 0f
            };

            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _territoryManager = new TerritoryManager(_gameBridge, _zoneService);

            _pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(_gameBridge, _pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(_gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var waveSpawnerService = new WaveSpawnerService();
            var combatResultHandler = new CombatResultHandler(_zoneService);
            var followerService = new FollowerService();

            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();
            _combatManager = new CombatManager(
                _gameBridge,
                _pedPool,
                pedSpawningService,
                spawnPositionCalculator,
                controlCalculator,
                takeoverDetector,
                combatResultHandler,
                waveSpawnerService,
                followerService,
                aggressionResponseServiceMock.Object);

            _combatTriggerCoordinator = new CombatTriggerCoordinator(
                _territoryManager,
                _combatManager,
                MichaelFactionId);
        }

        #region Combat Trigger Tests

        [Fact]
        public void PlayerEntersEnemyZone_TriggersCombat()
        {
            // Arrange: Create an enemy zone (owned by Trevor)
            var enemyZone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 500, 500);

            // Player is Michael, initially outside zone
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);
            _territoryManager.Update();

            // Act: Player enters enemy zone
            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            // Assert: Combat should be triggered
            Assert.True(_combatManager.IsInCombat);
            Assert.NotNull(_combatManager.CurrentEncounter);
            Assert.Equal("zone-1", _combatManager.CurrentEncounter!.ZoneId);
            Assert.Equal(MichaelFactionId, _combatManager.CurrentEncounter.AttackingFactionId);
            Assert.Equal(TrevorFactionId, _combatManager.CurrentEncounter.DefendingFactionId);
        }

        [Fact]
        public void PlayerEntersOwnZone_DoesNotTriggerCombat()
        {
            // Arrange: Create a zone owned by Michael (same faction as player)
            var ownZone = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, 500, 500);

            // Player is Michael, initially outside zone
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);
            _territoryManager.Update();

            // Act: Player enters own zone
            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            // Assert: Combat should NOT be triggered
            Assert.False(_combatManager.IsInCombat);
            Assert.Null(_combatManager.CurrentEncounter);
        }

        [Fact]
        public void PlayerEntersNeutralZone_DoesNotTriggerCombat()
        {
            // Arrange: Create a neutral zone (no owner)
            var neutralZone = new Zone("zone-1", "Downtown", new Vector3(500, 500, 0), 200f, 5);
            _zoneRepository.Add(neutralZone);

            // Player is Michael, initially outside zone
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);
            _territoryManager.Update();

            // Act: Player enters neutral zone
            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            // Assert: Combat should NOT be triggered
            Assert.False(_combatManager.IsInCombat);
            Assert.Null(_combatManager.CurrentEncounter);
        }

        #endregion

        #region Combat Exit Tests

        [Fact]
        public void PlayerExitsZoneDuringCombat_AbortsCombat()
        {
            // Arrange: Start combat in enemy zone
            var enemyZone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 500, 500);

            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            Assert.True(_combatManager.IsInCombat);

            // Act: Player leaves zone (exits to outside all zones)
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);
            _territoryManager.Update();

            // Assert: Combat should be aborted
            Assert.False(_combatManager.IsInCombat);
        }

        [Fact]
        public void PlayerExitsZoneDuringCombat_ZoneOwnershipUnchanged()
        {
            // Arrange: Start combat in enemy zone (owned by Trevor)
            var enemyZone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 500, 500);

            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            // Act: Player leaves zone
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);
            _territoryManager.Update();

            // Assert: Zone should still be owned by Trevor
            var zone = _zoneRepository.GetById("zone-1");
            Assert.Equal(TrevorFactionId, zone!.OwnerFactionId);
        }

        #endregion

        #region Full Capture Flow Tests

        [Fact]
        public void FullCaptureFlow_EnterZone_KillDefenders_CaptureZone()
        {
            // Arrange: Create enemy zone owned by Trevor
            var enemyZone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 500, 500);

            // Step 1: Player enters enemy zone, combat starts
            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            Assert.True(_combatManager.IsInCombat);
            Assert.Equal(TrevorFactionId, enemyZone.OwnerFactionId);

            // Step 2: Spawn attackers (player's faction) and defenders
            SpawnPedsForFaction(MichaelFactionId, "zone-1", 5);
            SpawnPedsForFaction(TrevorFactionId, "zone-1", 3);

            // Step 3: Combat update - both sides have peds, combat continues
            _combatManager.Update();
            Assert.True(_combatManager.IsInCombat);

            // Step 4: Simulate all defenders killed
            RemoveAllPedsForFaction(TrevorFactionId);

            // Step 5: Combat update - attacker victory
            _combatManager.Update();

            // Assert: Combat ended, zone captured by Michael
            Assert.False(_combatManager.IsInCombat);
            var capturedZone = _zoneRepository.GetById("zone-1");
            Assert.Equal(MichaelFactionId, capturedZone!.OwnerFactionId);
            Assert.Equal(100f, capturedZone.ControlPercentage);
        }

        [Fact]
        public void FullCaptureFlow_PlayerDies_RetreatsAndZoneUnchanged()
        {
            // Arrange: Create enemy zone owned by Trevor
            var enemyZone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 500, 500);

            // Step 1: Player enters enemy zone, combat starts
            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            Assert.True(_combatManager.IsInCombat);

            // Step 2: Spawn peds for battle
            SpawnPedsForFaction(MichaelFactionId, "zone-1", 1);
            SpawnPedsForFaction(TrevorFactionId, "zone-1", 5);

            // Step 3: Combat in progress
            _combatManager.Update();
            Assert.True(_combatManager.IsInCombat);

            // Step 4: Player dies
            _gameBridge.IsPlayerDeadValue = true;
            _combatManager.Update();

            // Assert: Combat ended as retreat, zone unchanged
            Assert.False(_combatManager.IsInCombat);
            var zone = _zoneRepository.GetById("zone-1");
            Assert.Equal(TrevorFactionId, zone!.OwnerFactionId);
        }

        #endregion

        #region Faction Switching During Combat Tests

        [Fact]
        public void FactionSwitch_UpdatesPlayerFaction_NewCombatUsesNewFaction()
        {
            // Arrange: Start combat as Michael against Trevor zone
            var enemyZone = CreateAndAddZone("zone-1", "Downtown", TrevorFactionId, 500, 500);

            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            Assert.Equal(MichaelFactionId, _combatManager.CurrentEncounter!.AttackingFactionId);

            // End current combat
            _combatManager.AbortCombat();

            // Dispose the old coordinator and create a new one with Franklin as the player
            _combatTriggerCoordinator.Dispose();
            var franklinCoordinator = new CombatTriggerCoordinator(
                _territoryManager,
                _combatManager,
                FranklinFactionId);

            // Re-enter zone as Franklin
            _gameBridge.PlayerPosition = new Vector3(0, 0, 0);
            _territoryManager.Update();

            _gameBridge.PlayerPosition = new Vector3(500, 500, 0);
            _territoryManager.Update();

            // Assert: New combat should use Franklin's faction
            Assert.True(_combatManager.IsInCombat);
            Assert.Equal(FranklinFactionId, _combatManager.CurrentEncounter!.AttackingFactionId);
        }

        #endregion

        #region Helper Methods

        private Zone CreateAndAddZone(string id, string name, string ownerFactionId, float x, float y)
        {
            var zone = new Zone(id, name, new Vector3(x, y, 0), 200f, 5);
            zone.OwnerFactionId = ownerFactionId;
            zone.ControlPercentage = 100f;
            _zoneRepository.Add(zone);
            return zone;
        }

        private void SpawnPedsForFaction(string factionId, string zoneId, int count)
        {
            var pedSpawningService = new PedSpawningService(_gameBridge, _pedPool);
            for (int i = 0; i < count; i++)
            {
                pedSpawningService.SpawnPed("s_m_y_dealer_01", new Vector3(500, 500, 0), factionId, zoneId);
            }
        }

        private void RemoveAllPedsForFaction(string factionId)
        {
            foreach (var ped in _pedPool.GetByFaction(factionId).ToList())
            {
                _pedPool.Remove(ped);
            }
        }

        #endregion
    }
}
