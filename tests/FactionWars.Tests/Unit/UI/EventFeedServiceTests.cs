using Xunit;
using Moq;
using FactionWars.UI.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace FactionWars.Tests.Unit.UI
{
    public class EventFeedEntryTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsPropertiesCorrectly()
        {
            var timestamp = DateTime.UtcNow;
            var entry = new EventFeedEntry(
                "Downtown captured!",
                EventFeedCategory.ZoneCaptured,
                "Michael's Crew",
                timestamp);

            Assert.Equal("Downtown captured!", entry.Message);
            Assert.Equal(EventFeedCategory.ZoneCaptured, entry.Category);
            Assert.Equal("Michael's Crew", entry.FactionName);
            Assert.Equal(timestamp, entry.Timestamp);
            Assert.NotEqual(Guid.Empty, entry.Id);
        }

        [Fact]
        public void Constructor_WithNullMessage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EventFeedEntry(
                null!,
                EventFeedCategory.ZoneCaptured,
                "Faction",
                DateTime.UtcNow));
        }

        [Fact]
        public void Constructor_WithEmptyMessage_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new EventFeedEntry(
                "",
                EventFeedCategory.ZoneCaptured,
                "Faction",
                DateTime.UtcNow));
        }

        [Fact]
        public void Constructor_WithNullFactionName_IsValid()
        {
            var entry = new EventFeedEntry(
                "Message",
                EventFeedCategory.General,
                null,
                DateTime.UtcNow);

            Assert.Null(entry.FactionName);
        }

        [Fact]
        public void Equals_WithSameId_ReturnsTrue()
        {
            var entry = new EventFeedEntry("Message", EventFeedCategory.General, null, DateTime.UtcNow);
            Assert.True(entry.Equals(entry));
        }

        [Fact]
        public void Equals_WithDifferentEntries_ReturnsFalse()
        {
            var entry1 = new EventFeedEntry("Message", EventFeedCategory.General, null, DateTime.UtcNow);
            var entry2 = new EventFeedEntry("Message", EventFeedCategory.General, null, DateTime.UtcNow);
            Assert.False(entry1.Equals(entry2));
        }

        [Fact]
        public void GetHashCode_ReturnsIdHashCode()
        {
            var entry = new EventFeedEntry("Message", EventFeedCategory.General, null, DateTime.UtcNow);
            Assert.Equal(entry.Id.GetHashCode(), entry.GetHashCode());
        }
    }

    public class EventFeedCategoryTests
    {
        [Fact]
        public void EventFeedCategory_HasExpectedValues()
        {
            Assert.Equal(0, (int)EventFeedCategory.General);
            Assert.Equal(1, (int)EventFeedCategory.ZoneCaptured);
            Assert.Equal(2, (int)EventFeedCategory.ZoneLost);
            Assert.Equal(3, (int)EventFeedCategory.CombatStarted);
            Assert.Equal(4, (int)EventFeedCategory.CombatEnded);
            Assert.Equal(5, (int)EventFeedCategory.TroopsRecruited);
            Assert.Equal(6, (int)EventFeedCategory.TroopsDeployed);
            Assert.Equal(7, (int)EventFeedCategory.IncomeReceived);
        }
    }

    public class EventFeedServiceTests
    {
        private readonly Mock<ITimeProvider> _mockTimeProvider;
        private readonly EventFeedService _service;

        public EventFeedServiceTests()
        {
            _mockTimeProvider = new Mock<ITimeProvider>();
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
            _service = new EventFeedService(_mockTimeProvider.Object);
        }

        [Fact]
        public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EventFeedService(null!));
        }

        [Fact]
        public void Entries_IsInitiallyEmpty()
        {
            Assert.Empty(_service.Entries);
        }

        [Fact]
        public void MaxEntries_DefaultsToSix()
        {
            Assert.Equal(6, _service.MaxEntries);
        }

        [Fact]
        public void Constructor_WithCustomMaxEntries_SetsMaxEntries()
        {
            var service = new EventFeedService(_mockTimeProvider.Object, maxEntries: 4);
            Assert.Equal(4, service.MaxEntries);
        }

        [Fact]
        public void AddEntry_WithValidEntry_AddsToFeed()
        {
            var entry = new EventFeedEntry("Message", EventFeedCategory.General, null, DateTime.UtcNow);

            _service.AddEntry(entry);

            Assert.Single(_service.Entries);
            Assert.Contains(entry, _service.Entries);
        }

        [Fact]
        public void AddEntry_WithNullEntry_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.AddEntry(null!));
        }

        [Fact]
        public void AddEntry_WhenAtMaxEntries_RemovesOldestEntry()
        {
            var service = new EventFeedService(_mockTimeProvider.Object, maxEntries: 3);
            var entry1 = new EventFeedEntry("Entry 1", EventFeedCategory.General, null, DateTime.UtcNow.AddSeconds(-3));
            var entry2 = new EventFeedEntry("Entry 2", EventFeedCategory.General, null, DateTime.UtcNow.AddSeconds(-2));
            var entry3 = new EventFeedEntry("Entry 3", EventFeedCategory.General, null, DateTime.UtcNow.AddSeconds(-1));
            var entry4 = new EventFeedEntry("Entry 4", EventFeedCategory.General, null, DateTime.UtcNow);

            service.AddEntry(entry1);
            service.AddEntry(entry2);
            service.AddEntry(entry3);
            service.AddEntry(entry4);

            Assert.Equal(3, service.Entries.Count);
            Assert.DoesNotContain(entry1, service.Entries);
            Assert.Contains(entry4, service.Entries);
        }

        [Fact]
        public void AddEntry_MaintainsNewestFirstOrder()
        {
            var entry1 = new EventFeedEntry("Entry 1", EventFeedCategory.General, null, DateTime.UtcNow.AddSeconds(-2));
            var entry2 = new EventFeedEntry("Entry 2", EventFeedCategory.General, null, DateTime.UtcNow.AddSeconds(-1));
            var entry3 = new EventFeedEntry("Entry 3", EventFeedCategory.General, null, DateTime.UtcNow);

            _service.AddEntry(entry1);
            _service.AddEntry(entry2);
            _service.AddEntry(entry3);

            Assert.Equal("Entry 3", _service.Entries[0].Message);
            Assert.Equal("Entry 2", _service.Entries[1].Message);
            Assert.Equal("Entry 1", _service.Entries[2].Message);
        }

        [Fact]
        public void AddZoneCaptured_CreatesCorrectEntry()
        {
            var timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockTimeProvider.Setup(t => t.UtcNow).Returns(timestamp);

            _service.AddZoneCaptured("Downtown", "Michael's Crew");

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.ZoneCaptured, entry.Category);
            Assert.Equal("Michael's Crew", entry.FactionName);
            Assert.Contains("Downtown", entry.Message);
            Assert.Contains("Michael's Crew", entry.Message);
        }

        [Fact]
        public void AddZoneLost_CreatesCorrectEntry()
        {
            _service.AddZoneLost("Downtown", "Michael's Crew", "Trevor's Gang");

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.ZoneLost, entry.Category);
            Assert.Equal("Trevor's Gang", entry.FactionName);
            Assert.Contains("Downtown", entry.Message);
        }

        [Fact]
        public void AddCombatStarted_CreatesCorrectEntry()
        {
            _service.AddCombatStarted("Downtown", "Michael's Crew", "Trevor's Gang");

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.CombatStarted, entry.Category);
            Assert.Contains("Downtown", entry.Message);
        }

        [Fact]
        public void AddCombatEnded_CreatesCorrectEntry()
        {
            _service.AddCombatEnded("Downtown", "Michael's Crew", true);

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.CombatEnded, entry.Category);
            Assert.Contains("Downtown", entry.Message);
        }

        [Fact]
        public void AddCombatEnded_WhenDefenderWon_IncludesDefendedText()
        {
            _service.AddCombatEnded("Downtown", "Michael's Crew", true);

            var entry = _service.Entries[0];
            Assert.Contains("defended", entry.Message.ToLower());
        }

        [Fact]
        public void AddCombatEnded_WhenAttackerWon_IncludesLostText()
        {
            _service.AddCombatEnded("Downtown", "Michael's Crew", false);

            var entry = _service.Entries[0];
            Assert.Contains("lost", entry.Message.ToLower());
        }

        [Fact]
        public void AddTroopsRecruited_CreatesCorrectEntry()
        {
            _service.AddTroopsRecruited("Michael's Crew", 10);

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.TroopsRecruited, entry.Category);
            Assert.Equal("Michael's Crew", entry.FactionName);
            Assert.Contains("10", entry.Message);
        }

        [Fact]
        public void AddTroopsDeployed_CreatesCorrectEntry()
        {
            _service.AddTroopsDeployed("Downtown", "Michael's Crew", 5);

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.TroopsDeployed, entry.Category);
            Assert.Equal("Michael's Crew", entry.FactionName);
            Assert.Contains("Downtown", entry.Message);
            Assert.Contains("5", entry.Message);
        }

        [Fact]
        public void AddIncomeReceived_CreatesCorrectEntry()
        {
            _service.AddIncomeReceived("Michael's Crew", 1000);

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.IncomeReceived, entry.Category);
            Assert.Equal("Michael's Crew", entry.FactionName);
            Assert.Contains("1000", entry.Message);
        }

        [Fact]
        public void AddGeneral_CreatesCorrectEntry()
        {
            _service.AddGeneral("Some event happened");

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.General, entry.Category);
            Assert.Equal("Some event happened", entry.Message);
            Assert.Null(entry.FactionName);
        }

        [Fact]
        public void AddGeneral_WithFaction_CreatesCorrectEntry()
        {
            _service.AddGeneral("Faction event", "Michael's Crew");

            Assert.Single(_service.Entries);
            var entry = _service.Entries[0];
            Assert.Equal(EventFeedCategory.General, entry.Category);
            Assert.Equal("Michael's Crew", entry.FactionName);
        }

        [Fact]
        public void Clear_RemovesAllEntries()
        {
            _service.AddGeneral("Entry 1");
            _service.AddGeneral("Entry 2");
            _service.AddGeneral("Entry 3");

            _service.Clear();

            Assert.Empty(_service.Entries);
        }

        [Fact]
        public void GetEntriesByCategory_ReturnsMatchingEntries()
        {
            _service.AddZoneCaptured("Zone1", "Faction1");
            _service.AddGeneral("General message");
            _service.AddZoneCaptured("Zone2", "Faction2");

            var captured = _service.GetEntriesByCategory(EventFeedCategory.ZoneCaptured);

            Assert.Equal(2, captured.Count);
            Assert.All(captured, e => Assert.Equal(EventFeedCategory.ZoneCaptured, e.Category));
        }

        [Fact]
        public void GetEntriesByFaction_ReturnsMatchingEntries()
        {
            _service.AddZoneCaptured("Zone1", "Faction A");
            _service.AddZoneCaptured("Zone2", "Faction B");
            _service.AddTroopsRecruited("Faction A", 5);

            var factionEntries = _service.GetEntriesByFaction("Faction A");

            Assert.Equal(2, factionEntries.Count);
            Assert.All(factionEntries, e => Assert.Equal("Faction A", e.FactionName));
        }

        [Fact]
        public void Count_ReturnsNumberOfEntries()
        {
            _service.AddGeneral("Entry 1");
            _service.AddGeneral("Entry 2");

            Assert.Equal(2, _service.Count);
        }

        [Fact]
        public void IsEmpty_WhenNoEntries_ReturnsTrue()
        {
            Assert.True(_service.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WhenHasEntries_ReturnsFalse()
        {
            _service.AddGeneral("Entry");

            Assert.False(_service.IsEmpty);
        }
    }
}
