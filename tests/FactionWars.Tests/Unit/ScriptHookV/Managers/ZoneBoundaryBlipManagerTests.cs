using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class ZoneBoundaryBlipManagerTests
    {
        private readonly Mock<IGameBridge> _bridge = new Mock<IGameBridge>();
        private readonly Mock<ITerritoryEvents> _territory = new Mock<ITerritoryEvents>();

        private readonly Zone _zone = new Zone(
            "vinewood", "Vinewood",
            new Vector3(100f, 200f, 30f),
            radius: 250f,
            strategicValue: 5)
        {
            OwnerFactionId = "michael",
        };

        private ZoneBoundaryBlipManager BuildSut()
        {
            return new ZoneBoundaryBlipManager(_bridge.Object, _territory.Object);
        }

        [Fact]
        public void OnZoneEntered_CreatesRadiusBlipAtZoneCenter()
        {
            _bridge.Setup(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(99);
            BuildSut();

            _territory.Raise(t => t.ZoneEntered += null, this, _zone);

            _bridge.Verify(b => b.CreateRadiusBlip(_zone.Center, _zone.Radius), Times.Once);
        }

        [Fact]
        public void OnZoneEntered_ColorsBlipWithOwnerFactionColor()
        {
            _bridge.Setup(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(99);
            BuildSut();

            _territory.Raise(t => t.ZoneEntered += null, this, _zone);

            _bridge.Verify(b => b.SetBlipColor(99, BlipColor.MichaelBlue), Times.Once);
        }

        [Fact]
        public void OnZoneExited_DeletesPriorRadiusBlip()
        {
            _bridge.Setup(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(99);
            BuildSut();

            _territory.Raise(t => t.ZoneEntered += null, this, _zone);
            _territory.Raise(t => t.ZoneExited += null, this, _zone);

            _bridge.Verify(b => b.DeleteBlip(99), Times.Once);
        }

        [Fact]
        public void OnConsecutiveZoneEntries_DeletesPriorBlipBeforeCreatingNew()
        {
            _bridge.SetupSequence(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>()))
                .Returns(11)
                .Returns(22);
            BuildSut();

            var second = new Zone("rockford", "Rockford Hills", new Vector3(0, 0, 0), 200f, 5)
            {
                OwnerFactionId = "michael",
            };

            _territory.Raise(t => t.ZoneEntered += null, this, _zone);
            _territory.Raise(t => t.ZoneEntered += null, this, second);

            _bridge.Verify(b => b.DeleteBlip(11), Times.Once);
            _bridge.Verify(b => b.CreateRadiusBlip(second.Center, second.Radius), Times.Once);
        }

        [Fact]
        public void OnZoneEntered_SetsLowAlphaSoMapStaysReadable()
        {
            _bridge.Setup(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(99);
            BuildSut();

            _territory.Raise(t => t.ZoneEntered += null, this, _zone);

            // 0 = invisible, 255 = solid. Anything <= 80 is roughly the GTA-native
            // "translucent overlay" range that lets roads remain visible through it.
            _bridge.Verify(b => b.SetBlipAlpha(99, It.Is<int>(a => a > 0 && a <= 80)), Times.Once);
        }

        [Fact]
        public void OnOwnershipChanged_ForCurrentZoneWhileInside_RecolorsBoundaryToNewOwner()
        {
            _bridge.Setup(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(99);
            var davis = new Zone("davis", "Davis", new Vector3(0, 0, 0), 200f, 5) { OwnerFactionId = "franklin" };
            _territory.Setup(t => t.CurrentZone).Returns(davis);
            var sut = BuildSut();

            _territory.Raise(t => t.ZoneEntered += null, this, davis);
            // Entered as franklin's -> green boundary.
            _bridge.Verify(b => b.SetBlipColor(99, BlipColor.FranklinGreen), Times.Once);

            // Player captures davis while standing in it (no exit/re-enter fires).
            sut.OnOwnershipChanged("davis", "michael");

            _bridge.Verify(b => b.SetBlipColor(99, BlipColor.MichaelBlue), Times.Once);
        }

        [Fact]
        public void OnOwnershipChanged_ForDifferentZone_DoesNotRecolor()
        {
            _bridge.Setup(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(99);
            var davis = new Zone("davis", "Davis", new Vector3(0, 0, 0), 200f, 5) { OwnerFactionId = "franklin" };
            _territory.Setup(t => t.CurrentZone).Returns(davis);
            var sut = BuildSut();

            _territory.Raise(t => t.ZoneEntered += null, this, davis);

            // A different zone changing hands must not touch the active boundary blip.
            sut.OnOwnershipChanged("strawberry", "michael");

            _bridge.Verify(b => b.SetBlipColor(99, BlipColor.MichaelBlue), Times.Never);
        }

        [Fact]
        public void OnOwnershipChanged_NoActiveBlip_DoesNothing()
        {
            var davis = new Zone("davis", "Davis", new Vector3(0, 0, 0), 200f, 5) { OwnerFactionId = "franklin" };
            _territory.Setup(t => t.CurrentZone).Returns(davis);
            var sut = BuildSut();

            // No ZoneEntered raised, so there is no active boundary blip to recolour.
            sut.OnOwnershipChanged("davis", "michael");

            _bridge.Verify(b => b.SetBlipColor(It.IsAny<int>(), It.IsAny<BlipColor>()), Times.Never);
        }

        [Fact]
        public void OnZoneEntered_NeutralZone_UsesWhiteColor()
        {
            _bridge.Setup(b => b.CreateRadiusBlip(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(99);
            var neutral = new Zone("alamo", "Alamo Sea", new Vector3(0, 0, 0), 300f, strategicValue: 1);

            BuildSut();
            _territory.Raise(t => t.ZoneEntered += null, this, neutral);

            _bridge.Verify(b => b.SetBlipColor(99, BlipColor.White), Times.Once);
        }
    }
}
