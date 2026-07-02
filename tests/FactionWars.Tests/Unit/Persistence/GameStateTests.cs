using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using FactionWars.Persistence.Models;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class GameStateTests
    {
        #region Constructor and Required Properties

        [Fact]
        public void GameState_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.NotNull(gameState.Factions);
            Assert.NotNull(gameState.FactionStates);
            Assert.NotNull(gameState.Zones);
            Assert.NotNull(gameState.Relationships);
            Assert.Empty(gameState.Factions);
            Assert.Empty(gameState.FactionStates);
            Assert.Empty(gameState.Zones);
            Assert.Empty(gameState.Relationships);
        }

        [Fact]
        public void GameState_ShouldHaveVersion()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.True(gameState.Version > 0, "GameState should have a positive version number");
        }

        [Fact]
        public void GameState_ShouldHaveCreatedTimestamp()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var gameState = new GameState();

            // Assert
            Assert.True(gameState.CreatedAt >= beforeCreation);
            Assert.True(gameState.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void GameState_ShouldHaveModifiedTimestamp()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.Equal(gameState.CreatedAt, gameState.ModifiedAt);
        }

        [Fact]
        public void GameState_ShouldAllowSettingSaveName()
        {
            // Arrange
            var gameState = new GameState();

            // Act
            gameState.SaveName = "My Save";

            // Assert
            Assert.Equal("My Save", gameState.SaveName);
        }

        [Fact]
        public void GameState_ShouldHaveDefaultSaveName()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.NotNull(gameState.SaveName);
        }

        [Fact]
        public void Difficulty_DefaultsToNormal()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.Equal(Difficulty.Normal, gameState.Difficulty);
        }

        [Fact]
        public void Difficulty_CanBeSetAndRetrieved()
        {
            // Arrange
            var gameState = new GameState();

            // Act
            gameState.Difficulty = Difficulty.Hard;

            // Assert
            Assert.Equal(Difficulty.Hard, gameState.Difficulty);
        }

        #endregion

        #region Faction Data

        [Fact]
        public void GameState_ShouldStoreFactionData()
        {
            // Arrange
            var gameState = new GameState();
            var factionData = new FactionData
            {
                Id = "faction_michael",
                Name = "Michael's Crew",
                Leader = "Michael De Santa",
                Description = "A professional crew",
                ColorR = 0,
                ColorG = 100,
                ColorB = 255,
                ColorA = 255,
                IsActive = true
            };

            // Act
            gameState.Factions.Add(factionData);

            // Assert
            Assert.Single(gameState.Factions);
            Assert.Equal("faction_michael", gameState.Factions[0].Id);
        }

        [Fact]
        public void GameState_ShouldStoreMultipleFactions()
        {
            // Arrange
            var gameState = new GameState();

            // Act
            gameState.Factions.Add(new FactionData { Id = "faction_michael", Name = "Michael's Crew" });
            gameState.Factions.Add(new FactionData { Id = "faction_trevor", Name = "Trevor's Gang" });
            gameState.Factions.Add(new FactionData { Id = "faction_franklin", Name = "Franklin's Family" });

            // Assert
            Assert.Equal(3, gameState.Factions.Count);
        }

        #endregion

        #region FactionState Data

        [Fact]
        public void GameState_ShouldStoreFactionStateData()
        {
            // Arrange
            var gameState = new GameState();
            var stateData = new FactionStateData
            {
                FactionId = "faction_michael",
                Cash = 50000,
                RecruitmentPoints = 100,
                Weapons = 25,
                TroopCount = 30,
                OwnedZoneIds = new List<string> { "zone_downtown", "zone_vinewood" }
            };

            // Act
            gameState.FactionStates.Add(stateData);

            // Assert
            Assert.Single(gameState.FactionStates);
            Assert.Equal("faction_michael", gameState.FactionStates[0].FactionId);
            Assert.Equal(50000, gameState.FactionStates[0].Cash);
        }

        [Fact]
        public void GameState_ShouldStoreFactionStateWithZoneIds()
        {
            // Arrange
            var gameState = new GameState();
            var stateData = new FactionStateData
            {
                FactionId = "faction_michael",
                OwnedZoneIds = new List<string> { "zone_a", "zone_b", "zone_c" }
            };

            // Act
            gameState.FactionStates.Add(stateData);

            // Assert
            Assert.Equal(3, gameState.FactionStates[0].OwnedZoneIds.Count);
            Assert.Contains("zone_a", gameState.FactionStates[0].OwnedZoneIds);
        }

        #endregion

        #region Zone Data

        [Fact]
        public void GameState_ShouldStoreZoneData()
        {
            // Arrange
            var gameState = new GameState();
            var zoneData = new ZoneData
            {
                Id = "zone_downtown",
                Name = "Downtown Los Santos",
                CenterX = 150.5f,
                CenterY = -200.3f,
                CenterZ = 30.0f,
                Radius = 150f,
                StrategicValue = 5,
                OwnerFactionId = "faction_michael",
                ControlPercentage = 85.5f,
                IsContested = false,
                Traits = ZoneTrait.Commercial | ZoneTrait.HighValue
            };

            // Act
            gameState.Zones.Add(zoneData);

            // Assert
            Assert.Single(gameState.Zones);
            Assert.Equal("zone_downtown", gameState.Zones[0].Id);
            Assert.Equal(150.5f, gameState.Zones[0].CenterX);
        }

        [Fact]
        public void GameState_ShouldStoreZoneTraits()
        {
            // Arrange
            var gameState = new GameState();
            var zoneData = new ZoneData
            {
                Id = "zone_port",
                Name = "Port of Los Santos",
                Traits = ZoneTrait.Port | ZoneTrait.Industrial
            };

            // Act
            gameState.Zones.Add(zoneData);

            // Assert
            Assert.True(gameState.Zones[0].Traits.HasFlag(ZoneTrait.Port));
            Assert.True(gameState.Zones[0].Traits.HasFlag(ZoneTrait.Industrial));
        }

        [Fact]
        public void GameState_ShouldStoreNeutralZone()
        {
            // Arrange
            var gameState = new GameState();
            var zoneData = new ZoneData
            {
                Id = "zone_neutral",
                Name = "Neutral Territory",
                OwnerFactionId = null,
                ControlPercentage = 0f
            };

            // Act
            gameState.Zones.Add(zoneData);

            // Assert
            Assert.Null(gameState.Zones[0].OwnerFactionId);
            Assert.Equal(0f, gameState.Zones[0].ControlPercentage);
        }

        #endregion

        #region Relationship Data

        [Fact]
        public void GameState_ShouldStoreRelationshipData()
        {
            // Arrange
            var gameState = new GameState();
            var relationshipData = new RelationshipData
            {
                FactionId1 = "faction_michael",
                FactionId2 = "faction_trevor",
                Value = -30
            };

            // Act
            gameState.Relationships.Add(relationshipData);

            // Assert
            Assert.Single(gameState.Relationships);
            Assert.Equal("faction_michael", gameState.Relationships[0].FactionId1);
            Assert.Equal(-30, gameState.Relationships[0].Value);
        }

        [Fact]
        public void GameState_ShouldStoreMultipleRelationships()
        {
            // Arrange
            var gameState = new GameState();

            // Act
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "faction_michael", FactionId2 = "faction_trevor", Value = -30 });
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "faction_michael", FactionId2 = "faction_franklin", Value = 50 });
            gameState.Relationships.Add(new RelationshipData { FactionId1 = "faction_trevor", FactionId2 = "faction_franklin", Value = 0 });

            // Assert
            Assert.Equal(3, gameState.Relationships.Count);
        }

        #endregion

        #region Metadata

        [Fact]
        public void GameState_ShouldTrackPlayTime()
        {
            // Arrange
            var gameState = new GameState();

            // Act
            gameState.TotalPlayTimeSeconds = 3600;

            // Assert
            Assert.Equal(3600, gameState.TotalPlayTimeSeconds);
        }

        [Fact]
        public void GameState_ShouldHaveDefaultPlayTime()
        {
            // Arrange & Act
            var gameState = new GameState();

            // Assert
            Assert.Equal(0, gameState.TotalPlayTimeSeconds);
        }

        [Fact]
        public void GameState_ShouldUpdateModifiedTimestamp()
        {
            // Arrange
            var gameState = new GameState();
            var originalModified = gameState.ModifiedAt;
            System.Threading.Thread.Sleep(10); // Ensure time passes

            // Act
            gameState.MarkModified();

            // Assert
            Assert.True(gameState.ModifiedAt > originalModified);
        }

        #endregion

        #region Data Transfer Objects

        [Fact]
        public void FactionData_ShouldHaveAllProperties()
        {
            // Arrange & Act
            var data = new FactionData
            {
                Id = "test_id",
                Name = "Test Name",
                Leader = "Test Leader",
                Description = "Test Description",
                ColorR = 100,
                ColorG = 150,
                ColorB = 200,
                ColorA = 255,
                IsActive = true
            };

            // Assert
            Assert.Equal("test_id", data.Id);
            Assert.Equal("Test Name", data.Name);
            Assert.Equal("Test Leader", data.Leader);
            Assert.Equal("Test Description", data.Description);
            Assert.Equal(100, data.ColorR);
            Assert.Equal(150, data.ColorG);
            Assert.Equal(200, data.ColorB);
            Assert.Equal(255, data.ColorA);
            Assert.True(data.IsActive);
        }

        [Fact]
        public void FactionStateData_ShouldHaveAllProperties()
        {
            // Arrange & Act
            var data = new FactionStateData
            {
                FactionId = "test_faction",
                Cash = 10000,
                RecruitmentPoints = 50,
                Weapons = 20,
                TroopCount = 100,
                OwnedZoneIds = new List<string> { "zone1", "zone2" }
            };

            // Assert
            Assert.Equal("test_faction", data.FactionId);
            Assert.Equal(10000, data.Cash);
            Assert.Equal(50, data.RecruitmentPoints);
            Assert.Equal(20, data.Weapons);
            Assert.Equal(100, data.TroopCount);
            Assert.Equal(2, data.OwnedZoneIds.Count);
        }

        [Fact]
        public void ZoneData_ShouldHaveAllProperties()
        {
            // Arrange & Act
            var data = new ZoneData
            {
                Id = "zone_test",
                Name = "Test Zone",
                CenterX = 100f,
                CenterY = 200f,
                CenterZ = 50f,
                Radius = 150f,
                StrategicValue = 7,
                OwnerFactionId = "faction_test",
                ControlPercentage = 75.5f,
                IsContested = true,
                Traits = ZoneTrait.Residential
            };

            // Assert
            Assert.Equal("zone_test", data.Id);
            Assert.Equal("Test Zone", data.Name);
            Assert.Equal(100f, data.CenterX);
            Assert.Equal(200f, data.CenterY);
            Assert.Equal(50f, data.CenterZ);
            Assert.Equal(150f, data.Radius);
            Assert.Equal(7, data.StrategicValue);
            Assert.Equal("faction_test", data.OwnerFactionId);
            Assert.Equal(75.5f, data.ControlPercentage);
            Assert.True(data.IsContested);
            Assert.Equal(ZoneTrait.Residential, data.Traits);
        }

        [Fact]
        public void RelationshipData_ShouldHaveAllProperties()
        {
            // Arrange & Act
            var data = new RelationshipData
            {
                FactionId1 = "faction_a",
                FactionId2 = "faction_b",
                Value = -75
            };

            // Assert
            Assert.Equal("faction_a", data.FactionId1);
            Assert.Equal("faction_b", data.FactionId2);
            Assert.Equal(-75, data.Value);
        }

        [Fact]
        public void FactionStateData_ShouldHaveEmptyZoneListByDefault()
        {
            // Arrange & Act
            var data = new FactionStateData();

            // Assert
            Assert.NotNull(data.OwnedZoneIds);
            Assert.Empty(data.OwnedZoneIds);
        }

        #endregion

        #region Model Conversion (ToModel / FromModel)

        [Fact]
        public void FactionData_FromFaction_ShouldConvertCorrectly()
        {
            // Arrange
            var faction = new Faction(
                "faction_michael",
                "Michael's Crew",
                "Michael De Santa",
                "A professional crew",
                new FactionColor(100, 150, 200)
            );
            faction.IsActive = true;

            // Act
            var data = FactionData.FromFaction(faction);

            // Assert
            Assert.Equal("faction_michael", data.Id);
            Assert.Equal("Michael's Crew", data.Name);
            Assert.Equal("Michael De Santa", data.Leader);
            Assert.Equal("A professional crew", data.Description);
            Assert.Equal(100, data.ColorR);
            Assert.Equal(150, data.ColorG);
            Assert.Equal(200, data.ColorB);
            Assert.Equal(255, data.ColorA);
            Assert.True(data.IsActive);
        }

        [Fact]
        public void FactionData_ToFaction_ShouldConvertCorrectly()
        {
            // Arrange
            var data = new FactionData
            {
                Id = "faction_michael",
                Name = "Michael's Crew",
                Leader = "Michael De Santa",
                Description = "A professional crew",
                ColorR = 100,
                ColorG = 150,
                ColorB = 200,
                ColorA = 255,
                IsActive = true
            };

            // Act
            var faction = data.ToFaction();

            // Assert
            Assert.Equal("faction_michael", faction.Id);
            Assert.Equal("Michael's Crew", faction.Name);
            Assert.Equal("Michael De Santa", faction.Leader);
            Assert.Equal("A professional crew", faction.Description);
            Assert.Equal(100, faction.Color.R);
            Assert.Equal(150, faction.Color.G);
            Assert.Equal(200, faction.Color.B);
            Assert.True(faction.IsActive);
        }

        [Fact]
        public void FactionStateData_FromFactionState_ShouldConvertCorrectly()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 50000, initialTroopCount: 30);
            state.RecruitmentPoints = 100;
            state.Weapons = 25;
            state.AddZone("zone_downtown");
            state.AddZone("zone_vinewood");

            // Act
            var data = FactionStateData.FromFactionState(state);

            // Assert
            Assert.Equal("faction_michael", data.FactionId);
            Assert.Equal(50000, data.Cash);
            Assert.Equal(100, data.RecruitmentPoints);
            Assert.Equal(25, data.Weapons);
            Assert.Equal(30, data.TroopCount);
            Assert.Equal(2, data.OwnedZoneIds.Count);
            Assert.Contains("zone_downtown", data.OwnedZoneIds);
            Assert.Contains("zone_vinewood", data.OwnedZoneIds);
        }

        [Fact]
        public void FactionStateData_ToFactionState_ShouldConvertCorrectly()
        {
            // Arrange
            var data = new FactionStateData
            {
                FactionId = "faction_michael",
                Cash = 50000,
                RecruitmentPoints = 100,
                Weapons = 25,
                TroopCount = 30,
                OwnedZoneIds = new List<string> { "zone_downtown", "zone_vinewood" }
            };

            // Act
            var state = data.ToFactionState();

            // Assert
            Assert.Equal("faction_michael", state.FactionId);
            Assert.Equal(50000, state.Cash);
            Assert.Equal(100, state.RecruitmentPoints);
            Assert.Equal(25, state.Weapons);
            Assert.Equal(30, state.TroopCount);
            Assert.Equal(2, state.ZoneCount);
            Assert.True(state.OwnsZone("zone_downtown"));
            Assert.True(state.OwnsZone("zone_vinewood"));
        }

        [Fact]
        public void FactionStateData_FromFactionState_ShouldConvertSupportSquadPackages()
        {
            // Arrange
            var state = new FactionState("faction_michael", initialCash: 50000, initialTroopCount: 30);
            state.RecruitmentPoints = 100;
            state.Weapons = 25;
            state.AddSupportSquadPackage(3);
            state.AddZone("zone_downtown");

            // Act
            var data = FactionStateData.FromFactionState(state);

            // Assert
            Assert.Equal(3, data.SupportSquadPackages);
            Assert.Equal("faction_michael", data.FactionId);
            Assert.Equal(50000, data.Cash);
        }

        [Fact]
        public void FactionStateData_ToFactionState_ShouldRestoreSupportSquadPackages()
        {
            // Arrange
            var data = new FactionStateData
            {
                FactionId = "faction_michael",
                Cash = 50000,
                RecruitmentPoints = 100,
                Weapons = 25,
                TroopCount = 30,
                SupportSquadPackages = 3,
                OwnedZoneIds = new List<string> { "zone_downtown" }
            };

            // Act
            var state = data.ToFactionState();

            // Assert
            Assert.Equal(3, state.SupportSquadPackages);
            Assert.Equal("faction_michael", state.FactionId);
            Assert.Equal(50000, state.Cash);
        }

        [Fact]
        public void FactionStateData_RoundTrip_ShouldPreserveSupportSquadPackages()
        {
            // Arrange
            var original = new FactionState("faction_michael", initialCash: 50000, initialTroopCount: 30);
            original.RecruitmentPoints = 100;
            original.Weapons = 25;
            original.AddSupportSquadPackage(3);
            original.AddZone("zone_downtown");
            original.AddZone("zone_vinewood");

            // Act
            var data = FactionStateData.FromFactionState(original);
            var restored = data.ToFactionState();

            // Assert
            Assert.Equal(3, restored.SupportSquadPackages);
            Assert.Equal("faction_michael", restored.FactionId);
            Assert.Equal(50000, restored.Cash);
            Assert.Equal(100, restored.RecruitmentPoints);
            Assert.Equal(25, restored.Weapons);
            Assert.Equal(30, restored.TroopCount);
            Assert.Equal(2, restored.ZoneCount);
            Assert.True(restored.OwnsZone("zone_downtown"));
            Assert.True(restored.OwnsZone("zone_vinewood"));
        }

        [Fact]
        public void FactionStateData_LegacySave_ShouldDefaultSupportSquadPackagesToZero()
        {
            // Arrange - Legacy save without SupportSquadPackages field
            var data = new FactionStateData
            {
                FactionId = "faction_michael",
                Cash = 50000,
                RecruitmentPoints = 100,
                Weapons = 25,
                TroopCount = 30,
                OwnedZoneIds = new List<string> { "zone_downtown" }
            };
            // SupportSquadPackages not set, defaults to 0

            // Act
            var state = data.ToFactionState();

            // Assert
            Assert.Equal(0, state.SupportSquadPackages);
            Assert.Equal("faction_michael", state.FactionId);
            Assert.Equal(50000, state.Cash);
        }

        [Fact]
        public void ZoneData_FromZone_ShouldConvertCorrectly()
        {
            // Arrange
            var zone = new Zone(
                "zone_downtown",
                "Downtown Los Santos",
                new Vector3(150.5f, -200.3f, 30.0f),
                150f,
                5
            );
            zone.OwnerFactionId = "faction_michael";
            zone.ControlPercentage = 85.5f;
            zone.IsContested = false;
            zone.Traits = ZoneTrait.Commercial | ZoneTrait.HighValue;

            // Act
            var data = ZoneData.FromZone(zone);

            // Assert
            Assert.Equal("zone_downtown", data.Id);
            Assert.Equal("Downtown Los Santos", data.Name);
            Assert.Equal(150.5f, data.CenterX);
            Assert.Equal(-200.3f, data.CenterY);
            Assert.Equal(30.0f, data.CenterZ);
            Assert.Equal(150f, data.Radius);
            Assert.Equal(5, data.StrategicValue);
            Assert.Equal("faction_michael", data.OwnerFactionId);
            Assert.Equal(85.5f, data.ControlPercentage);
            Assert.False(data.IsContested);
            Assert.Equal(ZoneTrait.Commercial | ZoneTrait.HighValue, data.Traits);
        }

        [Fact]
        public void ZoneData_ToZone_ShouldConvertCorrectly()
        {
            // Arrange
            var data = new ZoneData
            {
                Id = "zone_downtown",
                Name = "Downtown Los Santos",
                CenterX = 150.5f,
                CenterY = -200.3f,
                CenterZ = 30.0f,
                Radius = 150f,
                StrategicValue = 5,
                OwnerFactionId = "faction_michael",
                ControlPercentage = 85.5f,
                IsContested = false,
                Traits = ZoneTrait.Commercial | ZoneTrait.HighValue
            };

            // Act
            var zone = data.ToZone();

            // Assert
            Assert.Equal("zone_downtown", zone.Id);
            Assert.Equal("Downtown Los Santos", zone.Name);
            Assert.Equal(150.5f, zone.Center.X);
            Assert.Equal(-200.3f, zone.Center.Y);
            Assert.Equal(30.0f, zone.Center.Z);
            Assert.Equal(150f, zone.Radius);
            Assert.Equal(5, zone.StrategicValue);
            Assert.Equal("faction_michael", zone.OwnerFactionId);
            Assert.Equal(85.5f, zone.ControlPercentage);
            Assert.False(zone.IsContested);
            Assert.Equal(ZoneTrait.Commercial | ZoneTrait.HighValue, zone.Traits);
        }

        [Fact]
        public void RelationshipData_FromFactionRelationship_ShouldConvertCorrectly()
        {
            // Arrange
            var relationship = new FactionRelationship("faction_michael", "faction_trevor", -30);

            // Act
            var data = RelationshipData.FromFactionRelationship(relationship);

            // Assert
            Assert.Equal("faction_michael", data.FactionId1);
            Assert.Equal("faction_trevor", data.FactionId2);
            Assert.Equal(-30, data.Value);
        }

        [Fact]
        public void RelationshipData_ToFactionRelationship_ShouldConvertCorrectly()
        {
            // Arrange
            var data = new RelationshipData
            {
                FactionId1 = "faction_michael",
                FactionId2 = "faction_trevor",
                Value = -30
            };

            // Act
            var relationship = data.ToFactionRelationship();

            // Assert
            Assert.Equal("faction_michael", relationship.FactionId1);
            Assert.Equal("faction_trevor", relationship.FactionId2);
            Assert.Equal(-30, relationship.Value);
        }

        #endregion

        #region GameState Snapshot Creation

        [Fact]
        public void GameState_CreateSnapshot_ShouldCaptureAllFactions()
        {
            // Arrange
            var factions = new List<Faction>
            {
                new Faction("faction_michael", "Michael's Crew"),
                new Faction("faction_trevor", "Trevor's Gang")
            };
            var states = new List<FactionState>();
            var zones = new List<Zone>();
            var relationships = new List<FactionRelationship>();

            // Act
            var gameState = GameState.CreateSnapshot(factions, states, zones, relationships);

            // Assert
            Assert.Equal(2, gameState.Factions.Count);
        }

        [Fact]
        public void GameState_CreateSnapshot_ShouldCaptureAllZones()
        {
            // Arrange
            var factions = new List<Faction>();
            var states = new List<FactionState>();
            var zones = new List<Zone>
            {
                new Zone("zone_a", "Zone A", new Vector3(0, 0, 0)),
                new Zone("zone_b", "Zone B", new Vector3(100, 100, 0)),
                new Zone("zone_c", "Zone C", new Vector3(200, 200, 0))
            };
            var relationships = new List<FactionRelationship>();

            // Act
            var gameState = GameState.CreateSnapshot(factions, states, zones, relationships);

            // Assert
            Assert.Equal(3, gameState.Zones.Count);
        }

        [Fact]
        public void GameState_CreateSnapshot_ShouldCaptureAllFactionStates()
        {
            // Arrange
            var factions = new List<Faction>();
            var states = new List<FactionState>
            {
                new FactionState("faction_michael", initialCash: 10000),
                new FactionState("faction_trevor", initialCash: 5000)
            };
            var zones = new List<Zone>();
            var relationships = new List<FactionRelationship>();

            // Act
            var gameState = GameState.CreateSnapshot(factions, states, zones, relationships);

            // Assert
            Assert.Equal(2, gameState.FactionStates.Count);
        }

        [Fact]
        public void GameState_CreateSnapshot_ShouldCaptureAllRelationships()
        {
            // Arrange
            var factions = new List<Faction>();
            var states = new List<FactionState>();
            var zones = new List<Zone>();
            var relationships = new List<FactionRelationship>
            {
                new FactionRelationship("faction_michael", "faction_trevor", -30),
                new FactionRelationship("faction_michael", "faction_franklin", 50)
            };

            // Act
            var gameState = GameState.CreateSnapshot(factions, states, zones, relationships);

            // Assert
            Assert.Equal(2, gameState.Relationships.Count);
        }

        #endregion
    }
}
