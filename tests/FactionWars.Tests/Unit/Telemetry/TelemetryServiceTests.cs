using System;
using System.Collections.Generic;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class TelemetryServiceTests
    {
        private readonly Mock<ITelemetrySink> _sink = new();
        private readonly Mock<IFactionService> _factionService = new();
        private readonly Mock<IZoneService> _zoneService = new();
        private readonly Mock<IGameStateManager> _gameStateManager = new();

        public TelemetryServiceTests()
        {
            _factionService.Setup(s => s.GetAllFactions()).Returns(System.Array.Empty<Faction>());
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(0);
        }

        [Fact]
        public void Tick_BuildsSnapshotAndWritesToSink()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael });
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });
            _zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(123L);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Tick(); // public test entry that bypasses the timer

            _sink.Verify(s => s.WriteSnapshot(It.Is<IReadOnlyList<FactionSnapshot>>(rows =>
                rows.Count == 1 && rows[0].FactionId == "michael" && rows[0].Cash == 500
                && rows[0].PlayTimeSeconds == 123L)), Times.Once);
        }

        [Fact]
        public void Tick_EmptyFactionList_DoesNotCallSink()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Tick();

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Never);
        }

        [Fact]
        public void Dispose_StopsTimerAndIsIdempotent()
        {
            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);
            svc.Dispose();
            svc.Dispose(); // must not throw
        }

        [Fact]
        public void TickAfterDispose_DoesNotCallSink()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns(new[] { michael });
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });

            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);
            svc.Dispose();
            svc.Tick();

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Never);
        }

        [Fact]
        public void Tick_BuilderThrows_DoesNotPropagate()
        {
            // Cause the snapshot builder to throw via the underlying faction service.
            _factionService.Setup(s => s.GetAllFactions()).Throws(new InvalidOperationException("boom"));

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            // Must not propagate the exception out of Tick.
            svc.Tick();

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Never);
        }
    }
}
