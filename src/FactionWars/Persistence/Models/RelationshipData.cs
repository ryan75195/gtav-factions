using FactionWars.Factions.Models;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Data transfer object for serializing FactionRelationship data.
    /// </summary>
    public class RelationshipData
    {
        public string FactionId1 { get; set; } = string.Empty;
        public string FactionId2 { get; set; } = string.Empty;
        public int Value { get; set; }

        /// <summary>
        /// Creates a RelationshipData from a FactionRelationship model.
        /// </summary>
        public static RelationshipData FromFactionRelationship(FactionRelationship relationship)
        {
            return new RelationshipData
            {
                FactionId1 = relationship.FactionId1,
                FactionId2 = relationship.FactionId2,
                Value = relationship.Value
            };
        }

        /// <summary>
        /// Converts this data object to a FactionRelationship model.
        /// </summary>
        public FactionRelationship ToFactionRelationship()
        {
            return new FactionRelationship(FactionId1, FactionId2, Value);
        }
    }
}
