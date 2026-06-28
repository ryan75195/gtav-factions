using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class FactionSnapshotBuilderTests
    {
        private static FactionState MakeState(string id, int cash, int basic, int medium, int heavy, int elite)
        {
            var s = new FactionState(id) { Cash = cash };
            s.AddReserveTroops(DefenderRole.Grunt, basic);
            s.AddReserveTroops(DefenderRole.Gunner, medium);
            s.AddReserveTroops(DefenderRole.Rifleman, heavy);
            s.AddReserveTroops(DefenderRole.Rocketeer, elite);
            return s;
        }

        [Fact]
        public void Build_ProducesOneSnapshotPerActiveFaction()
        {
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            var michael = new Faction("michael", "Michael's Crew");
            var trevor = new Faction("trevor", "Trevor's Crew");
            factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael, trevor });
            factionService.Setup(s => s.GetFactionState("michael")).Returns(MakeState("michael", 500, 4, 2, 1, 0));
            factionService.Setup(s => s.GetFactionState("trevor")).Returns(MakeState("trevor", 0, 70, 0, 0, 0));
            zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            zoneService.Setup(z => z.GetZoneCount("trevor")).Returns(21);

            var builder = new FactionSnapshotBuilder(factionService.Object, zoneService.Object);

            var rows = builder.Build(new DateTime(2026, 1, 1, 12, 0, 0), playTimeSeconds: 100);

            Assert.Equal(2, rows.Count);
            var m = rows.Single(r => r.FactionId == "michael");
            Assert.Equal(500, m.Cash);
            Assert.Equal(7, m.TotalTroops);    // 4+2+1+0
            Assert.Equal(8, m.ZonesOwned);
            Assert.Equal(4, m.Basic);
            Assert.Equal(2, m.Medium);
            Assert.Equal(1, m.Heavy);
            Assert.Equal(0, m.Elite);
            Assert.Equal(7, m.ReserveTroops);  // sum of tiers
            Assert.Equal(0, m.DeployedTroops); // total - reserve = 0
        }

        [Fact]
        public void Build_IncludesDeployedZoneAllocationsInTroopTotals()
        {
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            var allocationService = new Mock<IZoneDefenderAllocationService>();
            var michael = new Faction("michael", "Michael's Crew");
            var state = MakeState("michael", 500, 4, 2, 1, 0);
            var downtown = new ZoneDefenderAllocation("michael", "downtown");
            downtown.AddTroops(DefenderRole.Grunt, 10);
            downtown.AddTroops(DefenderRole.Gunner, 3);
            downtown.AddTroops(DefenderRole.Rocketeer, 1);
            var vinewood = new ZoneDefenderAllocation("michael", "vinewood");
            vinewood.AddTroops(DefenderRole.Rifleman, 2);

            factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael });
            factionService.Setup(s => s.GetFactionState("michael")).Returns(state);
            zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            allocationService.Setup(a => a.GetAllocationsForFaction("michael"))
                .Returns(new[] { downtown, vinewood });

            var builder = new FactionSnapshotBuilder(
                factionService.Object,
                zoneService.Object,
                allocationService.Object,
                getPlayerFactionId: null,
                getPlayerMoney: null);

            var rows = builder.Build(new DateTime(2026, 1, 1, 12, 0, 0), playTimeSeconds: 100);

            var m = Assert.Single(rows);
            Assert.Equal(23, m.TotalTroops);
            Assert.Equal(14, m.Basic);
            Assert.Equal(5, m.Medium);
            Assert.Equal(3, m.Heavy);
            Assert.Equal(1, m.Elite);
            Assert.Equal(7, m.ReserveTroops);
            Assert.Equal(16, m.DeployedTroops);
        }

        [Fact]
        public void Build_UsesGameWalletForPlayerFactionCash()
        {
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            var michael = new Faction("michael", "Michael's Crew");
            var trevor = new Faction("trevor", "Trevor's Crew");

            factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael, trevor });
            factionService.Setup(s => s.GetFactionState("michael")).Returns(MakeState("michael", 500, 0, 0, 0, 0));
            factionService.Setup(s => s.GetFactionState("trevor")).Returns(MakeState("trevor", 800, 0, 0, 0, 0));

            var builder = new FactionSnapshotBuilder(
                factionService.Object,
                zoneService.Object,
                allocationService: null,
                getPlayerFactionId: () => "michael",
                getPlayerMoney: () => 1234);

            var rows = builder.Build(new DateTime(2026, 1, 1, 12, 0, 0), playTimeSeconds: 100);

            Assert.Equal(1234, rows.Single(r => r.FactionId == "michael").Cash);
            Assert.Equal(800, rows.Single(r => r.FactionId == "trevor").Cash);
        }

        [Fact]
        public void Build_NoFactions_ReturnsEmpty()
        {
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            factionService.Setup(s => s.GetAllFactions()).Returns(System.Array.Empty<Faction>());

            var builder = new FactionSnapshotBuilder(factionService.Object, zoneService.Object);
            var rows = builder.Build(new DateTime(2025, 6, 1), 999);

            Assert.Empty(rows);
        }
    }
}
