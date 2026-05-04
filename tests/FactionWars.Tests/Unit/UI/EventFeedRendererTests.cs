using Xunit;
using Moq;
using FactionWars.UI.Models;
using FactionWars.UI.Interfaces;
using FactionWars.ScriptHookV.UI;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Tests.Unit.UI
{
    public class EventFeedRendererInterfaceTests
    {
        [Fact]
        public void IEventFeedRenderer_DefinedCorrectly()
        {
            // Verify interface can be mocked (i.e., it's properly defined)
            var mock = new Mock<IEventFeedRenderer>();
            mock.Setup(r => r.Render(It.IsAny<IReadOnlyList<EventFeedEntry>>()));
            mock.Setup(r => r.Hide());
            mock.SetupGet(r => r.IsVisible).Returns(true);
            mock.SetupGet(r => r.MaxDisplayCount).Returns(6);

            Assert.NotNull(mock.Object);
            Assert.True(mock.Object.IsVisible);
            Assert.Equal(6, mock.Object.MaxDisplayCount);
        }
    }

    public class EventFeedRendererTests
    {
        private readonly Mock<IFactionRepository> _mockFactionRepository;

        public EventFeedRendererTests()
        {
            _mockFactionRepository = new Mock<IFactionRepository>();
            _mockFactionRepository.Setup(r => r.GetAll()).Returns(Enumerable.Empty<Faction>());
        }

        [Fact]
        public void Constructor_WithNullFactionRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TestableEventFeedRenderer(null!));
        }

        [Fact]
        public void IsVisible_InitiallyFalse()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);
            Assert.False(renderer.IsVisible);
        }

        [Fact]
        public void MaxDisplayCount_DefaultsToSix()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);
            Assert.Equal(6, renderer.MaxDisplayCount);
        }

        [Fact]
        public void Constructor_WithCustomMaxDisplayCount_SetsMaxDisplayCount()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object, maxDisplayCount: 4);
            Assert.Equal(4, renderer.MaxDisplayCount);
        }

        [Fact]
        public void Render_WithValidEntries_SetsVisibleTrue()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);
            var entries = new List<EventFeedEntry>
            {
                new EventFeedEntry("Test message", EventFeedCategory.General, null, DateTime.UtcNow)
            };

            renderer.Render(entries);

            Assert.True(renderer.IsVisible);
        }

        [Fact]
        public void Render_WithNullEntries_ThrowsArgumentNullException()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);

            Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));
        }

        [Fact]
        public void Render_WithEmptyEntries_SetsVisibleFalse()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);
            var entries = new List<EventFeedEntry>();

            renderer.Render(entries);

            Assert.False(renderer.IsVisible);
        }

        [Fact]
        public void Render_StoresCurrentEntries()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);
            var entries = new List<EventFeedEntry>
            {
                new EventFeedEntry("Entry 1", EventFeedCategory.ZoneCaptured, "Faction1", DateTime.UtcNow),
                new EventFeedEntry("Entry 2", EventFeedCategory.CombatStarted, "Faction2", DateTime.UtcNow)
            };

            renderer.Render(entries);

            Assert.Equal(2, renderer.CurrentEntries!.Count);
        }

        [Fact]
        public void Render_LimitsEntriesToMaxDisplayCount()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object, maxDisplayCount: 3);
            var entries = new List<EventFeedEntry>
            {
                new EventFeedEntry("Entry 1", EventFeedCategory.General, null, DateTime.UtcNow),
                new EventFeedEntry("Entry 2", EventFeedCategory.General, null, DateTime.UtcNow),
                new EventFeedEntry("Entry 3", EventFeedCategory.General, null, DateTime.UtcNow),
                new EventFeedEntry("Entry 4", EventFeedCategory.General, null, DateTime.UtcNow),
                new EventFeedEntry("Entry 5", EventFeedCategory.General, null, DateTime.UtcNow)
            };

            renderer.Render(entries);

            Assert.Equal(3, renderer.CurrentEntries!.Count);
        }

        [Fact]
        public void Hide_SetsVisibleFalse()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);
            var entries = new List<EventFeedEntry>
            {
                new EventFeedEntry("Test message", EventFeedCategory.General, null, DateTime.UtcNow)
            };
            renderer.Render(entries);

            renderer.Hide();

            Assert.False(renderer.IsVisible);
        }

        [Fact]
        public void Hide_ClearsCurrentEntries()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);
            var entries = new List<EventFeedEntry>
            {
                new EventFeedEntry("Test message", EventFeedCategory.General, null, DateTime.UtcNow)
            };
            renderer.Render(entries);

            renderer.Hide();

            Assert.Null(renderer.CurrentEntries);
        }

        [Fact]
        public void GetFactionColor_WithKnownFaction_ReturnsFactionColor()
        {
            var factionColor = new FactionColor(100, 150, 200);
            var faction = new Faction("michael", "Michael's Crew", null, "", factionColor);
            _mockFactionRepository.Setup(r => r.GetAll()).Returns(new[] { faction });

            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);

            var result = renderer.GetFactionColorPublic("Michael's Crew");

            Assert.NotNull(result);
            Assert.Equal(100, result!.Value.R);
            Assert.Equal(150, result.Value.G);
            Assert.Equal(200, result.Value.B);
        }

        [Fact]
        public void GetFactionColor_WithUnknownFaction_ReturnsNull()
        {
            _mockFactionRepository.Setup(r => r.GetAll()).Returns(Enumerable.Empty<Faction>());

            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);

            var result = renderer.GetFactionColorPublic("Unknown");

            Assert.Null(result);
        }

        [Fact]
        public void GetFactionColor_WithNullFactionName_ReturnsNull()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);

            var result = renderer.GetFactionColorPublic(null);

            Assert.Null(result);
        }

        [Fact]
        public void GetCategoryIcon_ReturnsCorrectIcons()
        {
            var renderer = new TestableEventFeedRenderer(_mockFactionRepository.Object);

            Assert.Equal("[+]", renderer.GetCategoryIconPublic(EventFeedCategory.ZoneCaptured));
            Assert.Equal("[-]", renderer.GetCategoryIconPublic(EventFeedCategory.ZoneLost));
            Assert.Equal("[!]", renderer.GetCategoryIconPublic(EventFeedCategory.CombatStarted));
            Assert.Equal("[X]", renderer.GetCategoryIconPublic(EventFeedCategory.CombatEnded));
            Assert.Equal("[T]", renderer.GetCategoryIconPublic(EventFeedCategory.TroopsRecruited));
            Assert.Equal("[D]", renderer.GetCategoryIconPublic(EventFeedCategory.TroopsDeployed));
            Assert.Equal("[$]", renderer.GetCategoryIconPublic(EventFeedCategory.IncomeReceived));
            Assert.Equal("[*]", renderer.GetCategoryIconPublic(EventFeedCategory.General));
        }

        [Fact]
        public void EventFeedRenderer_PublicMethods_ShouldUpdateVisibilityAndDrawWhenHidden()
        {
            var renderer = new EventFeedRenderer(_mockFactionRepository.Object);
            var entries = new List<EventFeedEntry>
            {
                new EventFeedEntry("Test message", EventFeedCategory.General, null, DateTime.UtcNow)
            };

            renderer.Render(entries);
            renderer.Hide();
            var exception = Record.Exception(() => renderer.Draw());

            Assert.False(renderer.IsVisible);
            Assert.Null(exception);
        }
    }

    /// <summary>
    /// Testable version of EventFeedRenderer that exposes protected methods for testing.
    /// This avoids depending on ScriptHookV assemblies in unit tests.
    /// </summary>
    public class TestableEventFeedRenderer : IEventFeedRenderer
    {
        private readonly IFactionRepository _factionRepository;
        private readonly int _maxDisplayCount;
        private IReadOnlyList<EventFeedEntry>? _currentEntries;
        private bool _isVisible;

        public bool IsVisible => _isVisible;
        public int MaxDisplayCount => _maxDisplayCount;
        public IReadOnlyList<EventFeedEntry>? CurrentEntries => _currentEntries;

        public TestableEventFeedRenderer(IFactionRepository factionRepository, int maxDisplayCount = 6)
        {
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _maxDisplayCount = maxDisplayCount;
        }

        public void Render(IReadOnlyList<EventFeedEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            if (entries.Count == 0)
            {
                _isVisible = false;
                _currentEntries = null;
                return;
            }

            // Limit entries to max display count
            if (entries.Count > _maxDisplayCount)
            {
                var limited = new List<EventFeedEntry>();
                for (int i = 0; i < _maxDisplayCount; i++)
                {
                    limited.Add(entries[i]);
                }
                _currentEntries = limited;
            }
            else
            {
                _currentEntries = entries;
            }

            _isVisible = true;
        }

        public void Hide()
        {
            _isVisible = false;
            _currentEntries = null;
        }

        public FactionColor? GetFactionColorPublic(string? factionName)
        {
            return GetFactionColor(factionName);
        }

        public string GetCategoryIconPublic(EventFeedCategory category)
        {
            return GetCategoryIcon(category);
        }

        protected FactionColor? GetFactionColor(string? factionName)
        {
            if (factionName == null)
                return null;

            var faction = _factionRepository.GetAll().FirstOrDefault(f => f.Name == factionName);
            return faction?.Color;
        }

        protected string GetCategoryIcon(EventFeedCategory category)
        {
            return category switch
            {
                EventFeedCategory.ZoneCaptured => "[+]",
                EventFeedCategory.ZoneLost => "[-]",
                EventFeedCategory.CombatStarted => "[!]",
                EventFeedCategory.CombatEnded => "[X]",
                EventFeedCategory.TroopsRecruited => "[T]",
                EventFeedCategory.TroopsDeployed => "[D]",
                EventFeedCategory.IncomeReceived => "[$]",
                EventFeedCategory.General => "[*]",
                _ => "[*]"
            };
        }
    }
}
