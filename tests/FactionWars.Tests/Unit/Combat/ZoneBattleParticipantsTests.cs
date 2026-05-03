using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class ZoneBattleParticipantsTests
    {
        private static Dictionary<DefenderTier, int> Troops(int basic) =>
            new Dictionary<DefenderTier, int> { { DefenderTier.Basic, basic } };

        [Fact]
        public void Participants_HasExactlyOneDefenderAndOneAttacker()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(2, battle.Participants.Count);
            Assert.Single(battle.Participants.Where(p => p.Role == BattleRole.Defender));
            Assert.Single(battle.Participants.Where(p => p.Role == BattleRole.Attacker));
        }

        [Fact]
        public void Defender_ReturnsTheDefendingFactionParticipant()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal("michael", battle.Defender.FactionId);
            Assert.Equal(BattleRole.Defender, battle.Defender.Role);
            Assert.Equal(7, battle.Defender.AliveCount);
        }

        [Fact]
        public void Attackers_ReturnsListContainingOnlyTheAttacker()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Single(battle.Attackers);
            Assert.Equal("trevor", battle.Attackers[0].FactionId);
            Assert.Equal(BattleRole.Attacker, battle.Attackers[0].Role);
            Assert.Equal(5, battle.Attackers[0].AliveCount);
        }

        [Fact]
        public void LegacyAccessor_AttackerFactionId_AgreesWithAttackersList()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(battle.Attackers[0].FactionId, battle.AttackerFactionId);
        }

        [Fact]
        public void LegacyAccessor_DefenderFactionId_AgreesWithDefender()
        {
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(battle.Defender.FactionId, battle.DefenderFactionId);
        }

        [Fact]
        public void RemovingTroopsViaParticipant_UpdatesLegacyTotals()
        {
            // Mutating through the new participant API should update legacy totals.
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(3), Troops(3));

            battle.Defender.RemoveTroop(DefenderTier.Basic);

            Assert.Equal(2, battle.TotalDefenderTroops);
            Assert.Equal(2, battle.DefenderTroops[DefenderTier.Basic]);
        }

        [Fact]
        public void RemovingTroopsViaLegacyApi_UpdatesParticipantState()
        {
            // Mutating through the legacy API (RemoveAttackerTroop) should be
            // visible on the participant.
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(3), Troops(3));

            battle.RemoveAttackerTroop(DefenderTier.Basic);

            Assert.Equal(2, battle.Attackers[0].AliveCount);
        }

        [Fact]
        public void Participants_AreInDefenderThenAttackerOrder()
        {
            // Plan 2 readers may rely on the index 0 = defender ordering. Lock it in.
            var battle = new ZoneBattle("trevor", "michael", "zone_1", Troops(5), Troops(7));

            Assert.Equal(BattleRole.Defender, battle.Participants[0].Role);
            Assert.Equal(BattleRole.Attacker, battle.Participants[1].Role);
        }

        [Fact]
        public void ConstructorWithParticipants_PreservesParticipantInstances()
        {
            var defender = BattleParticipant.ForAi("michael", BattleRole.Defender,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            var attacker = BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => 4);

            var battle = new ZoneBattle("zone_42", new List<BattleParticipant> { defender, attacker },
                playerFactionId: "player_faction");

            Assert.Equal("zone_42", battle.ZoneId);
            Assert.Equal("player_faction", battle.PlayerFactionId);
            Assert.Same(defender, battle.Defender);
            Assert.Same(attacker, battle.Attackers[0]);
            Assert.True(battle.Attackers[0].IsPlayer);
            Assert.Equal(4, battle.Attackers[0].AliveCount);
            Assert.Equal(5, battle.InitialDefenderTroops);
            Assert.Equal(4, battle.InitialAttackerTroops);
        }

        [Fact]
        public void ConstructorWithParticipants_ThrowsWhenNoDefender()
        {
            var attacker1 = BattleParticipant.ForAi("trevor", BattleRole.Attacker,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });
            var attacker2 = BattleParticipant.ForAi("franklin", BattleRole.Attacker,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });

            Assert.Throws<ArgumentException>(() =>
                new ZoneBattle("zone_42", new List<BattleParticipant> { attacker1, attacker2 }));
        }
    }
}
