using FactionWars.Factions.Models;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Data transfer object for serializing Faction data.
    /// </summary>
    public class FactionData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Leader { get; set; }
        public string Description { get; set; } = string.Empty;
        public byte ColorR { get; set; }
        public byte ColorG { get; set; }
        public byte ColorB { get; set; }
        public byte ColorA { get; set; } = 255;
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Creates a FactionData from a Faction model.
        /// </summary>
        public static FactionData FromFaction(Faction faction)
        {
            return new FactionData
            {
                Id = faction.Id,
                Name = faction.Name,
                Leader = faction.Leader,
                Description = faction.Description,
                ColorR = faction.Color.R,
                ColorG = faction.Color.G,
                ColorB = faction.Color.B,
                ColorA = faction.Color.A,
                IsActive = faction.IsActive
            };
        }

        /// <summary>
        /// Converts this data object to a Faction model.
        /// </summary>
        public Faction ToFaction()
        {
            var faction = new Faction(
                Id,
                Name,
                Leader,
                Description,
                new FactionColor(ColorR, ColorG, ColorB, ColorA)
            );
            faction.IsActive = IsActive;
            return faction;
        }
    }
}
