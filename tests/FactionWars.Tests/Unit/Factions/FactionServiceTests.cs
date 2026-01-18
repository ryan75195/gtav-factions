using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    /// <summary>
    /// Tests for FactionService business logic and faction management.
    /// </summary>
    public class FactionServiceTests
    {
        private readonly IFactionRepository _repository;
        private readonly IFactionService _service;

        public FactionServiceTests()
        {
            _repository = new InMemoryFactionRepository();
            _service = new FactionService(_repository);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowOnNullRepository()
        {
            Assert.Throws<ArgumentNullException>(() => new FactionService(null!));
        }

        #endregion

        #region GetFaction Tests

        [Fact]
        public void GetFaction_ShouldReturnFactionWhenExists()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.GetFaction("michael");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("michael", result!.Id);
        }

        [Fact]
        public void GetFaction_ShouldReturnNullWhenNotExists()
        {
            // Act
            var result = _service.GetFaction("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFaction_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetFaction(null!));
        }

        [Fact]
        public void GetFaction_ShouldThrowOnEmptyId()
        {
            Assert.Throws<ArgumentException>(() => _service.GetFaction(""));
        }

        [Fact]
        public void GetFaction_ShouldThrowOnWhitespaceId()
        {
            Assert.Throws<ArgumentException>(() => _service.GetFaction("   "));
        }

        #endregion

        #region GetAllFactions Tests

        [Fact]
        public void GetAllFactions_ShouldReturnEmptyWhenNoFactions()
        {
            // Act
            var result = _service.GetAllFactions();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllFactions_ShouldReturnAllFactions()
        {
            // Arrange
            _repository.Add(CreateTestFaction("michael", "De Santa Family"));
            _repository.Add(CreateTestFaction("trevor", "Trevor Philips Industries"));

            // Act
            var result = _service.GetAllFactions().ToList();

            // Assert
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetActiveFactions Tests

        [Fact]
        public void GetActiveFactions_ShouldReturnOnlyActiveFactions()
        {
            // Arrange
            var faction1 = CreateTestFaction("michael", "De Santa Family");
            var faction2 = CreateTestFaction("trevor", "Trevor Philips Industries");
            faction2.IsActive = false;
            _repository.Add(faction1);
            _repository.Add(faction2);

            // Act
            var result = _service.GetActiveFactions().ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("michael", result[0].Id);
        }

        [Fact]
        public void GetActiveFactions_ShouldReturnEmptyWhenNoActiveFactions()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            faction.IsActive = false;
            _repository.Add(faction);

            // Act
            var result = _service.GetActiveFactions();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetFactionState Tests

        [Fact]
        public void GetFactionState_ShouldReturnStateWhenExists()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", initialCash: 1000);
            _repository.SetState(state);

            // Act
            var result = _service.GetFactionState("michael");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("michael", result!.FactionId);
            Assert.Equal(1000, result.Cash);
        }

        [Fact]
        public void GetFactionState_ShouldReturnNullWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.GetFactionState("michael");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFactionState_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetFactionState(null!));
        }

        #endregion

        #region ActivateFaction Tests

        [Fact]
        public void ActivateFaction_ShouldSetIsActiveToTrue()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            faction.IsActive = false;
            _repository.Add(faction);

            // Act
            var result = _service.ActivateFaction("michael");
            var updated = _repository.GetById("michael");

            // Assert
            Assert.True(result);
            Assert.True(updated!.IsActive);
        }

        [Fact]
        public void ActivateFaction_ShouldReturnFalseWhenFactionNotFound()
        {
            // Act
            var result = _service.ActivateFaction("nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ActivateFaction_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.ActivateFaction(null!));
        }

        #endregion

        #region DeactivateFaction Tests

        [Fact]
        public void DeactivateFaction_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            faction.IsActive = true;
            _repository.Add(faction);

            // Act
            var result = _service.DeactivateFaction("michael");
            var updated = _repository.GetById("michael");

            // Assert
            Assert.True(result);
            Assert.False(updated!.IsActive);
        }

        [Fact]
        public void DeactivateFaction_ShouldReturnFalseWhenFactionNotFound()
        {
            // Act
            var result = _service.DeactivateFaction("nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DeactivateFaction_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.DeactivateFaction(null!));
        }

        #endregion

        #region InitializeFactionState Tests

        [Fact]
        public void InitializeFactionState_ShouldCreateNewState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.InitializeFactionState("michael", 5000, 10);

            // Assert
            Assert.True(result);
            var state = _repository.GetState("michael");
            Assert.NotNull(state);
            Assert.Equal(5000, state!.Cash);
            Assert.Equal(10, state.TroopCount);
        }

        [Fact]
        public void InitializeFactionState_ShouldReturnFalseWhenFactionNotFound()
        {
            // Act
            var result = _service.InitializeFactionState("nonexistent", 5000, 10);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InitializeFactionState_ShouldOverwriteExistingState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var oldState = new FactionState("michael", 1000, 5);
            _repository.SetState(oldState);

            // Act
            var result = _service.InitializeFactionState("michael", 10000, 50);
            var state = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(10000, state!.Cash);
            Assert.Equal(50, state.TroopCount);
        }

        [Fact]
        public void InitializeFactionState_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.InitializeFactionState(null!, 1000, 10));
        }

        [Fact]
        public void InitializeFactionState_ShouldClampNegativeValues()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            _service.InitializeFactionState("michael", -100, -10);
            var state = _repository.GetState("michael");

            // Assert
            Assert.Equal(0, state!.Cash);
            Assert.Equal(0, state.TroopCount);
        }

        #endregion

        #region AddZoneToFaction Tests

        [Fact]
        public void AddZoneToFaction_ShouldAddZoneToState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            _repository.SetState(state);

            // Act
            var result = _service.AddZoneToFaction("michael", "zone_1");
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.True(updatedState!.OwnsZone("zone_1"));
        }

        [Fact]
        public void AddZoneToFaction_ShouldReturnFalseWhenFactionNotFound()
        {
            // Act
            var result = _service.AddZoneToFaction("nonexistent", "zone_1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddZoneToFaction_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.AddZoneToFaction("michael", "zone_1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddZoneToFaction_ShouldThrowOnNullFactionId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AddZoneToFaction(null!, "zone_1"));
        }

        [Fact]
        public void AddZoneToFaction_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AddZoneToFaction("michael", null!));
        }

        #endregion

        #region RemoveZoneFromFaction Tests

        [Fact]
        public void RemoveZoneFromFaction_ShouldRemoveZoneFromState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            state.AddZone("zone_1");
            _repository.SetState(state);

            // Act
            var result = _service.RemoveZoneFromFaction("michael", "zone_1");
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.False(updatedState!.OwnsZone("zone_1"));
        }

        [Fact]
        public void RemoveZoneFromFaction_ShouldReturnFalseWhenZoneNotOwned()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            _repository.SetState(state);

            // Act
            var result = _service.RemoveZoneFromFaction("michael", "zone_1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveZoneFromFaction_ShouldReturnFalseWhenFactionNotFound()
        {
            // Act
            var result = _service.RemoveZoneFromFaction("nonexistent", "zone_1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveZoneFromFaction_ShouldThrowOnNullFactionId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveZoneFromFaction(null!, "zone_1"));
        }

        [Fact]
        public void RemoveZoneFromFaction_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveZoneFromFaction("michael", null!));
        }

        #endregion

        #region AddCash Tests

        [Fact]
        public void AddCash_ShouldIncreaseCash()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 1000);
            _repository.SetState(state);

            // Act
            var result = _service.AddCash("michael", 500);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(1500, updatedState!.Cash);
        }

        [Fact]
        public void AddCash_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.AddCash("michael", 500);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddCash_ShouldThrowOnNegativeAmount()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 1000);
            _repository.SetState(state);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.AddCash("michael", -100));
        }

        [Fact]
        public void AddCash_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AddCash(null!, 100));
        }

        #endregion

        #region SpendCash Tests

        [Fact]
        public void SpendCash_ShouldDecreaseCash()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 1000);
            _repository.SetState(state);

            // Act
            var result = _service.SpendCash("michael", 300);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(700, updatedState!.Cash);
        }

        [Fact]
        public void SpendCash_ShouldReturnFalseWhenInsufficientFunds()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 100);
            _repository.SetState(state);

            // Act
            var result = _service.SpendCash("michael", 500);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.False(result);
            Assert.Equal(100, updatedState!.Cash); // Unchanged
        }

        [Fact]
        public void SpendCash_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.SpendCash("michael", 100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SpendCash_ShouldThrowOnNegativeAmount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.SpendCash("michael", -100));
        }

        [Fact]
        public void SpendCash_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.SpendCash(null!, 100));
        }

        #endregion

        #region RecruitTroops Tests

        [Fact]
        public void RecruitTroops_ShouldIncreaseTroopCount()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 0, 10);
            _repository.SetState(state);

            // Act
            var result = _service.RecruitTroops("michael", 5);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(15, updatedState!.TroopCount);
        }

        [Fact]
        public void RecruitTroops_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.RecruitTroops("michael", 5);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RecruitTroops_ShouldThrowOnNegativeCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.RecruitTroops("michael", -5));
        }

        [Fact]
        public void RecruitTroops_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RecruitTroops(null!, 5));
        }

        #endregion

        #region LoseTroops Tests

        [Fact]
        public void LoseTroops_ShouldDecreaseTroopCount()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 0, 20);
            _repository.SetState(state);

            // Act
            var result = _service.LoseTroops("michael", 5);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(15, updatedState!.TroopCount);
        }

        [Fact]
        public void LoseTroops_ShouldClampToZero()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 0, 5);
            _repository.SetState(state);

            // Act
            _service.LoseTroops("michael", 10);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.Equal(0, updatedState!.TroopCount);
        }

        [Fact]
        public void LoseTroops_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.LoseTroops("michael", 5);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LoseTroops_ShouldThrowOnNegativeCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.LoseTroops("michael", -5));
        }

        [Fact]
        public void LoseTroops_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.LoseTroops(null!, 5));
        }

        #endregion

        #region GetMilitaryStrength Tests

        [Fact]
        public void GetMilitaryStrength_ShouldReturnCalculatedStrength()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 0, 10);
            state.Weapons = 5;
            _repository.SetState(state);

            // Act
            var result = _service.GetMilitaryStrength("michael");

            // Assert - Strength = Troops + (Weapons * 2)
            Assert.Equal(20, result); // 10 + (5 * 2)
        }

        [Fact]
        public void GetMilitaryStrength_ShouldReturnZeroWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.GetMilitaryStrength("michael");

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetMilitaryStrength_ShouldReturnZeroWhenFactionNotFound()
        {
            // Act
            var result = _service.GetMilitaryStrength("nonexistent");

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetMilitaryStrength_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetMilitaryStrength(null!));
        }

        #endregion

        #region GetZoneCount Tests

        [Fact]
        public void GetZoneCount_ShouldReturnCountOfOwnedZones()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            state.AddZone("zone_1");
            state.AddZone("zone_2");
            state.AddZone("zone_3");
            _repository.SetState(state);

            // Act
            var result = _service.GetZoneCount("michael");

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void GetZoneCount_ShouldReturnZeroWhenNoZones()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            _repository.SetState(state);

            // Act
            var result = _service.GetZoneCount("michael");

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetZoneCount_ShouldReturnZeroWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.GetZoneCount("michael");

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetZoneCount_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetZoneCount(null!));
        }

        #endregion

        #region CanAfford Tests

        [Fact]
        public void CanAfford_ShouldReturnTrueWhenSufficientFunds()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 1000);
            _repository.SetState(state);

            // Act
            var result = _service.CanAfford("michael", 500);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanAfford_ShouldReturnTrueWhenExactAmount()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 1000);
            _repository.SetState(state);

            // Act
            var result = _service.CanAfford("michael", 1000);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanAfford_ShouldReturnFalseWhenInsufficientFunds()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael", 100);
            _repository.SetState(state);

            // Act
            var result = _service.CanAfford("michael", 500);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanAfford_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.CanAfford("michael", 100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanAfford_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CanAfford(null!, 100));
        }

        #endregion

        #region TransferZoneBetweenFactions Tests

        [Fact]
        public void TransferZoneBetweenFactions_ShouldMoveZone()
        {
            // Arrange
            var faction1 = CreateTestFaction("michael", "De Santa Family");
            var faction2 = CreateTestFaction("trevor", "Trevor Philips Industries");
            _repository.Add(faction1);
            _repository.Add(faction2);
            var state1 = new FactionState("michael");
            state1.AddZone("zone_1");
            var state2 = new FactionState("trevor");
            _repository.SetState(state1);
            _repository.SetState(state2);

            // Act
            var result = _service.TransferZoneBetweenFactions("zone_1", "michael", "trevor");
            var updatedState1 = _repository.GetState("michael");
            var updatedState2 = _repository.GetState("trevor");

            // Assert
            Assert.True(result);
            Assert.False(updatedState1!.OwnsZone("zone_1"));
            Assert.True(updatedState2!.OwnsZone("zone_1"));
        }

        [Fact]
        public void TransferZoneBetweenFactions_ShouldReturnFalseWhenSourceDoesNotOwn()
        {
            // Arrange
            var faction1 = CreateTestFaction("michael", "De Santa Family");
            var faction2 = CreateTestFaction("trevor", "Trevor Philips Industries");
            _repository.Add(faction1);
            _repository.Add(faction2);
            var state1 = new FactionState("michael");
            var state2 = new FactionState("trevor");
            _repository.SetState(state1);
            _repository.SetState(state2);

            // Act
            var result = _service.TransferZoneBetweenFactions("zone_1", "michael", "trevor");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TransferZoneBetweenFactions_ShouldReturnFalseWhenSourceNotFound()
        {
            // Arrange
            var faction2 = CreateTestFaction("trevor", "Trevor Philips Industries");
            _repository.Add(faction2);
            var state2 = new FactionState("trevor");
            _repository.SetState(state2);

            // Act
            var result = _service.TransferZoneBetweenFactions("zone_1", "nonexistent", "trevor");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TransferZoneBetweenFactions_ShouldReturnFalseWhenTargetNotFound()
        {
            // Arrange
            var faction1 = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction1);
            var state1 = new FactionState("michael");
            state1.AddZone("zone_1");
            _repository.SetState(state1);

            // Act
            var result = _service.TransferZoneBetweenFactions("zone_1", "michael", "nonexistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TransferZoneBetweenFactions_ShouldThrowOnNullZoneId()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.TransferZoneBetweenFactions(null!, "michael", "trevor"));
        }

        [Fact]
        public void TransferZoneBetweenFactions_ShouldThrowOnNullSourceId()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.TransferZoneBetweenFactions("zone_1", null!, "trevor"));
        }

        [Fact]
        public void TransferZoneBetweenFactions_ShouldThrowOnNullTargetId()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.TransferZoneBetweenFactions("zone_1", "michael", null!));
        }

        #endregion

        #region AddWeapons Tests

        [Fact]
        public void AddWeapons_ShouldIncreaseWeapons()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            state.Weapons = 10;
            _repository.SetState(state);

            // Act
            var result = _service.AddWeapons("michael", 5);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(15, updatedState!.Weapons);
        }

        [Fact]
        public void AddWeapons_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.AddWeapons("michael", 5);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddWeapons_ShouldThrowOnNegativeCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.AddWeapons("michael", -5));
        }

        [Fact]
        public void AddWeapons_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AddWeapons(null!, 5));
        }

        #endregion

        #region AddRecruitmentPoints Tests

        [Fact]
        public void AddRecruitmentPoints_ShouldIncreaseRecruitmentPoints()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            state.RecruitmentPoints = 50;
            _repository.SetState(state);

            // Act
            var result = _service.AddRecruitmentPoints("michael", 25);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(75, updatedState!.RecruitmentPoints);
        }

        [Fact]
        public void AddRecruitmentPoints_ShouldReturnFalseWhenNoState()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);

            // Act
            var result = _service.AddRecruitmentPoints("michael", 25);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddRecruitmentPoints_ShouldReturnFalseWhenFactionNotFound()
        {
            // Act
            var result = _service.AddRecruitmentPoints("nonexistent", 25);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddRecruitmentPoints_ShouldThrowOnNegativeAmount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.AddRecruitmentPoints("michael", -25));
        }

        [Fact]
        public void AddRecruitmentPoints_ShouldThrowOnNullId()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AddRecruitmentPoints(null!, 25));
        }

        [Fact]
        public void AddRecruitmentPoints_ShouldWorkWithZeroAmount()
        {
            // Arrange
            var faction = CreateTestFaction("michael", "De Santa Family");
            _repository.Add(faction);
            var state = new FactionState("michael");
            state.RecruitmentPoints = 50;
            _repository.SetState(state);

            // Act
            var result = _service.AddRecruitmentPoints("michael", 0);
            var updatedState = _repository.GetState("michael");

            // Assert
            Assert.True(result);
            Assert.Equal(50, updatedState!.RecruitmentPoints);
        }

        #endregion

        #region Helper Methods

        private static Faction CreateTestFaction(string id, string name)
        {
            return new Faction(id, name, leader: null, description: "Test faction");
        }

        #endregion
    }
}
