using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using System;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class FactionRelationshipRepositoryTests
    {
        private const string FactionA = "faction-a";
        private const string FactionB = "faction-b";
        private const string FactionC = "faction-c";

        private IFactionRelationshipRepository CreateRepository()
        {
            return new InMemoryFactionRelationshipRepository();
        }

        #region Add Tests

        [Fact]
        public void Add_WithValidRelationship_IncreasesCount()
        {
            var repo = CreateRepository();
            var relationship = new FactionRelationship(FactionA, FactionB);

            repo.Add(relationship);

            Assert.Equal(1, repo.Count);
        }

        [Fact]
        public void Add_WithNullRelationship_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repo.Add(null!));
        }

        [Fact]
        public void Add_DuplicateRelationship_ThrowsInvalidOperationException()
        {
            var repo = CreateRepository();
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionA, FactionB);
            repo.Add(r1);

            Assert.Throws<InvalidOperationException>(() => repo.Add(r2));
        }

        [Fact]
        public void Add_ReversedDuplicateRelationship_ThrowsInvalidOperationException()
        {
            var repo = CreateRepository();
            var r1 = new FactionRelationship(FactionA, FactionB);
            var r2 = new FactionRelationship(FactionB, FactionA);
            repo.Add(r1);

            Assert.Throws<InvalidOperationException>(() => repo.Add(r2));
        }

        #endregion

        #region Get Tests

        [Fact]
        public void Get_ExistingRelationship_ReturnsRelationship()
        {
            var repo = CreateRepository();
            var relationship = new FactionRelationship(FactionA, FactionB, 50);
            repo.Add(relationship);

            var result = repo.Get(FactionA, FactionB);

            Assert.NotNull(result);
            Assert.Equal(50, result.Value);
        }

        [Fact]
        public void Get_ExistingRelationshipReversed_ReturnsRelationship()
        {
            var repo = CreateRepository();
            var relationship = new FactionRelationship(FactionA, FactionB, 50);
            repo.Add(relationship);

            var result = repo.Get(FactionB, FactionA);

            Assert.NotNull(result);
            Assert.Equal(50, result.Value);
        }

        [Fact]
        public void Get_NonExistentRelationship_ReturnsNull()
        {
            var repo = CreateRepository();

            var result = repo.Get(FactionA, FactionB);

            Assert.Null(result);
        }

        [Fact]
        public void Get_WithNullFirstId_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repo.Get(null!, FactionB));
        }

        [Fact]
        public void Get_WithNullSecondId_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repo.Get(FactionA, null!));
        }

        #endregion

        #region GetByFaction Tests

        [Fact]
        public void GetByFaction_ReturnsAllRelationshipsForFaction()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB));
            repo.Add(new FactionRelationship(FactionA, FactionC));
            repo.Add(new FactionRelationship(FactionB, FactionC));

            var result = repo.GetByFaction(FactionA).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True(r.ContainsFaction(FactionA)));
        }

        [Fact]
        public void GetByFaction_NoRelationships_ReturnsEmpty()
        {
            var repo = CreateRepository();

            var result = repo.GetByFaction(FactionA).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void GetByFaction_WithNullId_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repo.GetByFaction(null!).ToList());
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_ExistingRelationship_UpdatesValue()
        {
            var repo = CreateRepository();
            var relationship = new FactionRelationship(FactionA, FactionB, 50);
            repo.Add(relationship);
            relationship.SetValue(-50);

            repo.Update(relationship);
            var result = repo.Get(FactionA, FactionB);

            Assert.Equal(-50, result!.Value);
        }

        [Fact]
        public void Update_NonExistentRelationship_ThrowsInvalidOperationException()
        {
            var repo = CreateRepository();
            var relationship = new FactionRelationship(FactionA, FactionB);

            Assert.Throws<InvalidOperationException>(() => repo.Update(relationship));
        }

        [Fact]
        public void Update_WithNull_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repo.Update(null!));
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_ExistingRelationship_ReturnsTrue()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB));

            var result = repo.Remove(FactionA, FactionB);

            Assert.True(result);
            Assert.Equal(0, repo.Count);
        }

        [Fact]
        public void Remove_ExistingRelationshipReversed_ReturnsTrue()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB));

            var result = repo.Remove(FactionB, FactionA);

            Assert.True(result);
        }

        [Fact]
        public void Remove_NonExistentRelationship_ReturnsFalse()
        {
            var repo = CreateRepository();

            var result = repo.Remove(FactionA, FactionB);

            Assert.False(result);
        }

        [Fact]
        public void Remove_WithNullFirstId_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repo.Remove(null!, FactionB));
        }

        [Fact]
        public void Remove_WithNullSecondId_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            Assert.Throws<ArgumentNullException>(() => repo.Remove(FactionA, null!));
        }

        #endregion

        #region Contains Tests

        [Fact]
        public void Contains_ExistingRelationship_ReturnsTrue()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB));

            Assert.True(repo.Contains(FactionA, FactionB));
        }

        [Fact]
        public void Contains_ExistingRelationshipReversed_ReturnsTrue()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB));

            Assert.True(repo.Contains(FactionB, FactionA));
        }

        [Fact]
        public void Contains_NonExistentRelationship_ReturnsFalse()
        {
            var repo = CreateRepository();

            Assert.False(repo.Contains(FactionA, FactionB));
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public void GetAll_ReturnsAllRelationships()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB));
            repo.Add(new FactionRelationship(FactionB, FactionC));
            repo.Add(new FactionRelationship(FactionA, FactionC));

            var result = repo.GetAll().ToList();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void GetAll_EmptyRepository_ReturnsEmpty()
        {
            var repo = CreateRepository();

            var result = repo.GetAll().ToList();

            Assert.Empty(result);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_RemovesAllRelationships()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB));
            repo.Add(new FactionRelationship(FactionB, FactionC));

            repo.Clear();

            Assert.Equal(0, repo.Count);
        }

        #endregion

        #region GetOrCreate Tests

        [Fact]
        public void GetOrCreate_ExistingRelationship_ReturnsExisting()
        {
            var repo = CreateRepository();
            repo.Add(new FactionRelationship(FactionA, FactionB, 50));

            var result = repo.GetOrCreate(FactionA, FactionB);

            Assert.Equal(50, result.Value);
        }

        [Fact]
        public void GetOrCreate_NonExistentRelationship_CreatesNew()
        {
            var repo = CreateRepository();

            var result = repo.GetOrCreate(FactionA, FactionB);

            Assert.NotNull(result);
            Assert.Equal(0, result.Value);
            Assert.Equal(1, repo.Count);
        }

        [Fact]
        public void GetOrCreate_WithDefaultValue_CreatesWithValue()
        {
            var repo = CreateRepository();

            var result = repo.GetOrCreate(FactionA, FactionB, 75);

            Assert.Equal(75, result.Value);
        }

        #endregion
    }
}
