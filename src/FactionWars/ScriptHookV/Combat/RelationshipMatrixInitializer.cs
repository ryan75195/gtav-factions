using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;

namespace FactionWars.ScriptHookV.Combat
{
    /// <inheritdoc />
    public class RelationshipMatrixInitializer : IRelationshipMatrixInitializer
    {
        private const int RelHate = 5;
        private const int RelCompanion = 0;
        private const string PlayerGroup = "PLAYER";

        private readonly IGameBridge _gameBridge;

        public RelationshipMatrixInitializer(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        public void Initialize(string playerFactionId, IReadOnlyList<string> allFactionIds)
        {
            if (allFactionIds == null) throw new ArgumentNullException(nameof(allFactionIds));

            var groups = new List<string>(allFactionIds.Count);
            foreach (var id in allFactionIds) groups.Add(id.ToUpperInvariant());

            for (int i = 0; i < groups.Count; i++)
                for (int j = i + 1; j < groups.Count; j++)
                    _gameBridge.SetRelationshipBetweenGroups(groups[i], groups[j], RelHate, true);

            var playerGroupName = playerFactionId?.ToUpperInvariant();
            foreach (var group in groups)
            {
                var rel = group == playerGroupName ? RelCompanion : RelHate;
                _gameBridge.SetRelationshipBetweenGroups(group, PlayerGroup, rel, true);
            }
        }
    }
}
