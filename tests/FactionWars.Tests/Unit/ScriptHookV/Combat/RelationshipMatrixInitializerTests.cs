using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class RelationshipMatrixInitializerTests
    {
        private const int Hate = 5;
        private const int Companion = 0;

        [Fact]
        public void Initialize_HatesBetweenFactionsAndProtectsPlayerFaction()
        {
            var bridge = new Mock<IGameBridge>();
            var sut = new RelationshipMatrixInitializer(bridge.Object);

            sut.Initialize("michael", new List<string> { "michael", "franklin", "trevor" });

            // Faction-vs-faction hate (group names are uppercased)
            bridge.Verify(b => b.SetRelationshipBetweenGroups("MICHAEL", "FRANKLIN", Hate, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("MICHAEL", "TREVOR", Hate, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("FRANKLIN", "TREVOR", Hate, true), Times.Once);

            // Player's faction allied to PLAYER group; others hate PLAYER
            bridge.Verify(b => b.SetRelationshipBetweenGroups("MICHAEL", "PLAYER", Companion, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("FRANKLIN", "PLAYER", Hate, true), Times.Once);
            bridge.Verify(b => b.SetRelationshipBetweenGroups("TREVOR", "PLAYER", Hate, true), Times.Once);
        }
    }
}
