using System;
using System.Collections.Generic;
using System.Linq;
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
            s.AddReserveTroops(DefenderTier.Basic, basic);
            s.AddReserveTroops(DefenderTier.Medium, medium);
            s.AddReserveTroops(DefenderTier.Heavy, heavy);
            s.AddReserveTroops(DefenderTier.Elite, elite);
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
