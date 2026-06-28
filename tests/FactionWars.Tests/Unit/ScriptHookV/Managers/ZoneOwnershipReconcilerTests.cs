using System.Collections.Generic;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class ZoneOwnershipReconcilerTests
    {
        private readonly List<string> _friendlyDespawned = new List<string>();
        private readonly List<string> _enemyDespawned = new List<string>();

        private ZoneOwnershipReconciler CreateReconciler(string? playerFaction)
            => new ZoneOwnershipReconciler(
                z => _friendlyDespawned.Add(z),
                z => _enemyDespawned.Add(z),
                () => playerFaction);

        [Fact]
        public void OnOwnershipChanged_ZoneLeavesPlayer_DespawnsFriendlyDefenders()
        {
            var sut = CreateReconciler("michael");

            sut.OnOwnershipChanged("davis", previousOwner: "michael", newOwner: "franklin");

            Assert.Contains("davis", _friendlyDespawned);
            Assert.DoesNotContain("davis", _enemyDespawned);
        }

        [Fact]
        public void OnOwnershipChanged_PlayerCapturesEnemyZone_DespawnsEnemyDefenders()
        {
            var sut = CreateReconciler("michael");

            sut.OnOwnershipChanged("davis", previousOwner: "franklin", newOwner: "michael");

            Assert.Contains("davis", _enemyDespawned);
            Assert.DoesNotContain("davis", _friendlyDespawned);
        }

        [Fact]
        public void OnOwnershipChanged_EnemyZoneNeutralisedWhileInside_DespawnsEnemyDefenders()
        {
            // Reproduces the orphaned-garrison bug: a zone goes franklin -> neutral while the
            // player stands inside it (no zone-exit fires), leaving live hostile peds behind.
            var sut = CreateReconciler("michael");

            sut.OnOwnershipChanged("davis", previousOwner: "franklin", newOwner: null);

            Assert.Contains("davis", _enemyDespawned);
        }

        [Fact]
        public void OnOwnershipChanged_NoRealChange_DespawnsNothing()
        {
            var sut = CreateReconciler("michael");

            sut.OnOwnershipChanged("davis", previousOwner: "franklin", newOwner: "franklin");

            Assert.Empty(_friendlyDespawned);
            Assert.Empty(_enemyDespawned);
        }
    }
}
