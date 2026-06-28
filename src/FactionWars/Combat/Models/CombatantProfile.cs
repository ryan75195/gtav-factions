using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// The canonical presentation + allegiance of a zone combatant, produced by the single
    /// allegiance authority so blip colour and relationship can never diverge.
    /// </summary>
    public sealed class CombatantProfile
    {
        public CombatantProfile(string relationshipGroup, BlipColor blipColor, Allegiance allegiance)
        {
            RelationshipGroup = relationshipGroup;
            BlipColor = blipColor;
            Allegiance = allegiance;
        }

        public string RelationshipGroup { get; }
        public BlipColor BlipColor { get; }
        public Allegiance Allegiance { get; }
    }
}
