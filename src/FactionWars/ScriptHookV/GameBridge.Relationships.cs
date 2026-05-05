using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        /// <inheritdoc />
        public void SetRelationshipBetweenGroups(string groupName1, string groupName2, int relationship, bool bidirectional = true)
        {
            if (string.IsNullOrWhiteSpace(groupName1) || string.IsNullOrWhiteSpace(groupName2))
                return;

            try
            {
                var group1 = World.AddRelationshipGroup(groupName1.ToUpperInvariant());
                var group2 = World.AddRelationshipGroup(groupName2.ToUpperInvariant());

                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationship, group1, group2);
                if (bidirectional)
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationship, group2, group1);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetRelationshipBetweenGroups exception for {groupName1} -> {groupName2}", ex);
            }
        }
    }
}
