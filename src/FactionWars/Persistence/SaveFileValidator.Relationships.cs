using System.Collections.Generic;
using FactionWars.Persistence.Models;

namespace FactionWars.Persistence
{
    public partial class SaveFileValidator
    {
        private void ValidateRelationships(GameState gameState, SaveFileValidationResult result,
            HashSet<string> factionIds)
        {
            foreach (var relationship in gameState.Relationships)
            {
                if (string.IsNullOrEmpty(relationship.FactionId1))
                {
                    result.AddError("Relationship has missing or empty FactionId1.");
                    continue;
                }

                if (string.IsNullOrEmpty(relationship.FactionId2))
                {
                    result.AddError("Relationship has missing or empty FactionId2.");
                    continue;
                }

                if (relationship.FactionId1 == relationship.FactionId2)
                {
                    result.AddError($"Relationship has self-reference: faction '{relationship.FactionId1}' cannot have a relationship with itself.");
                }

                if (!factionIds.Contains(relationship.FactionId1))
                {
                    result.AddError($"Relationship references non-existent faction '{relationship.FactionId1}'.");
                }

                if (!factionIds.Contains(relationship.FactionId2))
                {
                    result.AddError($"Relationship references non-existent faction '{relationship.FactionId2}'.");
                }

                if (relationship.Value < -100 || relationship.Value > 100)
                {
                    result.AddError($"Relationship Value {relationship.Value} is out of range. Must be between -100 and 100.");
                }
            }
        }
    }
}
