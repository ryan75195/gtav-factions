using System;
using System.Linq;
using Xunit;
using FactionWars.Escalation.Models;
using FactionWars.Escalation.Interfaces;
using FactionWars.Escalation.Repositories;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the IEscalationRepository interface and InMemoryEscalationRepository implementation.
    /// </summary>
    public class EscalationRepositoryTests
    {
        private const string FactionMichael = "faction-michael";
        private const string FactionTrevor = "faction-trevor";
        private const string FactionFranklin = "faction-franklin";

        private IEscalationRepository CreateRepository()
        {
            return new InMemoryEscalationRepository();
        }

        #region Add Tests

        [Fact]
        public void Add_NewEscalation_ReturnsTrue()
        {
            var repository = CreateRepository();
            var escalation = new FactionEscalation(FactionMichael);

            var result = repository.Add(escalation);

            Assert.True(result);
        }

        [Fact]
        public void Add_DuplicateEscalation_ReturnsFalse()
        {
            var repository = CreateRepository();
            var escalation1 = new FactionEscalation(FactionMichael);
            var escalation2 = new FactionEscalation(FactionMichael, 500);

            repository.Add(escalation1);
            var result = repository.Add(escalation2);

            Assert.False(result);
        }

        [Fact]
        public void Add_WithNull_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Add(null!));
        }

        #endregion

        #region Get Tests

        [Fact]
        public void GetByFactionId_ExistingFaction_ReturnsEscalation()
        {
            var repository = CreateRepository();
            var escalation = new FactionEscalation(FactionMichael, 500);
            repository.Add(escalation);

            var result = repository.GetByFactionId(FactionMichael);

            Assert.NotNull(result);
            Assert.Equal(FactionMichael, result.FactionId);
            Assert.Equal(500, result.Points);
        }

        [Fact]
        public void GetByFactionId_NonExistingFaction_ReturnsNull()
        {
            var repository = CreateRepository();

            var result = repository.GetByFactionId(FactionMichael);

            Assert.Null(result);
        }

        [Fact]
        public void GetByFactionId_WithNull_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.GetByFactionId(null!));
        }

        [Fact]
        public void GetByFactionId_WithEmpty_ThrowsArgumentException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentException>(() => repository.GetByFactionId(""));
        }

        [Fact]
        public void GetAll_ReturnsAllEscalations()
        {
            var repository = CreateRepository();
            repository.Add(new FactionEscalation(FactionMichael));
            repository.Add(new FactionEscalation(FactionTrevor, 1000));
            repository.Add(new FactionEscalation(FactionFranklin, 3000));

            var result = repository.GetAll().ToList();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void GetAll_EmptyRepository_ReturnsEmptyCollection()
        {
            var repository = CreateRepository();

            var result = repository.GetAll().ToList();

            Assert.Empty(result);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_ExistingEscalation_ReturnsTrue()
        {
            var repository = CreateRepository();
            var escalation = new FactionEscalation(FactionMichael, 500);
            repository.Add(escalation);

            escalation.AddPoints(500);
            var result = repository.Update(escalation);

            Assert.True(result);
        }

        [Fact]
        public void Update_NonExistingEscalation_ReturnsFalse()
        {
            var repository = CreateRepository();
            var escalation = new FactionEscalation(FactionMichael);

            var result = repository.Update(escalation);

            Assert.False(result);
        }

        [Fact]
        public void Update_WithNull_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Update(null!));
        }

        [Fact]
        public void Update_PersistsChanges()
        {
            var repository = CreateRepository();
            var escalation = new FactionEscalation(FactionMichael, 500);
            repository.Add(escalation);

            escalation.AddPoints(1000);
            repository.Update(escalation);

            var retrieved = repository.GetByFactionId(FactionMichael);
            Assert.Equal(1500, retrieved!.Points);
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_ExistingEscalation_ReturnsTrue()
        {
            var repository = CreateRepository();
            var escalation = new FactionEscalation(FactionMichael);
            repository.Add(escalation);

            var result = repository.Remove(FactionMichael);

            Assert.True(result);
        }

        [Fact]
        public void Remove_NonExistingEscalation_ReturnsFalse()
        {
            var repository = CreateRepository();

            var result = repository.Remove(FactionMichael);

            Assert.False(result);
        }

        [Fact]
        public void Remove_WithNull_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Remove(null!));
        }

        [Fact]
        public void Remove_ActuallyRemovesEscalation()
        {
            var repository = CreateRepository();
            repository.Add(new FactionEscalation(FactionMichael));

            repository.Remove(FactionMichael);

            var result = repository.GetByFactionId(FactionMichael);
            Assert.Null(result);
        }

        #endregion

        #region Exists Tests

        [Fact]
        public void Exists_ExistingFaction_ReturnsTrue()
        {
            var repository = CreateRepository();
            repository.Add(new FactionEscalation(FactionMichael));

            var result = repository.Exists(FactionMichael);

            Assert.True(result);
        }

        [Fact]
        public void Exists_NonExistingFaction_ReturnsFalse()
        {
            var repository = CreateRepository();

            var result = repository.Exists(FactionMichael);

            Assert.False(result);
        }

        [Fact]
        public void Exists_WithNull_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repository.Exists(null!));
        }

        #endregion

        #region GetOrCreate Tests

        [Fact]
        public void GetOrCreate_ExistingFaction_ReturnsExisting()
        {
            var repository = CreateRepository();
            repository.Add(new FactionEscalation(FactionMichael, 500));

            var result = repository.GetOrCreate(FactionMichael);

            Assert.Equal(500, result.Points);
        }

        [Fact]
        public void GetOrCreate_NonExistingFaction_CreatesNew()
        {
            var repository = CreateRepository();

            var result = repository.GetOrCreate(FactionMichael);

            Assert.Equal(FactionMichael, result.FactionId);
            Assert.Equal(0, result.Points);
        }

        [Fact]
        public void GetOrCreate_NonExistingFaction_AddsToRepository()
        {
            var repository = CreateRepository();

            repository.GetOrCreate(FactionMichael);

            Assert.True(repository.Exists(FactionMichael));
        }

        #endregion

        #region GetByTier Tests

        [Fact]
        public void GetByTier_ReturnsFactionsAtSpecifiedTier()
        {
            var repository = CreateRepository();
            repository.Add(new FactionEscalation(FactionMichael, 500)); // Tier1
            repository.Add(new FactionEscalation(FactionTrevor, 1500)); // Tier2
            repository.Add(new FactionEscalation(FactionFranklin, 2500)); // Tier2

            var result = repository.GetByTier(EscalationTier.Tier2).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.FactionId == FactionTrevor);
            Assert.Contains(result, e => e.FactionId == FactionFranklin);
        }

        [Fact]
        public void GetByTier_NoFactionsAtTier_ReturnsEmpty()
        {
            var repository = CreateRepository();
            repository.Add(new FactionEscalation(FactionMichael, 500)); // Tier1

            var result = repository.GetByTier(EscalationTier.Tier5).ToList();

            Assert.Empty(result);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_RemovesAllEscalations()
        {
            var repository = CreateRepository();
            repository.Add(new FactionEscalation(FactionMichael));
            repository.Add(new FactionEscalation(FactionTrevor));

            repository.Clear();

            Assert.Empty(repository.GetAll());
        }

        #endregion
    }
}
