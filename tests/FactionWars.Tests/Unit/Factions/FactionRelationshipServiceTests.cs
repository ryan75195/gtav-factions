using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class FactionRelationshipServiceTests
    {
        private const string FactionMichael = "faction-michael";
        private const string FactionTrevor = "faction-trevor";
        private const string FactionFranklin = "faction-franklin";

        private (IFactionRelationshipService service, Mock<IFactionRepository> factionRepo, IFactionRelationshipRepository relationshipRepo) CreateService()
        {
            var factionRepo = new Mock<IFactionRepository>();
            var relationshipRepo = new InMemoryFactionRelationshipRepository();
            var service = new FactionRelationshipService(factionRepo.Object, relationshipRepo);
            return (service, factionRepo, relationshipRepo);
        }

        private void SetupFactions(Mock<IFactionRepository> factionRepo)
        {
            var michael = new Faction(FactionMichael, "De Santa Crime Family");
            var trevor = new Faction(FactionTrevor, "Trevor Philips Enterprises");
            var franklin = new Faction(FactionFranklin, "Forum Gangsters");

            factionRepo.Setup(r => r.GetById(FactionMichael)).Returns(michael);
            factionRepo.Setup(r => r.GetById(FactionTrevor)).Returns(trevor);
            factionRepo.Setup(r => r.GetById(FactionFranklin)).Returns(franklin);
            factionRepo.Setup(r => r.Contains(FactionMichael)).Returns(true);
            factionRepo.Setup(r => r.Contains(FactionTrevor)).Returns(true);
            factionRepo.Setup(r => r.Contains(FactionFranklin)).Returns(true);
            factionRepo.Setup(r => r.GetAll()).Returns(new[] { michael, trevor, franklin });
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullFactionRepository_ThrowsArgumentNullException()
        {
            var relationshipRepo = new InMemoryFactionRelationshipRepository();

            Assert.Throws<ArgumentNullException>(() => new FactionRelationshipService(null!, relationshipRepo));
        }

        [Fact]
        public void Constructor_WithNullRelationshipRepository_ThrowsArgumentNullException()
        {
            var factionRepo = new Mock<IFactionRepository>();

            Assert.Throws<ArgumentNullException>(() => new FactionRelationshipService(factionRepo.Object, null!));
        }

        #endregion

        #region GetRelationship Tests

        [Fact]
        public void GetRelationship_ExistingRelationship_ReturnsRelationship()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, 50));

            var result = service.GetRelationship(FactionMichael, FactionTrevor);

            Assert.NotNull(result);
            Assert.Equal(50, result.Value);
        }

        [Fact]
        public void GetRelationship_NonExistentRelationship_ReturnsNull()
        {
            var (service, factionRepo, _) = CreateService();
            SetupFactions(factionRepo);

            var result = service.GetRelationship(FactionMichael, FactionTrevor);

            Assert.Null(result);
        }

        [Fact]
        public void GetRelationship_WithNullFirstId_ThrowsArgumentNullException()
        {
            var (service, _, _) = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetRelationship(null!, FactionTrevor));
        }

        [Fact]
        public void GetRelationship_WithNullSecondId_ThrowsArgumentNullException()
        {
            var (service, _, _) = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetRelationship(FactionMichael, null!));
        }

        #endregion

        #region GetRelationshipValue Tests

        [Fact]
        public void GetRelationshipValue_ExistingRelationship_ReturnsValue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -75));

            var result = service.GetRelationshipValue(FactionMichael, FactionTrevor);

            Assert.Equal(-75, result);
        }

        [Fact]
        public void GetRelationshipValue_NonExistentRelationship_ReturnsZero()
        {
            var (service, factionRepo, _) = CreateService();
            SetupFactions(factionRepo);

            var result = service.GetRelationshipValue(FactionMichael, FactionTrevor);

            Assert.Equal(0, result);
        }

        #endregion

        #region GetRelationshipStatus Tests

        [Fact]
        public void GetRelationshipStatus_ExistingRelationship_ReturnsStatus()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -75));

            var result = service.GetRelationshipStatus(FactionMichael, FactionTrevor);

            Assert.Equal(RelationshipStatus.War, result);
        }

        [Fact]
        public void GetRelationshipStatus_NonExistentRelationship_ReturnsNeutral()
        {
            var (service, factionRepo, _) = CreateService();
            SetupFactions(factionRepo);

            var result = service.GetRelationshipStatus(FactionMichael, FactionTrevor);

            Assert.Equal(RelationshipStatus.Neutral, result);
        }

        #endregion

        #region SetRelationshipValue Tests

        [Fact]
        public void SetRelationshipValue_ExistingRelationship_UpdatesValue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, 0));

            var result = service.SetRelationshipValue(FactionMichael, FactionTrevor, -50);

            Assert.True(result);
            Assert.Equal(-50, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        [Fact]
        public void SetRelationshipValue_NonExistentRelationship_CreatesNew()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);

            var result = service.SetRelationshipValue(FactionMichael, FactionTrevor, 75);

            Assert.True(result);
            Assert.Equal(75, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        [Fact]
        public void SetRelationshipValue_InvalidFaction_ReturnsFalse()
        {
            var (service, factionRepo, _) = CreateService();
            factionRepo.Setup(r => r.Contains("invalid")).Returns(false);
            SetupFactions(factionRepo);

            var result = service.SetRelationshipValue("invalid", FactionTrevor, 50);

            Assert.False(result);
        }

        [Fact]
        public void SetRelationshipValue_ClampsToMax()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);

            service.SetRelationshipValue(FactionMichael, FactionTrevor, 200);

            Assert.Equal(FactionRelationship.MaxValue, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        [Fact]
        public void SetRelationshipValue_ClampsToMin()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);

            service.SetRelationshipValue(FactionMichael, FactionTrevor, -200);

            Assert.Equal(FactionRelationship.MinValue, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        #endregion

        #region AdjustRelationship Tests

        [Fact]
        public void AdjustRelationship_PositiveAmount_IncreasesValue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, 0));

            var result = service.AdjustRelationship(FactionMichael, FactionTrevor, 25);

            Assert.True(result);
            Assert.Equal(25, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        [Fact]
        public void AdjustRelationship_NegativeAmount_DecreasesValue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, 50));

            var result = service.AdjustRelationship(FactionMichael, FactionTrevor, -30);

            Assert.True(result);
            Assert.Equal(20, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        [Fact]
        public void AdjustRelationship_NonExistentRelationship_CreatesFromNeutral()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);

            var result = service.AdjustRelationship(FactionMichael, FactionTrevor, -50);

            Assert.True(result);
            Assert.Equal(-50, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        [Fact]
        public void AdjustRelationship_InvalidFaction_ReturnsFalse()
        {
            var (service, factionRepo, _) = CreateService();
            factionRepo.Setup(r => r.Contains("invalid")).Returns(false);
            SetupFactions(factionRepo);

            var result = service.AdjustRelationship("invalid", FactionTrevor, 10);

            Assert.False(result);
        }

        #endregion

        #region GetAllRelationshipsForFaction Tests

        [Fact]
        public void GetAllRelationshipsForFaction_ReturnsAllRelationships()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -50));
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionFranklin, 50));

            var result = service.GetAllRelationshipsForFaction(FactionMichael).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAllRelationshipsForFaction_NoRelationships_ReturnsEmpty()
        {
            var (service, factionRepo, _) = CreateService();
            SetupFactions(factionRepo);

            var result = service.GetAllRelationshipsForFaction(FactionMichael).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void GetAllRelationshipsForFaction_WithNullId_ThrowsArgumentNullException()
        {
            var (service, _, _) = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetAllRelationshipsForFaction(null!).ToList());
        }

        #endregion

        #region GetEnemies Tests

        [Fact]
        public void GetEnemies_ReturnsFactionsWithNegativeRelationship()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -75));
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionFranklin, 50));

            var result = service.GetEnemies(FactionMichael).ToList();

            Assert.Single(result);
            Assert.Equal(FactionTrevor, result[0]);
        }

        [Fact]
        public void GetEnemies_NoEnemies_ReturnsEmpty()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, 50));

            var result = service.GetEnemies(FactionMichael).ToList();

            Assert.Empty(result);
        }

        #endregion

        #region GetAllies Tests

        [Fact]
        public void GetAllies_ReturnsFactionsWithPositiveRelationship()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -75));
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionFranklin, 60));

            var result = service.GetAllies(FactionMichael).ToList();

            Assert.Single(result);
            Assert.Equal(FactionFranklin, result[0]);
        }

        [Fact]
        public void GetAllies_NoAllies_ReturnsEmpty()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -50));

            var result = service.GetAllies(FactionMichael).ToList();

            Assert.Empty(result);
        }

        #endregion

        #region AreAtWar Tests

        [Fact]
        public void AreAtWar_WithWarStatus_ReturnsTrue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -75));

            var result = service.AreAtWar(FactionMichael, FactionTrevor);

            Assert.True(result);
        }

        [Fact]
        public void AreAtWar_WithHostileStatus_ReturnsFalse()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -40));

            var result = service.AreAtWar(FactionMichael, FactionTrevor);

            Assert.False(result);
        }

        [Fact]
        public void AreAtWar_NoRelationship_ReturnsFalse()
        {
            var (service, factionRepo, _) = CreateService();
            SetupFactions(factionRepo);

            var result = service.AreAtWar(FactionMichael, FactionTrevor);

            Assert.False(result);
        }

        #endregion

        #region AreAllied Tests

        [Fact]
        public void AreAllied_WithAlliedStatus_ReturnsTrue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionFranklin, 75));

            var result = service.AreAllied(FactionMichael, FactionFranklin);

            Assert.True(result);
        }

        [Fact]
        public void AreAllied_WithFriendlyStatus_ReturnsFalse()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionFranklin, 40));

            var result = service.AreAllied(FactionMichael, FactionFranklin);

            Assert.False(result);
        }

        #endregion

        #region InitializeAllRelationships Tests

        [Fact]
        public void InitializeAllRelationships_CreatesRelationshipsBetweenAllFactions()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);

            service.InitializeAllRelationships();

            // Should create 3 relationships: Michael-Trevor, Michael-Franklin, Trevor-Franklin
            Assert.Equal(3, relationshipRepo.Count);
        }

        [Fact]
        public void InitializeAllRelationships_WithDefaultValue_SetsValue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);

            service.InitializeAllRelationships(-25);

            var rel = relationshipRepo.Get(FactionMichael, FactionTrevor);
            Assert.Equal(-25, rel!.Value);
        }

        [Fact]
        public void InitializeAllRelationships_DoesNotOverwriteExisting()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, 100));

            service.InitializeAllRelationships(-50);

            Assert.Equal(100, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        #endregion

        #region DeclareWar Tests

        [Fact]
        public void DeclareWar_SetsRelationshipToMinimum()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, 50));

            var result = service.DeclareWar(FactionMichael, FactionTrevor);

            Assert.True(result);
            Assert.Equal(FactionRelationship.MinValue, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        [Fact]
        public void DeclareWar_NonExistentRelationship_CreatesWithMinValue()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);

            service.DeclareWar(FactionMichael, FactionTrevor);

            Assert.Equal(FactionRelationship.MinValue, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        #endregion

        #region FormAlliance Tests

        [Fact]
        public void FormAlliance_SetsRelationshipToMaximum()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionFranklin, 0));

            var result = service.FormAlliance(FactionMichael, FactionFranklin);

            Assert.True(result);
            Assert.Equal(FactionRelationship.MaxValue, relationshipRepo.Get(FactionMichael, FactionFranklin)!.Value);
        }

        #endregion

        #region MakePeace Tests

        [Fact]
        public void MakePeace_SetsRelationshipToNeutral()
        {
            var (service, factionRepo, relationshipRepo) = CreateService();
            SetupFactions(factionRepo);
            relationshipRepo.Add(new FactionRelationship(FactionMichael, FactionTrevor, -100));

            var result = service.MakePeace(FactionMichael, FactionTrevor);

            Assert.True(result);
            Assert.Equal(0, relationshipRepo.Get(FactionMichael, FactionTrevor)!.Value);
        }

        #endregion
    }
}
