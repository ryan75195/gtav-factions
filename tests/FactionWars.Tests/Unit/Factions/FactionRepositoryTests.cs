using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    /// <summary>
    /// Tests for IFactionRepository interface behavior and InMemoryFactionRepository implementation.
    /// These tests define the contract that any FactionRepository implementation must follow.
    /// </summary>
    public class FactionRepositoryTests
    {
        private readonly IFactionRepository _repository;

        public FactionRepositoryTests()
        {
            _repository = new InMemoryFactionRepository();
        }

        #region Add Operations

        [Fact]
        public void Add_ShouldStoreFaction()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");

            // Act
            _repository.Add(faction);
            var retrieved = _repository.GetById("faction_1");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("faction_1", retrieved!.Id);
            Assert.Equal("Michael's Crew", retrieved.Name);
        }

        [Fact]
        public void Add_ShouldThrowOnNullFaction()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Add(null!));
        }

        [Fact]
        public void Add_ShouldThrowOnDuplicateId()
        {
            // Arrange
            var faction1 = CreateTestFaction("faction_1", "Michael's Crew");
            var faction2 = CreateTestFaction("faction_1", "Trevor's Gang");
            _repository.Add(faction1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _repository.Add(faction2));
        }

        #endregion

        #region GetById Operations

        [Fact]
        public void GetById_ShouldReturnFactionWhenExists()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);

            // Act
            var retrieved = _repository.GetById("faction_1");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(faction.Id, retrieved!.Id);
        }

        [Fact]
        public void GetById_ShouldReturnNullWhenNotExists()
        {
            // Arrange - empty repository

            // Act
            var retrieved = _repository.GetById("nonexistent");

            // Assert
            Assert.Null(retrieved);
        }

        [Fact]
        public void GetById_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.GetById(null!));
        }

        [Fact]
        public void GetById_ShouldThrowOnEmptyId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _repository.GetById(""));
        }

        [Fact]
        public void GetById_ShouldThrowOnWhitespaceId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _repository.GetById("   "));
        }

        #endregion

        #region GetAll Operations

        [Fact]
        public void GetAll_ShouldReturnEmptyWhenNoFactions()
        {
            // Arrange - empty repository

            // Act
            var factions = _repository.GetAll();

            // Assert
            Assert.Empty(factions);
        }

        [Fact]
        public void GetAll_ShouldReturnAllFactions()
        {
            // Arrange
            var faction1 = CreateTestFaction("faction_1", "Michael's Crew");
            var faction2 = CreateTestFaction("faction_2", "Trevor's Gang");
            var faction3 = CreateTestFaction("faction_3", "Franklin's Family");
            _repository.Add(faction1);
            _repository.Add(faction2);
            _repository.Add(faction3);

            // Act
            var factions = _repository.GetAll().ToList();

            // Assert
            Assert.Equal(3, factions.Count);
            Assert.Contains(factions, f => f.Id == "faction_1");
            Assert.Contains(factions, f => f.Id == "faction_2");
            Assert.Contains(factions, f => f.Id == "faction_3");
        }

        #endregion

        #region Update Operations

        [Fact]
        public void Update_ShouldModifyExistingFaction()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);
            faction.IsActive = false;

            // Act
            _repository.Update(faction);
            var retrieved = _repository.GetById("faction_1");

            // Assert
            Assert.False(retrieved!.IsActive);
        }

        [Fact]
        public void Update_ShouldThrowOnNullFaction()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Update(null!));
        }

        [Fact]
        public void Update_ShouldThrowWhenFactionNotExists()
        {
            // Arrange
            var faction = CreateTestFaction("nonexistent", "Ghost Faction");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _repository.Update(faction));
        }

        #endregion

        #region Remove Operations

        [Fact]
        public void Remove_ShouldDeleteFaction()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);

            // Act
            var removed = _repository.Remove("faction_1");
            var retrieved = _repository.GetById("faction_1");

            // Assert
            Assert.True(removed);
            Assert.Null(retrieved);
        }

        [Fact]
        public void Remove_ShouldReturnFalseWhenNotExists()
        {
            // Arrange - empty repository

            // Act
            var removed = _repository.Remove("nonexistent");

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void Remove_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Remove(null!));
        }

        [Fact]
        public void Remove_ShouldAlsoRemoveAssociatedState()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);
            var state = new FactionState("faction_1", 1000, 10);
            _repository.SetState(state);

            // Act
            _repository.Remove("faction_1");
            var retrievedState = _repository.GetState("faction_1");

            // Assert
            Assert.Null(retrievedState);
        }

        #endregion

        #region Contains Operations

        [Fact]
        public void Contains_ShouldReturnTrueWhenFactionExists()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);

            // Act
            var exists = _repository.Contains("faction_1");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void Contains_ShouldReturnFalseWhenFactionNotExists()
        {
            // Arrange - empty repository

            // Act
            var exists = _repository.Contains("nonexistent");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void Contains_ShouldThrowOnNullId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.Contains(null!));
        }

        #endregion

        #region Count Operations

        [Fact]
        public void Count_ShouldReturnZeroWhenEmpty()
        {
            // Arrange - empty repository

            // Act
            var count = _repository.Count;

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void Count_ShouldReturnCorrectNumberOfFactions()
        {
            // Arrange
            _repository.Add(CreateTestFaction("faction_1", "Michael's Crew"));
            _repository.Add(CreateTestFaction("faction_2", "Trevor's Gang"));
            _repository.Add(CreateTestFaction("faction_3", "Franklin's Family"));

            // Act
            var count = _repository.Count;

            // Assert
            Assert.Equal(3, count);
        }

        #endregion

        #region Clear Operations

        [Fact]
        public void Clear_ShouldRemoveAllFactions()
        {
            // Arrange
            _repository.Add(CreateTestFaction("faction_1", "Michael's Crew"));
            _repository.Add(CreateTestFaction("faction_2", "Trevor's Gang"));

            // Act
            _repository.Clear();

            // Assert
            Assert.Equal(0, _repository.Count);
            Assert.Empty(_repository.GetAll());
        }

        [Fact]
        public void Clear_ShouldAlsoRemoveAllStates()
        {
            // Arrange
            _repository.Add(CreateTestFaction("faction_1", "Michael's Crew"));
            _repository.SetState(new FactionState("faction_1", 1000, 10));

            // Act
            _repository.Clear();

            // Assert
            Assert.Empty(_repository.GetAllStates());
        }

        #endregion

        #region GetActive Operations

        [Fact]
        public void GetActive_ShouldReturnOnlyActiveFactions()
        {
            // Arrange
            var faction1 = CreateTestFaction("faction_1", "Michael's Crew");
            var faction2 = CreateTestFaction("faction_2", "Trevor's Gang");
            var faction3 = CreateTestFaction("faction_3", "Franklin's Family");
            faction2.IsActive = false;
            _repository.Add(faction1);
            _repository.Add(faction2);
            _repository.Add(faction3);

            // Act
            var active = _repository.GetActive().ToList();

            // Assert
            Assert.Equal(2, active.Count);
            Assert.All(active, f => Assert.True(f.IsActive));
        }

        [Fact]
        public void GetActive_ShouldReturnEmptyWhenNoActiveFactions()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            faction.IsActive = false;
            _repository.Add(faction);

            // Act
            var active = _repository.GetActive();

            // Assert
            Assert.Empty(active);
        }

        #endregion

        #region GetState Operations

        [Fact]
        public void GetState_ShouldReturnNullWhenNoStateExists()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);

            // Act
            var state = _repository.GetState("faction_1");

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void GetState_ShouldThrowOnNullFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.GetState(null!));
        }

        [Fact]
        public void GetState_ShouldThrowOnEmptyFactionId()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => _repository.GetState(""));
        }

        [Fact]
        public void GetState_ShouldReturnStateWhenExists()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);
            var state = new FactionState("faction_1", 1000, 10);
            _repository.SetState(state);

            // Act
            var retrievedState = _repository.GetState("faction_1");

            // Assert
            Assert.NotNull(retrievedState);
            Assert.Equal("faction_1", retrievedState!.FactionId);
            Assert.Equal(1000, retrievedState.Cash);
            Assert.Equal(10, retrievedState.TroopCount);
        }

        #endregion

        #region SetState Operations

        [Fact]
        public void SetState_ShouldStoreState()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);
            var state = new FactionState("faction_1", 1000, 10);

            // Act
            _repository.SetState(state);
            var retrieved = _repository.GetState("faction_1");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(1000, retrieved!.Cash);
        }

        [Fact]
        public void SetState_ShouldThrowOnNullState()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _repository.SetState(null!));
        }

        [Fact]
        public void SetState_ShouldThrowWhenFactionNotExists()
        {
            // Arrange
            var state = new FactionState("nonexistent", 1000, 10);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _repository.SetState(state));
        }

        [Fact]
        public void SetState_ShouldOverwriteExistingState()
        {
            // Arrange
            var faction = CreateTestFaction("faction_1", "Michael's Crew");
            _repository.Add(faction);
            var state1 = new FactionState("faction_1", 1000, 10);
            var state2 = new FactionState("faction_1", 5000, 50);
            _repository.SetState(state1);

            // Act
            _repository.SetState(state2);
            var retrieved = _repository.GetState("faction_1");

            // Assert
            Assert.Equal(5000, retrieved!.Cash);
            Assert.Equal(50, retrieved.TroopCount);
        }

        #endregion

        #region GetAllStates Operations

        [Fact]
        public void GetAllStates_ShouldReturnEmptyWhenNoStates()
        {
            // Arrange
            _repository.Add(CreateTestFaction("faction_1", "Michael's Crew"));

            // Act
            var states = _repository.GetAllStates();

            // Assert
            Assert.Empty(states);
        }

        [Fact]
        public void GetAllStates_ShouldReturnAllStates()
        {
            // Arrange
            _repository.Add(CreateTestFaction("faction_1", "Michael's Crew"));
            _repository.Add(CreateTestFaction("faction_2", "Trevor's Gang"));
            _repository.SetState(new FactionState("faction_1", 1000, 10));
            _repository.SetState(new FactionState("faction_2", 2000, 20));

            // Act
            var states = _repository.GetAllStates().ToList();

            // Assert
            Assert.Equal(2, states.Count);
            Assert.Contains(states, s => s.FactionId == "faction_1");
            Assert.Contains(states, s => s.FactionId == "faction_2");
        }

        #endregion

        #region Helper Methods

        private static Faction CreateTestFaction(string id, string name)
        {
            return new Faction(id, name, "Test Leader", "Test Description");
        }

        #endregion
    }
}
