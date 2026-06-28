using System;
using System.Collections.Generic;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class BattleParticipantTests
    {
        private static Dictionary<DefenderRole, int> Troops(int basic = 0, int medium = 0, int heavy = 0, int elite = 0)
        {
            return new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, basic },
                { DefenderRole.Gunner, medium },
                { DefenderRole.Rifleman, heavy },
                { DefenderRole.Rocketeer, elite }
            };
        }

        [Fact]
        public void AiParticipant_StoresFactionRoleAndTroops()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 5, medium: 2));

            Assert.Equal("trevor", p.FactionId);
            Assert.Equal(BattleRole.Attacker, p.Role);
            Assert.False(p.IsPlayer);
            Assert.Equal(5, p.Troops[DefenderRole.Grunt]);
            Assert.Equal(2, p.Troops[DefenderRole.Gunner]);
        }

        [Fact]
        public void AiParticipant_AliveCount_SumsTroops()
        {
            var p = BattleParticipant.ForAi("michael", BattleRole.Defender, Troops(basic: 3, heavy: 4));

            Assert.Equal(7, p.AliveCount);
        }

        [Fact]
        public void AiParticipant_AliveCount_DropsAsTroopsRemoved()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 5));

            p.RemoveTroop(DefenderRole.Grunt);
            Assert.Equal(4, p.AliveCount);

            p.RemoveTroop(DefenderRole.Grunt);
            p.RemoveTroop(DefenderRole.Grunt);
            Assert.Equal(2, p.AliveCount);
        }

        [Fact]
        public void AiParticipant_RemoveTroop_ReturnsFalseWhenEmpty()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 0));

            Assert.False(p.RemoveTroop(DefenderRole.Grunt));
            Assert.Equal(0, p.AliveCount);
        }

        [Fact]
        public void AiParticipant_AddTroops_IncrementsExistingTier()
        {
            var p = BattleParticipant.ForAi("trevor", BattleRole.Attacker, Troops(basic: 1));

            p.AddTroops(DefenderRole.Grunt, 4);
            Assert.Equal(5, p.Troops[DefenderRole.Grunt]);
            Assert.Equal(5, p.AliveCount);
        }

        [Fact]
        public void PlayerParticipant_HasIsPlayerTrue_AndUsesCallbackForAliveCount()
        {
            int squadCount = 3;
            var p = BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => squadCount);

            Assert.Equal("player_faction", p.FactionId);
            Assert.Equal(BattleRole.Attacker, p.Role);
            Assert.True(p.IsPlayer);
            Assert.Empty(p.Troops);
            Assert.Equal(3, p.AliveCount);

            squadCount = 0;
            Assert.Equal(0, p.AliveCount);
        }

        [Fact]
        public void PlayerParticipant_RemoveTroop_DoesNothing()
        {
            // Player aliveness is owned by the squad callback, not the troop dict.
            // RemoveTroop is a no-op for player participants and returns false.
            var p = BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, () => 2);

            Assert.False(p.RemoveTroop(DefenderRole.Grunt));
            Assert.Equal(2, p.AliveCount);
        }

        [Fact]
        public void ForAi_NullFactionId_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BattleParticipant.ForAi(null!, BattleRole.Attacker, Troops()));
        }

        [Fact]
        public void ForAi_NullTroops_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BattleParticipant.ForAi("trevor", BattleRole.Attacker, null!));
        }

        [Fact]
        public void ForPlayer_NullCallback_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BattleParticipant.ForPlayer("player_faction", BattleRole.Attacker, null!));
        }
    }
}
