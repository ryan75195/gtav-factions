using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using FactionWars.Loyalty.Repositories;
using FactionWars.Loyalty.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Loyalty
{
    /// <summary>
    /// Tests for the captured zone integration coordinator that manages the full
    /// lifecycle of integrating captured zones into the controlling faction.
    /// </summary>
    public class CapturedZoneIntegrationTests
    {
        #region IZoneIntegrationRepository Interface Tests

        [Fact]
        public void ZoneIntegrationRepository_Add_ShouldStoreIntegrationState()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");

            // Act
            repository.Add(state);
            var retrieved = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("zone_downtown", retrieved.ZoneId);
        }

        [Fact]
        public void ZoneIntegrationRepository_Add_ShouldThrowOnNull()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => repository.Add(null!));
        }

        [Fact]
        public void ZoneIntegrationRepository_Add_ShouldThrowOnDuplicateZoneId()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            var state1 = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");
            var state2 = new ZoneIntegrationState("zone_downtown", "faction_franklin", "faction_michael");
            repository.Add(state1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => repository.Add(state2));
        }

        [Fact]
        public void ZoneIntegrationRepository_GetByZoneId_ShouldReturnNullForUnknownZone()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();

            // Act
            var result = repository.GetByZoneId("unknown_zone");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ZoneIntegrationRepository_GetByZoneId_ShouldThrowOnNullId()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => repository.GetByZoneId(null!));
        }

        [Fact]
        public void ZoneIntegrationRepository_Remove_ShouldRemoveState()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor");
            repository.Add(state);

            // Act
            var removed = repository.Remove("zone_downtown");
            var retrieved = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.True(removed);
            Assert.Null(retrieved);
        }

        [Fact]
        public void ZoneIntegrationRepository_Remove_ShouldReturnFalseForUnknownZone()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();

            // Act
            var removed = repository.Remove("unknown_zone");

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void ZoneIntegrationRepository_GetAll_ShouldReturnAllStates()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            repository.Add(new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor"));
            repository.Add(new ZoneIntegrationState("zone_b", "faction_franklin", "faction_michael"));
            repository.Add(new ZoneIntegrationState("zone_c", "faction_trevor", "faction_franklin"));

            // Act
            var all = repository.GetAll().ToList();

            // Assert
            Assert.Equal(3, all.Count);
        }

        [Fact]
        public void ZoneIntegrationRepository_GetByFaction_ShouldReturnStatesForFaction()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            repository.Add(new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor"));
            repository.Add(new ZoneIntegrationState("zone_b", "faction_michael", "faction_franklin"));
            repository.Add(new ZoneIntegrationState("zone_c", "faction_trevor", "faction_franklin"));

            // Act
            var michaelZones = repository.GetByFaction("faction_michael").ToList();

            // Assert
            Assert.Equal(2, michaelZones.Count);
            Assert.All(michaelZones, z => Assert.Equal("faction_michael", z.NewControllerFactionId));
        }

        [Fact]
        public void ZoneIntegrationRepository_GetPendingIntegration_ShouldReturnOnlyIncompleteStates()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            var incomplete = new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor", initialProgress: 50);
            var complete = new ZoneIntegrationState("zone_b", "faction_michael", "faction_franklin", initialProgress: 100);
            repository.Add(incomplete);
            repository.Add(complete);

            // Act
            var pending = repository.GetPendingIntegration().ToList();

            // Assert
            Assert.Single(pending);
            Assert.Equal("zone_a", pending[0].ZoneId);
        }

        [Fact]
        public void ZoneIntegrationRepository_Update_ShouldPersistChanges()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 0);
            repository.Add(state);

            // Act
            state.AddProgress(25);
            repository.Update(state);
            var retrieved = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(25, retrieved.IntegrationProgress);
        }

        [Fact]
        public void ZoneIntegrationRepository_Update_ShouldThrowForUnknownZone()
        {
            // Arrange
            var repository = new InMemoryZoneIntegrationRepository();
            var state = new ZoneIntegrationState("unknown_zone", "faction_michael", "faction_trevor");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => repository.Update(state));
        }

        #endregion

        #region ICapturedZoneIntegrationManager Interface Tests

        [Fact]
        public void CapturedZoneIntegrationManager_OnZoneCaptured_ShouldCreateIntegrationState()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30, previousFactionId: "faction_trevor");

            // Act
            manager.OnZoneCaptured(loyalty);
            var state = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.NotNull(state);
            Assert.Equal("faction_michael", state.NewControllerFactionId);
            Assert.Equal("faction_trevor", state.PreviousControllerFactionId);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_OnZoneCaptured_ShouldThrowOnNull()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.OnZoneCaptured(null!));
        }

        [Fact]
        public void CapturedZoneIntegrationManager_OnZoneCaptured_ShouldThrowWithoutPreviousFaction()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);
            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 50);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => manager.OnZoneCaptured(loyalty));
        }

        [Fact]
        public void CapturedZoneIntegrationManager_OnZoneCaptured_ShouldReplaceExistingIntegrationState()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var firstLoyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30, previousFactionId: "faction_trevor");
            manager.OnZoneCaptured(firstLoyalty);

            var secondLoyalty = new ZoneLoyalty("zone_downtown", "faction_franklin", initialLoyalty: 25, previousFactionId: "faction_michael");

            // Act
            manager.OnZoneCaptured(secondLoyalty);
            var state = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.NotNull(state);
            Assert.Equal("faction_franklin", state.NewControllerFactionId);
            Assert.Equal("faction_michael", state.PreviousControllerFactionId);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_ProcessDailyTick_ShouldAdvanceAllPendingIntegrations()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var loyalty1 = new ZoneLoyalty("zone_a", "faction_michael", initialLoyalty: 50, previousFactionId: "faction_trevor");
            var loyalty2 = new ZoneLoyalty("zone_b", "faction_michael", initialLoyalty: 50, previousFactionId: "faction_franklin");
            manager.OnZoneCaptured(loyalty1);
            manager.OnZoneCaptured(loyalty2);

            // Act
            var results = manager.ProcessDailyTick();

            // Assert
            Assert.Equal(2, results.Count());
            Assert.All(repository.GetAll(), state => Assert.True(state.IntegrationProgress > 0));
            Assert.All(repository.GetAll(), state => Assert.Equal(1, state.DaysSinceCapture));
        }

        [Fact]
        public void CapturedZoneIntegrationManager_ProcessDailyTick_ShouldNotProcessFullyIntegratedZones()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            // Manually add a fully integrated zone
            var state = new ZoneIntegrationState("zone_complete", "faction_michael", "faction_trevor", initialProgress: 100);
            repository.Add(state);

            // Act
            var results = manager.ProcessDailyTick();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_ProcessDailyTick_ShouldReturnProcessingResults()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 70, previousFactionId: "faction_trevor");
            manager.OnZoneCaptured(loyalty);

            // Act
            var results = manager.ProcessDailyTick().ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal("zone_downtown", results[0].ZoneId);
            Assert.True(results[0].ProgressGained > 0);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_GetResourceMultiplier_ShouldReturnPenaltyForIntegratingZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30, previousFactionId: "faction_trevor");
            manager.OnZoneCaptured(loyalty);

            // Act
            var multiplier = manager.GetResourceMultiplier("zone_downtown");

            // Assert - New zone should have penalty (0.25 at 0% integration)
            Assert.Equal(0.25f, multiplier, 3);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_GetResourceMultiplier_ShouldReturnFullForUnknownZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            // Act
            var multiplier = manager.GetResourceMultiplier("unknown_zone");

            // Assert - Non-integrating zones produce full resources
            Assert.Equal(1.0f, multiplier, 3);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_GetDefenseModifier_ShouldReturnPenaltyForNewlyIntegratingZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30, previousFactionId: "faction_trevor");
            manager.OnZoneCaptured(loyalty);

            // Act
            var modifier = manager.GetDefenseModifier("zone_downtown");

            // Assert - 0% progress gives -15 defense
            Assert.Equal(-15, modifier);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_GetDefenseModifier_ShouldReturnZeroForUnknownZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            // Act
            var modifier = manager.GetDefenseModifier("unknown_zone");

            // Assert
            Assert.Equal(0, modifier);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_IsZoneIntegrating_ShouldReturnTrueForIntegratingZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var loyalty = new ZoneLoyalty("zone_downtown", "faction_michael", initialLoyalty: 30, previousFactionId: "faction_trevor");
            manager.OnZoneCaptured(loyalty);

            // Act & Assert
            Assert.True(manager.IsZoneIntegrating("zone_downtown"));
        }

        [Fact]
        public void CapturedZoneIntegrationManager_IsZoneIntegrating_ShouldReturnFalseForUnknownZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            // Act & Assert
            Assert.False(manager.IsZoneIntegrating("unknown_zone"));
        }

        [Fact]
        public void CapturedZoneIntegrationManager_IsZoneIntegrating_ShouldReturnFalseForFullyIntegrated()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var state = new ZoneIntegrationState("zone_complete", "faction_michael", "faction_trevor", initialProgress: 100);
            repository.Add(state);

            // Act & Assert
            Assert.False(manager.IsZoneIntegrating("zone_complete"));
        }

        [Fact]
        public void CapturedZoneIntegrationManager_GetIntegrationProgress_ShouldReturnCurrentProgress()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 45);
            repository.Add(state);

            // Act
            var progress = manager.GetIntegrationProgress("zone_downtown");

            // Assert
            Assert.Equal(45, progress);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_GetIntegrationProgress_ShouldReturn100ForUnknownZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            // Act
            var progress = manager.GetIntegrationProgress("unknown_zone");

            // Assert - Unknown zones are considered fully integrated
            Assert.Equal(100, progress);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_OnInsurgencyOccurred_ShouldReduceIntegrationProgress()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);
            repository.Add(state);

            // Act
            manager.OnInsurgencyOccurred("zone_downtown", InsurgencyLevel.High);
            var retrieved = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.NotNull(retrieved);
            Assert.True(retrieved.IntegrationProgress < 50);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_OnInsurgencyOccurred_ShouldDoNothingForUnknownZone()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            // Act & Assert - Should not throw
            manager.OnInsurgencyOccurred("unknown_zone", InsurgencyLevel.High);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_CompleteIntegration_ShouldRemoveFromRepository()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 100);
            repository.Add(state);

            // Act
            var completed = manager.CompleteIntegration("zone_downtown");
            var retrieved = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.True(completed);
            Assert.Null(retrieved);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_CompleteIntegration_ShouldReturnFalseForIncomplete()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            var state = new ZoneIntegrationState("zone_downtown", "faction_michael", "faction_trevor", initialProgress: 50);
            repository.Add(state);

            // Act
            var completed = manager.CompleteIntegration("zone_downtown");
            var retrieved = repository.GetByZoneId("zone_downtown");

            // Assert
            Assert.False(completed);
            Assert.NotNull(retrieved);
        }

        [Fact]
        public void CapturedZoneIntegrationManager_GetIntegratingZonesForFaction_ShouldReturnCorrectZones()
        {
            // Arrange
            var integrationService = new ZoneIntegrationService();
            var repository = new InMemoryZoneIntegrationRepository();
            var manager = new CapturedZoneIntegrationManager(integrationService, repository);

            repository.Add(new ZoneIntegrationState("zone_a", "faction_michael", "faction_trevor", initialProgress: 30));
            repository.Add(new ZoneIntegrationState("zone_b", "faction_michael", "faction_franklin", initialProgress: 50));
            repository.Add(new ZoneIntegrationState("zone_c", "faction_trevor", "faction_franklin", initialProgress: 40));

            // Act
            var michaelZones = manager.GetIntegratingZonesForFaction("faction_michael").ToList();

            // Assert
            Assert.Equal(2, michaelZones.Count);
            Assert.Contains("zone_a", michaelZones);
            Assert.Contains("zone_b", michaelZones);
        }

        #endregion

        #region IntegrationTickResult Tests

        [Fact]
        public void IntegrationTickResult_ShouldContainZoneId()
        {
            // Arrange & Act
            var result = new IntegrationTickResult("zone_downtown", 5, 25, false);

            // Assert
            Assert.Equal("zone_downtown", result.ZoneId);
        }

        [Fact]
        public void IntegrationTickResult_ShouldContainProgressGained()
        {
            // Arrange & Act
            var result = new IntegrationTickResult("zone_downtown", 5, 25, false);

            // Assert
            Assert.Equal(5, result.ProgressGained);
        }

        [Fact]
        public void IntegrationTickResult_ShouldContainNewProgress()
        {
            // Arrange & Act
            var result = new IntegrationTickResult("zone_downtown", 5, 25, false);

            // Assert
            Assert.Equal(25, result.NewProgress);
        }

        [Fact]
        public void IntegrationTickResult_ShouldIndicateIfJustCompleted()
        {
            // Arrange & Act
            var completed = new IntegrationTickResult("zone_downtown", 5, 100, true);
            var incomplete = new IntegrationTickResult("zone_downtown", 5, 50, false);

            // Assert
            Assert.True(completed.JustCompleted);
            Assert.False(incomplete.JustCompleted);
        }

        #endregion
    }
}
