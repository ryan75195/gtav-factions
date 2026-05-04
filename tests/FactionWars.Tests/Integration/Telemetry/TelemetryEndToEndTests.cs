using System;
using System.IO;
using System.Linq;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Services;
using FactionWars.Telemetry.Sinks;
using FactionWars.Territory.Events;
using FactionWars.Territory.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.Telemetry
{
    public class TelemetryEndToEndTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly CsvTelemetrySink _sink;

        public TelemetryEndToEndTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fw_tel_e2e_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _sink = new CsvTelemetrySink(_tempDir);
        }

        public void Dispose()
        {
            _sink.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Fact]
        public void FullScenario_WritesExpectedCsvFiles()
        {
            var factionService = new Mock<IFactionService>();
            var zoneService = new Mock<IZoneService>();
            var gameStateManager = new Mock<IGameStateManager>();

            var michael = new Faction("michael", "Michael's Crew");
            var michaelState = new FactionState("michael", initialCash: 500, initialTroopCount: 12);

            factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael });
            factionService.Setup(s => s.GetFactionState("michael")).Returns(michaelState);
            zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(100L);

            using var service = new TelemetryService(
                _sink,
                factionService.Object,
                zoneService.Object,
                gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 99,
                    IsFirstTimeSeenSave = _ => true
                });

            service.Tick();
            gameStateManager.Raise(g => g.OnGameLoaded += null,
                new GameStateLoadedEventArgs(0, "SGTA0099", true));
            zoneService.Raise(z => z.ZoneOwnershipChanged += null,
                new ZoneOwnershipChangedEventArgs("morningwood", "trevor", "michael"));

            var saveDir = Path.Combine(_tempDir, "SGTA0099");
            Assert.True(Directory.Exists(saveDir));

            var snapshotLines = File.ReadAllLines(Path.Combine(saveDir, "snapshots.csv"));
            Assert.True(snapshotLines.Length >= 2);
            Assert.Contains(snapshotLines.Skip(1), line => line.Contains("michael") && line.Contains("500"));

            var zoneLines = File.ReadAllLines(Path.Combine(saveDir, "zone_events.csv"));
            Assert.True(zoneLines.Length >= 3);
            Assert.Contains(zoneLines.Skip(1), line => line.Contains("Captured") && line.Contains("morningwood"));
            Assert.Contains(zoneLines.Skip(1), line => line.Contains("Lost") && line.Contains("morningwood"));

            var metaLines = File.ReadAllLines(Path.Combine(saveDir, "match_meta.csv"));
            Assert.Contains(metaLines.Skip(1), line => line.Contains("ModSessionStart"));
            Assert.Contains(metaLines.Skip(1), line => line.Contains("MatchStart") && line.Contains("SGTA0099"));
        }
    }
}
