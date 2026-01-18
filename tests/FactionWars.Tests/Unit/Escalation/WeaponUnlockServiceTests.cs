using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using FactionWars.Escalation.Models;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Services;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the IWeaponUnlockService interface and its implementations.
    /// The service provides business logic for weapon unlocks based on escalation tiers.
    /// </summary>
    public class WeaponUnlockServiceTests
    {
        private readonly Mock<IWeaponUnlockRepository> _mockRepository;
        private readonly Mock<IEscalationService> _mockEscalationService;
        private readonly IWeaponUnlockService _service;

        public WeaponUnlockServiceTests()
        {
            _mockRepository = new Mock<IWeaponUnlockRepository>();
            _mockEscalationService = new Mock<IEscalationService>();
            _service = new WeaponUnlockService(_mockRepository.Object, _mockEscalationService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WeaponUnlockService(null!, _mockEscalationService.Object));
        }

        [Fact]
        public void Constructor_WithNullEscalationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WeaponUnlockService(_mockRepository.Object, null!));
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesService()
        {
            var service = new WeaponUnlockService(_mockRepository.Object, _mockEscalationService.Object);

            Assert.NotNull(service);
        }

        #endregion

        #region GetAvailableWeapons Tests

        [Fact]
        public void GetAvailableWeapons_ForFaction_ReturnsWeaponsUpToCurrentTier()
        {
            var weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1),
                new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier2)).Returns(weapons);

            var result = _service.GetAvailableWeapons("faction1").ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAvailableWeapons_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetAvailableWeapons(null!));
        }

        [Fact]
        public void GetAvailableWeapons_WithEmptyFactionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.GetAvailableWeapons(""));
        }

        [Fact]
        public void GetAvailableWeapons_Tier1Faction_ReturnsOnlyTier1Weapons()
        {
            var tier1Weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier1);
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier1)).Returns(tier1Weapons);

            var result = _service.GetAvailableWeapons("faction1").ToList();

            Assert.Single(result);
            Assert.Equal("WEAPON_PISTOL", result[0].WeaponHash);
        }

        #endregion

        #region GetAvailableWeaponsByCategory Tests

        [Fact]
        public void GetAvailableWeaponsByCategory_ReturnsFilteredWeapons()
        {
            var weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1),
                new WeaponUnlock("WEAPON_COMBATPISTOL", "Combat Pistol", WeaponCategory.Pistol, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier2, WeaponCategory.Pistol))
                .Returns(weapons);

            var result = _service.GetAvailableWeaponsByCategory("faction1", WeaponCategory.Pistol).ToList();

            Assert.Equal(2, result.Count);
            _mockRepository.Verify(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier2, WeaponCategory.Pistol), Times.Once);
        }

        [Fact]
        public void GetAvailableWeaponsByCategory_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.GetAvailableWeaponsByCategory(null!, WeaponCategory.Pistol));
        }

        #endregion

        #region IsWeaponAvailable Tests

        [Fact]
        public void IsWeaponAvailable_WeaponUnlocked_ReturnsTrue()
        {
            var weapon = new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2);
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier3);
            _mockRepository.Setup(x => x.GetByHash("WEAPON_SMG")).Returns(weapon);

            var result = _service.IsWeaponAvailable("faction1", "WEAPON_SMG");

            Assert.True(result);
        }

        [Fact]
        public void IsWeaponAvailable_WeaponNotUnlocked_ReturnsFalse()
        {
            var weapon = new WeaponUnlock("WEAPON_RPG", "RPG", WeaponCategory.Heavy, EscalationTier.Tier5);
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetByHash("WEAPON_RPG")).Returns(weapon);

            var result = _service.IsWeaponAvailable("faction1", "WEAPON_RPG");

            Assert.False(result);
        }

        [Fact]
        public void IsWeaponAvailable_WeaponNotFound_ReturnsFalse()
        {
            _mockRepository.Setup(x => x.GetByHash("WEAPON_NONEXISTENT")).Returns((WeaponUnlock?)null);

            var result = _service.IsWeaponAvailable("faction1", "WEAPON_NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void IsWeaponAvailable_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.IsWeaponAvailable(null!, "WEAPON_PISTOL"));
        }

        [Fact]
        public void IsWeaponAvailable_WithNullWeaponHash_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.IsWeaponAvailable("faction1", null!));
        }

        [Fact]
        public void IsWeaponAvailable_AtExactTier_ReturnsTrue()
        {
            var weapon = new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2);
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetByHash("WEAPON_SMG")).Returns(weapon);

            var result = _service.IsWeaponAvailable("faction1", "WEAPON_SMG");

            Assert.True(result);
        }

        #endregion

        #region GetNewlyUnlockedWeapons Tests

        [Fact]
        public void GetNewlyUnlockedWeapons_TierChanged_ReturnsNewWeapons()
        {
            var tier3Weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3),
                new WeaponUnlock("WEAPON_ADVANCEDRIFLE", "Advanced Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3)
            };
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier3)).Returns(tier3Weapons);

            var result = _service.GetNewlyUnlockedWeapons(EscalationTier.Tier2, EscalationTier.Tier3).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, w => Assert.Equal(EscalationTier.Tier3, w.RequiredTier));
        }

        [Fact]
        public void GetNewlyUnlockedWeapons_SameTier_ReturnsEmpty()
        {
            var result = _service.GetNewlyUnlockedWeapons(EscalationTier.Tier2, EscalationTier.Tier2);

            Assert.Empty(result);
            _mockRepository.Verify(x => x.GetByTier(It.IsAny<EscalationTier>()), Times.Never);
        }

        [Fact]
        public void GetNewlyUnlockedWeapons_TierDecreased_ReturnsEmpty()
        {
            var result = _service.GetNewlyUnlockedWeapons(EscalationTier.Tier3, EscalationTier.Tier2);

            Assert.Empty(result);
            _mockRepository.Verify(x => x.GetByTier(It.IsAny<EscalationTier>()), Times.Never);
        }

        [Fact]
        public void GetNewlyUnlockedWeapons_JumpMultipleTiers_ReturnsAllNewWeapons()
        {
            var tier2Weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2)
            };
            var tier3Weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3)
            };
            var tier4Weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_MG", "Machine Gun", WeaponCategory.LMG, EscalationTier.Tier4)
            };

            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier2)).Returns(tier2Weapons);
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier3)).Returns(tier3Weapons);
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier4)).Returns(tier4Weapons);

            var result = _service.GetNewlyUnlockedWeapons(EscalationTier.Tier1, EscalationTier.Tier4).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_SMG");
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_CARBINERIFLE");
            Assert.Contains(result, w => w.WeaponHash == "WEAPON_MG");
        }

        #endregion

        #region GetRandomWeaponForTier Tests

        [Fact]
        public void GetRandomWeaponForTier_WithAvailableWeapons_ReturnsWeapon()
        {
            var weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1),
                new WeaponUnlock("WEAPON_SNSPISTOL", "SNS Pistol", WeaponCategory.Pistol, EscalationTier.Tier1)
            };
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier1)).Returns(weapons);

            var result = _service.GetRandomWeaponForTier(EscalationTier.Tier1);

            Assert.NotNull(result);
            Assert.Contains(result.WeaponHash, new[] { "WEAPON_PISTOL", "WEAPON_SNSPISTOL" });
        }

        [Fact]
        public void GetRandomWeaponForTier_NoWeaponsAvailable_ReturnsNull()
        {
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier5))
                .Returns(Enumerable.Empty<WeaponUnlock>());

            var result = _service.GetRandomWeaponForTier(EscalationTier.Tier5);

            Assert.Null(result);
        }

        #endregion

        #region GetRandomWeaponForFaction Tests

        [Fact]
        public void GetRandomWeaponForFaction_ReturnsWeaponBasedOnFactionTier()
        {
            var weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1),
                new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTier(EscalationTier.Tier2)).Returns(weapons);

            var result = _service.GetRandomWeaponForFaction("faction1");

            Assert.NotNull(result);
            _mockEscalationService.Verify(x => x.GetCurrentTier("faction1"), Times.Once);
        }

        [Fact]
        public void GetRandomWeaponForFaction_WithNullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetRandomWeaponForFaction(null!));
        }

        #endregion

        #region GetRandomWeaponForFactionByCategory Tests

        [Fact]
        public void GetRandomWeaponForFactionByCategory_ReturnsFilteredWeapon()
        {
            var weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1),
                new WeaponUnlock("WEAPON_COMBATPISTOL", "Combat Pistol", WeaponCategory.Pistol, EscalationTier.Tier2)
            };
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier2);
            _mockRepository.Setup(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier2, WeaponCategory.Pistol))
                .Returns(weapons);

            var result = _service.GetRandomWeaponForFactionByCategory("faction1", WeaponCategory.Pistol);

            Assert.NotNull(result);
            Assert.Equal(WeaponCategory.Pistol, result!.Category);
        }

        [Fact]
        public void GetRandomWeaponForFactionByCategory_NoMatchingWeapons_ReturnsNull()
        {
            _mockEscalationService.Setup(x => x.GetCurrentTier("faction1")).Returns(EscalationTier.Tier1);
            _mockRepository.Setup(x => x.GetUnlockedAtTierByCategory(EscalationTier.Tier1, WeaponCategory.Heavy))
                .Returns(Enumerable.Empty<WeaponUnlock>());

            var result = _service.GetRandomWeaponForFactionByCategory("faction1", WeaponCategory.Heavy);

            Assert.Null(result);
        }

        #endregion

        #region RegisterWeapon Tests

        [Fact]
        public void RegisterWeapon_ValidWeapon_ReturnsTrue()
        {
            var weapon = new WeaponUnlock("WEAPON_TEST", "Test", WeaponCategory.Pistol, EscalationTier.Tier1);
            _mockRepository.Setup(x => x.Add(weapon)).Returns(true);

            var result = _service.RegisterWeapon(weapon);

            Assert.True(result);
            _mockRepository.Verify(x => x.Add(weapon), Times.Once);
        }

        [Fact]
        public void RegisterWeapon_DuplicateWeapon_ReturnsFalse()
        {
            var weapon = new WeaponUnlock("WEAPON_TEST", "Test", WeaponCategory.Pistol, EscalationTier.Tier1);
            _mockRepository.Setup(x => x.Add(weapon)).Returns(false);

            var result = _service.RegisterWeapon(weapon);

            Assert.False(result);
        }

        [Fact]
        public void RegisterWeapon_NullWeapon_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RegisterWeapon(null!));
        }

        #endregion

        #region UnregisterWeapon Tests

        [Fact]
        public void UnregisterWeapon_ExistingWeapon_ReturnsTrue()
        {
            _mockRepository.Setup(x => x.Remove("WEAPON_TEST")).Returns(true);

            var result = _service.UnregisterWeapon("WEAPON_TEST");

            Assert.True(result);
            _mockRepository.Verify(x => x.Remove("WEAPON_TEST"), Times.Once);
        }

        [Fact]
        public void UnregisterWeapon_NonExistentWeapon_ReturnsFalse()
        {
            _mockRepository.Setup(x => x.Remove("WEAPON_NONEXISTENT")).Returns(false);

            var result = _service.UnregisterWeapon("WEAPON_NONEXISTENT");

            Assert.False(result);
        }

        [Fact]
        public void UnregisterWeapon_NullHash_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UnregisterWeapon(null!));
        }

        #endregion

        #region GetAllRegisteredWeapons Tests

        [Fact]
        public void GetAllRegisteredWeapons_ReturnsAllWeapons()
        {
            var weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1),
                new WeaponUnlock("WEAPON_SMG", "SMG", WeaponCategory.SMG, EscalationTier.Tier2)
            };
            _mockRepository.Setup(x => x.GetAll()).Returns(weapons);

            var result = _service.GetAllRegisteredWeapons().ToList();

            Assert.Equal(2, result.Count);
            _mockRepository.Verify(x => x.GetAll(), Times.Once);
        }

        #endregion

        #region GetWeaponInfo Tests

        [Fact]
        public void GetWeaponInfo_ExistingWeapon_ReturnsWeapon()
        {
            var weapon = new WeaponUnlock("WEAPON_PISTOL", "Pistol", WeaponCategory.Pistol, EscalationTier.Tier1);
            _mockRepository.Setup(x => x.GetByHash("WEAPON_PISTOL")).Returns(weapon);

            var result = _service.GetWeaponInfo("WEAPON_PISTOL");

            Assert.NotNull(result);
            Assert.Equal("WEAPON_PISTOL", result!.WeaponHash);
        }

        [Fact]
        public void GetWeaponInfo_NonExistentWeapon_ReturnsNull()
        {
            _mockRepository.Setup(x => x.GetByHash("WEAPON_NONEXISTENT")).Returns((WeaponUnlock?)null);

            var result = _service.GetWeaponInfo("WEAPON_NONEXISTENT");

            Assert.Null(result);
        }

        [Fact]
        public void GetWeaponInfo_NullHash_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetWeaponInfo(null!));
        }

        #endregion

        #region GetWeaponsNewlyUnlockedForFaction Tests

        [Fact]
        public void GetWeaponsNewlyUnlockedForFaction_ReturnsWeaponsBasedOnPreviousTier()
        {
            // Start at Tier2 (2000 points), then add points to reach Tier3
            var escalation = new FactionEscalation("faction1", 2000);
            escalation.AddPoints(1500); // Now at 3500 points (Tier3), PreviousTier = Tier2
            _mockEscalationService.Setup(x => x.GetEscalation("faction1")).Returns(escalation);

            var tier3Weapons = new List<WeaponUnlock>
            {
                new WeaponUnlock("WEAPON_CARBINERIFLE", "Carbine Rifle", WeaponCategory.AssaultRifle, EscalationTier.Tier3)
            };
            _mockRepository.Setup(x => x.GetByTier(EscalationTier.Tier3)).Returns(tier3Weapons);

            var result = _service.GetWeaponsNewlyUnlockedForFaction("faction1").ToList();

            Assert.Single(result);
            Assert.Equal("WEAPON_CARBINERIFLE", result[0].WeaponHash);
        }

        [Fact]
        public void GetWeaponsNewlyUnlockedForFaction_FactionNotFound_ReturnsEmpty()
        {
            _mockEscalationService.Setup(x => x.GetEscalation("nonexistent")).Returns((FactionEscalation?)null);

            var result = _service.GetWeaponsNewlyUnlockedForFaction("nonexistent");

            Assert.Empty(result);
        }

        [Fact]
        public void GetWeaponsNewlyUnlockedForFaction_NullFactionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetWeaponsNewlyUnlockedForFaction(null!));
        }

        #endregion
    }
}
