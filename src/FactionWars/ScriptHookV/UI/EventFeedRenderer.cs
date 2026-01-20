using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// ScriptHookVDotNet implementation of the event feed renderer.
    /// Draws world events at the bottom-left of the screen with color-coding by faction.
    /// </summary>
    public class EventFeedRenderer : IEventFeedRenderer
    {
        // Position constants (screen-space coordinates 0-1)
        private const float FeedX = 0.01f;          // Left side of screen
        private const float FeedY = 0.75f;          // Bottom portion of screen
        private const float EntryHeight = 0.025f;   // Height per entry
        private const float TextScale = 0.3f;
        private const float IconScale = 0.28f;

        // Colors
        private static readonly Color DefaultColor = Color.FromArgb(220, 220, 220);    // Light gray for unknown factions
        private static readonly Color GeneralColor = Color.FromArgb(200, 200, 200);    // Gray for general events
        private static readonly Color CombatStartColor = Color.FromArgb(255, 180, 0);  // Orange for combat start
        private static readonly Color CombatEndColor = Color.FromArgb(100, 200, 100);  // Green for combat end

        private readonly IFactionRepository _factionRepository;
        private readonly int _maxDisplayCount;
        private IReadOnlyList<EventFeedEntry>? _currentEntries;
        private bool _isVisible;

        /// <inheritdoc />
        public bool IsVisible => _isVisible;

        /// <inheritdoc />
        public int MaxDisplayCount => _maxDisplayCount;

        /// <summary>
        /// Gets the current entries being displayed.
        /// </summary>
        public IReadOnlyList<EventFeedEntry>? CurrentEntries => _currentEntries;

        /// <summary>
        /// Creates a new EventFeedRenderer.
        /// </summary>
        /// <param name="factionRepository">Repository for faction lookups (for color coding).</param>
        /// <param name="maxDisplayCount">Maximum number of entries to display (default: 6).</param>
        /// <exception cref="ArgumentNullException">Thrown if factionRepository is null.</exception>
        public EventFeedRenderer(IFactionRepository factionRepository, int maxDisplayCount = 6)
        {
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _maxDisplayCount = maxDisplayCount;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Hide()
        {
            _isVisible = false;
            _currentEntries = null;
        }

        /// <summary>
        /// Draws the event feed on screen.
        /// Should be called each frame from the OnTick handler.
        /// </summary>
        public void Draw()
        {
            if (!_isVisible || _currentEntries == null || _currentEntries.Count == 0)
                return;

            // Draw entries from bottom to top (newest at bottom)
            float currentY = FeedY;

            // Iterate in reverse to show newest at the bottom
            for (int i = _currentEntries.Count - 1; i >= 0; i--)
            {
                var entry = _currentEntries[i];
                DrawEntry(entry, currentY);
                currentY -= EntryHeight;
            }
        }

        /// <summary>
        /// Draws a single entry at the specified Y position.
        /// </summary>
        private void DrawEntry(EventFeedEntry entry, float y)
        {
            // Get the appropriate color for this entry
            Color entryColor = GetEntryColor(entry);

            // Get the category icon
            string icon = GetCategoryIcon(entry.Category);

            // Format the message with icon
            string formattedMessage = $"{icon} {entry.Message}";

            // Draw the text
            DrawText(formattedMessage, FeedX, y, TextScale, entryColor, false);
        }

        /// <summary>
        /// Gets the color for an entry based on faction and category.
        /// </summary>
        private Color GetEntryColor(EventFeedEntry entry)
        {
            // For combat events, use special colors
            if (entry.Category == EventFeedCategory.CombatStarted)
                return CombatStartColor;
            if (entry.Category == EventFeedCategory.CombatEnded)
                return CombatEndColor;
            if (entry.Category == EventFeedCategory.General && entry.FactionName == null)
                return GeneralColor;

            // Try to get faction color
            var factionColor = GetFactionColor(entry.FactionName);
            if (factionColor.HasValue)
            {
                var fc = factionColor.Value;
                return Color.FromArgb(fc.R, fc.G, fc.B);
            }

            return DefaultColor;
        }

        /// <summary>
        /// Gets the faction color by faction name.
        /// </summary>
        protected FactionColor? GetFactionColor(string? factionName)
        {
            if (factionName == null)
                return null;

            var faction = _factionRepository.GetAll().FirstOrDefault(f => f.Name == factionName);
            return faction?.Color;
        }

        /// <summary>
        /// Gets the icon string for a category.
        /// </summary>
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

        /// <summary>
        /// Draws text on screen using GTA V's native text rendering.
        /// </summary>
        private void DrawText(string text, float x, float y, float scale, Color color, bool centered)
        {
            var textElement = new TextElement(text, new PointF(x * 1920f, y * 1080f), scale, color)
            {
                Alignment = centered ? Alignment.Center : Alignment.Left,
                Shadow = true,
                Outline = true
            };

            // Use screen resolution scaling for proper positioning
            textElement.ScaledDraw();
        }
    }
}
