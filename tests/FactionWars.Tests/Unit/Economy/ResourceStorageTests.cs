using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Economy.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    /// <summary>
    /// Tests for the ResourceStorage class which manages resource amounts with caps.
    /// </summary>
    public class ResourceStorageTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithNoParameters_CreatesStorageWithDefaultCaps()
        {
            // Act
            var storage = new ResourceStorage();

            // Assert
            Assert.Equal(ResourceTypeInfo.GetInfo(ResourceType.Cash).DefaultCap, storage.GetCap(ResourceType.Cash));
            Assert.Equal(ResourceTypeInfo.GetInfo(ResourceType.Recruitment).DefaultCap, storage.GetCap(ResourceType.Recruitment));
            Assert.Equal(ResourceTypeInfo.GetInfo(ResourceType.Weapons).DefaultCap, storage.GetCap(ResourceType.Weapons));
        }

        [Fact]
        public void Constructor_WithNoParameters_StartsWithZeroResources()
        {
            // Act
            var storage = new ResourceStorage();

            // Assert
            Assert.Equal(0, storage.GetAmount(ResourceType.Cash));
            Assert.Equal(0, storage.GetAmount(ResourceType.Recruitment));
            Assert.Equal(0, storage.GetAmount(ResourceType.Weapons));
        }

        [Fact]
        public void Constructor_WithCustomCaps_SetsProvidedCaps()
        {
            // Arrange
            int customCashCap = 50000;
            int customRecruitmentCap = 500;
            int customWeaponsCap = 250;

            // Act
            var storage = new ResourceStorage(customCashCap, customRecruitmentCap, customWeaponsCap);

            // Assert
            Assert.Equal(customCashCap, storage.GetCap(ResourceType.Cash));
            Assert.Equal(customRecruitmentCap, storage.GetCap(ResourceType.Recruitment));
            Assert.Equal(customWeaponsCap, storage.GetCap(ResourceType.Weapons));
        }

        [Theory]
        [InlineData(-1, 100, 100)]
        [InlineData(100, -1, 100)]
        [InlineData(100, 100, -1)]
        public void Constructor_WithNegativeCaps_ThrowsArgumentOutOfRangeException(int cash, int recruitment, int weapons)
        {
            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new ResourceStorage(cash, recruitment, weapons));
        }

        [Theory]
        [InlineData(0, 100, 100)]
        [InlineData(100, 0, 100)]
        [InlineData(100, 100, 0)]
        public void Constructor_WithZeroCaps_ThrowsArgumentOutOfRangeException(int cash, int recruitment, int weapons)
        {
            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new ResourceStorage(cash, recruitment, weapons));
        }

        #endregion

        #region Add Tests

        [Fact]
        public void Add_ValidAmount_IncreasesResource()
        {
            // Arrange
            var storage = new ResourceStorage();
            int addAmount = 500;

            // Act
            storage.Add(ResourceType.Cash, addAmount);

            // Assert
            Assert.Equal(addAmount, storage.GetAmount(ResourceType.Cash));
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void Add_ValidAmount_WorksForAllResourceTypes(ResourceType resourceType)
        {
            // Arrange
            var storage = new ResourceStorage();
            int addAmount = 100;

            // Act
            storage.Add(resourceType, addAmount);

            // Assert
            Assert.Equal(addAmount, storage.GetAmount(resourceType));
        }

        [Fact]
        public void Add_NegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.Add(ResourceType.Cash, -100));
        }

        [Fact]
        public void Add_ExceedsCap_ClampsToCapValue()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);

            // Act
            storage.Add(ResourceType.Cash, cap + 500);

            // Assert
            Assert.Equal(cap, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Add_WouldExceedCap_ClampsToCapValue()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, 800);

            // Act
            storage.Add(ResourceType.Cash, 400); // Would make 1200, but cap is 1000

            // Assert
            Assert.Equal(cap, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Add_ReturnsActualAmountAdded_WhenNotCapped()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act
            int added = storage.Add(ResourceType.Cash, 500);

            // Assert
            Assert.Equal(500, added);
        }

        [Fact]
        public void Add_ReturnsActualAmountAdded_WhenCapped()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, 800);

            // Act
            int added = storage.Add(ResourceType.Cash, 400); // Only 200 can be added

            // Assert
            Assert.Equal(200, added);
        }

        [Fact]
        public void Add_AtCap_ReturnsZero()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, cap);

            // Act
            int added = storage.Add(ResourceType.Cash, 100);

            // Assert
            Assert.Equal(0, added);
            Assert.Equal(cap, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Add_Zero_DoesNotChangeAmountAndReturnsZero()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 500);

            // Act
            int added = storage.Add(ResourceType.Cash, 0);

            // Assert
            Assert.Equal(0, added);
            Assert.Equal(500, storage.GetAmount(ResourceType.Cash));
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_ValidAmount_DecreasesResource()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 1000);

            // Act
            bool success = storage.Remove(ResourceType.Cash, 400);

            // Assert
            Assert.True(success);
            Assert.Equal(600, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Remove_ExactAmount_LeavesZero()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 500);

            // Act
            bool success = storage.Remove(ResourceType.Cash, 500);

            // Assert
            Assert.True(success);
            Assert.Equal(0, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Remove_MoreThanAvailable_ReturnsFalseAndDoesNotChange()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 500);

            // Act
            bool success = storage.Remove(ResourceType.Cash, 600);

            // Assert
            Assert.False(success);
            Assert.Equal(500, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Remove_FromZero_ReturnsFalse()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act
            bool success = storage.Remove(ResourceType.Cash, 100);

            // Assert
            Assert.False(success);
            Assert.Equal(0, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Remove_NegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 1000);

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.Remove(ResourceType.Cash, -100));
        }

        [Fact]
        public void Remove_Zero_ReturnsTrueAndDoesNotChange()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 500);

            // Act
            bool success = storage.Remove(ResourceType.Cash, 0);

            // Assert
            Assert.True(success);
            Assert.Equal(500, storage.GetAmount(ResourceType.Cash));
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void Remove_WorksForAllResourceTypes(ResourceType resourceType)
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(resourceType, 200);

            // Act
            bool success = storage.Remove(resourceType, 100);

            // Assert
            Assert.True(success);
            Assert.Equal(100, storage.GetAmount(resourceType));
        }

        #endregion

        #region Set Tests

        [Fact]
        public void Set_ValidAmount_SetsResource()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act
            storage.Set(ResourceType.Cash, 500);

            // Assert
            Assert.Equal(500, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Set_OverwritesPreviousValue()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 1000);

            // Act
            storage.Set(ResourceType.Cash, 200);

            // Assert
            Assert.Equal(200, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Set_ExceedsCap_ClampsToCapValue()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);

            // Act
            storage.Set(ResourceType.Cash, cap + 500);

            // Assert
            Assert.Equal(cap, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Set_ToZero_SetsToZero()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 500);

            // Act
            storage.Set(ResourceType.Cash, 0);

            // Assert
            Assert.Equal(0, storage.GetAmount(ResourceType.Cash));
        }

        [Fact]
        public void Set_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.Set(ResourceType.Cash, -100));
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void Set_WorksForAllResourceTypes(ResourceType resourceType)
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act
            storage.Set(resourceType, 150);

            // Assert
            Assert.Equal(150, storage.GetAmount(resourceType));
        }

        #endregion

        #region Cap Management Tests

        [Fact]
        public void SetCap_ValidCap_UpdatesCap()
        {
            // Arrange
            var storage = new ResourceStorage();
            int newCap = 50000;

            // Act
            storage.SetCap(ResourceType.Cash, newCap);

            // Assert
            Assert.Equal(newCap, storage.GetCap(ResourceType.Cash));
        }

        [Fact]
        public void SetCap_LowerThanCurrentAmount_ClampsAmountToCap()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 5000);
            int newCap = 2000;

            // Act
            storage.SetCap(ResourceType.Cash, newCap);

            // Assert
            Assert.Equal(newCap, storage.GetCap(ResourceType.Cash));
            Assert.Equal(newCap, storage.GetAmount(ResourceType.Cash)); // Amount clamped
        }

        [Fact]
        public void SetCap_NegativeCap_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.SetCap(ResourceType.Cash, -100));
        }

        [Fact]
        public void SetCap_ZeroCap_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.SetCap(ResourceType.Cash, 0));
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void SetCap_WorksForAllResourceTypes(ResourceType resourceType)
        {
            // Arrange
            var storage = new ResourceStorage();
            int newCap = 999;

            // Act
            storage.SetCap(resourceType, newCap);

            // Assert
            Assert.Equal(newCap, storage.GetCap(resourceType));
        }

        #endregion

        #region Capacity Query Tests

        [Fact]
        public void GetRemainingCapacity_EmptyStorage_ReturnsFullCap()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);

            // Act
            int remaining = storage.GetRemainingCapacity(ResourceType.Cash);

            // Assert
            Assert.Equal(cap, remaining);
        }

        [Fact]
        public void GetRemainingCapacity_PartiallyFilled_ReturnsCorrectValue()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, 300);

            // Act
            int remaining = storage.GetRemainingCapacity(ResourceType.Cash);

            // Assert
            Assert.Equal(700, remaining);
        }

        [Fact]
        public void GetRemainingCapacity_AtCap_ReturnsZero()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, cap);

            // Act
            int remaining = storage.GetRemainingCapacity(ResourceType.Cash);

            // Assert
            Assert.Equal(0, remaining);
        }

        [Fact]
        public void IsAtCap_WhenFull_ReturnsTrue()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, cap);

            // Act
            bool atCap = storage.IsAtCap(ResourceType.Cash);

            // Assert
            Assert.True(atCap);
        }

        [Fact]
        public void IsAtCap_WhenNotFull_ReturnsFalse()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, 500);

            // Act
            bool atCap = storage.IsAtCap(ResourceType.Cash);

            // Assert
            Assert.False(atCap);
        }

        [Fact]
        public void IsAtCap_WhenEmpty_ReturnsFalse()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act
            bool atCap = storage.IsAtCap(ResourceType.Cash);

            // Assert
            Assert.False(atCap);
        }

        [Fact]
        public void GetFillPercentage_Empty_ReturnsZero()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act
            float percentage = storage.GetFillPercentage(ResourceType.Cash);

            // Assert
            Assert.Equal(0f, percentage);
        }

        [Fact]
        public void GetFillPercentage_HalfFull_Returns50()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, 500);

            // Act
            float percentage = storage.GetFillPercentage(ResourceType.Cash);

            // Assert
            Assert.Equal(50f, percentage);
        }

        [Fact]
        public void GetFillPercentage_Full_Returns100()
        {
            // Arrange
            int cap = 1000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, cap);

            // Act
            float percentage = storage.GetFillPercentage(ResourceType.Cash);

            // Assert
            Assert.Equal(100f, percentage);
        }

        #endregion

        #region HasAmount Tests

        [Fact]
        public void HasAmount_SufficientResources_ReturnsTrue()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 1000);

            // Act
            bool has = storage.HasAmount(ResourceType.Cash, 500);

            // Assert
            Assert.True(has);
        }

        [Fact]
        public void HasAmount_ExactResources_ReturnsTrue()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 500);

            // Act
            bool has = storage.HasAmount(ResourceType.Cash, 500);

            // Assert
            Assert.True(has);
        }

        [Fact]
        public void HasAmount_InsufficientResources_ReturnsFalse()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 300);

            // Act
            bool has = storage.HasAmount(ResourceType.Cash, 500);

            // Assert
            Assert.False(has);
        }

        [Fact]
        public void HasAmount_Zero_ReturnsTrue()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act
            bool has = storage.HasAmount(ResourceType.Cash, 0);

            // Assert
            Assert.True(has);
        }

        [Fact]
        public void HasAmount_NegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 1000);

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.HasAmount(ResourceType.Cash, -100));
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_SingleResource_SetsToZero()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 1000);
            storage.Add(ResourceType.Weapons, 500);

            // Act
            storage.Clear(ResourceType.Cash);

            // Assert
            Assert.Equal(0, storage.GetAmount(ResourceType.Cash));
            Assert.Equal(500, storage.GetAmount(ResourceType.Weapons)); // Unchanged
        }

        [Fact]
        public void ClearAll_ResetsAllResourcesToZero()
        {
            // Arrange
            var storage = new ResourceStorage();
            storage.Add(ResourceType.Cash, 1000);
            storage.Add(ResourceType.Recruitment, 500);
            storage.Add(ResourceType.Weapons, 250);

            // Act
            storage.ClearAll();

            // Assert
            Assert.Equal(0, storage.GetAmount(ResourceType.Cash));
            Assert.Equal(0, storage.GetAmount(ResourceType.Recruitment));
            Assert.Equal(0, storage.GetAmount(ResourceType.Weapons));
        }

        [Fact]
        public void ClearAll_DoesNotAffectCaps()
        {
            // Arrange
            int cap = 5000;
            var storage = new ResourceStorage(cap, cap, cap);
            storage.Add(ResourceType.Cash, 1000);

            // Act
            storage.ClearAll();

            // Assert
            Assert.Equal(cap, storage.GetCap(ResourceType.Cash));
            Assert.Equal(cap, storage.GetCap(ResourceType.Recruitment));
            Assert.Equal(cap, storage.GetCap(ResourceType.Weapons));
        }

        #endregion

        #region Cap Modifier Tests

        [Fact]
        public void ModifyCap_ByMultiplier_IncreasesCap()
        {
            // Arrange
            int initialCap = 1000;
            var storage = new ResourceStorage(initialCap, initialCap, initialCap);

            // Act
            storage.ModifyCap(ResourceType.Cash, 1.5f);

            // Assert
            Assert.Equal(1500, storage.GetCap(ResourceType.Cash));
        }

        [Fact]
        public void ModifyCap_ByMultiplier_DecreasesCap()
        {
            // Arrange
            int initialCap = 1000;
            var storage = new ResourceStorage(initialCap, initialCap, initialCap);

            // Act
            storage.ModifyCap(ResourceType.Cash, 0.5f);

            // Assert
            Assert.Equal(500, storage.GetCap(ResourceType.Cash));
        }

        [Fact]
        public void ModifyCap_DecreaseClampCurrentAmount()
        {
            // Arrange
            int initialCap = 1000;
            var storage = new ResourceStorage(initialCap, initialCap, initialCap);
            storage.Add(ResourceType.Cash, 800);

            // Act
            storage.ModifyCap(ResourceType.Cash, 0.5f); // New cap = 500

            // Assert
            Assert.Equal(500, storage.GetCap(ResourceType.Cash));
            Assert.Equal(500, storage.GetAmount(ResourceType.Cash)); // Clamped
        }

        [Fact]
        public void ModifyCap_NegativeMultiplier_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.ModifyCap(ResourceType.Cash, -0.5f));
        }

        [Fact]
        public void ModifyCap_ZeroMultiplier_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var storage = new ResourceStorage();

            // Act & Assert
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.ModifyCap(ResourceType.Cash, 0f));
        }

        [Fact]
        public void ModifyCap_ResultLessThanOne_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            int initialCap = 10;
            var storage = new ResourceStorage(initialCap, initialCap, initialCap);

            // Act & Assert - 10 * 0.05 = 0.5 which rounds to 0, invalid cap
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                storage.ModifyCap(ResourceType.Cash, 0.05f));
        }

        #endregion

        #region Interface Tests

        [Fact]
        public void ImplementsIResourceStorage()
        {
            // Arrange & Act
            var storage = new ResourceStorage();

            // Assert
            Assert.IsAssignableFrom<IResourceStorage>(storage);
        }

        #endregion
    }
}
