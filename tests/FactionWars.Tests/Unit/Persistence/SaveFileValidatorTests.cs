using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class SaveFileValidatorTests
    {
        private readonly ISaveFileValidator _validator;

        public SaveFileValidatorTests()
        {
            _validator = new SaveFileValidator();
        }

        #region Basic Validation

        [Fact]
        public void Validate_WithValidGameState_ShouldReturnValid()
        {
            // Arrange
            var gameState = CreateValidGameState();

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithNullGameState_ShouldReturnInvalid()
        {
            // Act
            var result = _validator.Validate(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("null"));
        }

        #endregion

        #region Version Validation

        [Fact]
        public void Validate_WithVersionZero_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Version = 0;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Version"));
        }

        [Fact]
        public void Validate_WithNegativeVersion_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Version = -1;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Version"));
        }

        [Fact]
        public void Validate_WithSupportedVersion_ShouldReturnValid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Version = 1;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithUnsupportedFutureVersion_ShouldReturnInvalidWithWarning()
        {
            // Arrange
            var validator = new SaveFileValidator(currentVersion: 1);
            var gameState = CreateValidGameState();
            gameState.Version = 999;

            // Act
            var result = validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("version") && e.Contains("not supported"));
        }

        #endregion

        #region Timestamp Validation

        [Fact]
        public void Validate_WithCreatedAtInFuture_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.CreatedAt = DateTime.UtcNow.AddDays(1);

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("CreatedAt") || e.Contains("future"));
        }

        [Fact]
        public void Validate_WithModifiedAtBeforeCreatedAt_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.CreatedAt = DateTime.UtcNow;
            gameState.ModifiedAt = gameState.CreatedAt.AddHours(-1);

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("ModifiedAt") || e.Contains("before"));
        }

        [Fact]
        public void Validate_WithNegativePlayTime_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.TotalPlayTimeSeconds = -100;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("PlayTime") || e.Contains("negative"));
        }

        #endregion

        #region Faction Validation

        [Fact]
        public void Validate_WithNullFactionsList_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions = null!;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Factions"));
        }

        [Fact]
        public void Validate_WithEmptyFactionsList_ShouldReturnValid()
        {
            // Arrange - empty game is valid (new game scenario)
            var gameState = CreateValidGameState();
            gameState.Factions = new List<FactionData>();
            gameState.FactionStates = new List<FactionStateData>();
            gameState.Zones = new List<ZoneData>();
            gameState.Relationships = new List<RelationshipData>();

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithFactionMissingId_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData
            {
                Id = null!,
                Name = "Some Faction"
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Faction") && e.Contains("Id"));
        }

        [Fact]
        public void Validate_WithFactionEmptyId_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData
            {
                Id = "",
                Name = "Some Faction"
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Faction") && e.Contains("Id"));
        }

        [Fact]
        public void Validate_WithFactionMissingName_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData
            {
                Id = "faction_test",
                Name = null!
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Faction") && e.Contains("Name"));
        }

        [Fact]
        public void Validate_WithDuplicateFactionIds_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_dup", Name = "First" });
            gameState.Factions.Add(new FactionData { Id = "faction_dup", Name = "Second" });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Faction") && e.Contains("duplicate"));
        }

        #endregion

        #region FactionState Validation

        [Fact]
        public void Validate_WithNullFactionStatesList_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.FactionStates = null!;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("FactionStates"));
        }

        [Fact]
        public void Validate_WithFactionStateMissingFactionId_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_test", Name = "Test" });
            gameState.FactionStates.Add(new FactionStateData { FactionId = null! });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("FactionState") && e.Contains("FactionId"));
        }

        [Fact]
        public void Validate_WithFactionStateReferencingNonexistentFaction_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_real", Name = "Real" });
            gameState.FactionStates.Add(new FactionStateData { FactionId = "faction_nonexistent" });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("FactionState") && e.Contains("references"));
        }

        [Fact]
        public void Validate_WithNegativeResources_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_test", Name = "Test" });
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction_test",
                Cash = -1000
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Cash") || e.Contains("negative"));
        }

        [Fact]
        public void Validate_WithNegativeTroopCount_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_test", Name = "Test" });
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction_test",
                Cash = 1000,
                TroopCount = -5
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("TroopCount") || e.Contains("negative"));
        }

        #endregion

        #region Zone Validation

        [Fact]
        public void Validate_WithNullZonesList_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Zones = null!;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Zones"));
        }

        [Fact]
        public void Validate_WithZoneMissingId_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Zones.Add(new ZoneData { Id = null!, Name = "Test Zone" });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Zone") && e.Contains("Id"));
        }

        [Fact]
        public void Validate_WithZoneMissingName_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Zones.Add(new ZoneData { Id = "zone_test", Name = null! });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Zone") && e.Contains("Name"));
        }

        [Fact]
        public void Validate_WithDuplicateZoneIds_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Zones.Add(new ZoneData { Id = "zone_dup", Name = "First" });
            gameState.Zones.Add(new ZoneData { Id = "zone_dup", Name = "Second" });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Zone") && e.Contains("duplicate"));
        }

        [Fact]
        public void Validate_WithZoneNegativeRadius_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Zones.Add(new ZoneData { Id = "zone_test", Name = "Test", Radius = -10f });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Radius"));
        }

        [Fact]
        public void Validate_WithZoneInvalidControlPercentage_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_test",
                Name = "Test",
                Radius = 100f,
                ControlPercentage = 150f // Invalid: > 100
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("ControlPercentage"));
        }

        [Fact]
        public void Validate_WithZoneNegativeControlPercentage_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_test",
                Name = "Test",
                Radius = 100f,
                ControlPercentage = -10f
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("ControlPercentage"));
        }

        [Fact]
        public void Validate_WithZoneReferencingNonexistentFaction_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_real", Name = "Real" });
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_test",
                Name = "Test",
                Radius = 100f,
                OwnerFactionId = "faction_fake"
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Zone") && e.Contains("references"));
        }

        [Fact]
        public void Validate_WithNullOwnerFactionId_ShouldReturnValid()
        {
            // Arrange - neutral zones have null owner
            var gameState = CreateValidGameState();
            gameState.Zones.Add(new ZoneData
            {
                Id = "zone_neutral",
                Name = "Neutral",
                Radius = 100f,
                OwnerFactionId = null
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.True(result.IsValid);
        }

        #endregion

        #region Relationship Validation

        [Fact]
        public void Validate_WithNullRelationshipsList_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Relationships = null!;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Relationships"));
        }

        [Fact]
        public void Validate_WithRelationshipMissingFactionId1_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_a", Name = "A" });
            gameState.Factions.Add(new FactionData { Id = "faction_b", Name = "B" });
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = null!,
                FactionId2 = "faction_b",
                Value = 0
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Relationship") && e.Contains("FactionId"));
        }

        [Fact]
        public void Validate_WithRelationshipReferencingNonexistentFaction_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_a", Name = "A" });
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = "faction_a",
                FactionId2 = "faction_nonexistent",
                Value = 0
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Relationship") && e.Contains("references"));
        }

        [Fact]
        public void Validate_WithRelationshipValueOutOfRange_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_a", Name = "A" });
            gameState.Factions.Add(new FactionData { Id = "faction_b", Name = "B" });
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = "faction_a",
                FactionId2 = "faction_b",
                Value = 150 // Out of range: should be -100 to 100
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Relationship") && e.Contains("Value"));
        }

        [Fact]
        public void Validate_WithRelationshipValueBelowRange_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_a", Name = "A" });
            gameState.Factions.Add(new FactionData { Id = "faction_b", Name = "B" });
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = "faction_a",
                FactionId2 = "faction_b",
                Value = -150
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Relationship") && e.Contains("Value"));
        }

        [Fact]
        public void Validate_WithSelfRelationship_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_a", Name = "A" });
            gameState.Relationships.Add(new RelationshipData
            {
                FactionId1 = "faction_a",
                FactionId2 = "faction_a",
                Value = 0
            });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Relationship") && e.Contains("self"));
        }

        #endregion

        #region Cross-Reference Validation

        [Fact]
        public void Validate_WithFactionStateZoneNotInZonesList_ShouldReturnInvalid()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Factions.Add(new FactionData { Id = "faction_a", Name = "A" });
            gameState.FactionStates.Add(new FactionStateData
            {
                FactionId = "faction_a",
                Cash = 1000,
                OwnedZoneIds = new List<string> { "zone_nonexistent" }
            });
            gameState.Zones.Add(new ZoneData { Id = "zone_real", Name = "Real", Radius = 100f });

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("zone") && e.Contains("does not exist"));
        }

        #endregion

        #region Validation Result

        [Fact]
        public void ValidationResult_ShouldCollectMultipleErrors()
        {
            // Arrange
            var gameState = CreateValidGameState();
            gameState.Version = -1;
            gameState.TotalPlayTimeSeconds = -100;
            gameState.Factions = null!;

            // Act
            var result = _validator.Validate(gameState);

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 2); // Multiple errors collected
        }

        #endregion

        #region Helper Methods

        private static GameState CreateValidGameState()
        {
            return new GameState
            {
                Version = 1,
                SaveName = "Test Save",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ModifiedAt = DateTime.UtcNow,
                TotalPlayTimeSeconds = 3600,
                Factions = new List<FactionData>(),
                FactionStates = new List<FactionStateData>(),
                Zones = new List<ZoneData>(),
                Relationships = new List<RelationshipData>()
            };
        }

        #endregion
    }
}
