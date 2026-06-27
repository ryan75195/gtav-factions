using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class ZoneCombatantSpawnerTests
    {
        [Fact]
        public void Spawn_FriendlyCombatant_AssignsFactionGroupColourAndBlip()
        {
            var bridge = new Mock<IGameBridge>();
            var spawning = new Mock<IPedSpawningService>();
            var blips = new Mock<IPedBlipService>();
            spawning.Setup(s => s.SpawnPed("model", It.IsAny<Vector3>(), "michael", "z1"))
                    .Returns(new PedHandle(50));

            var sut = new ZoneCombatantSpawner(new AllegianceResolver(), spawning.Object, blips.Object, bridge.Object);

            var handle = sut.Spawn("michael", "michael", "model", new Vector3(1, 2, 3), "z1");

            Assert.Equal(50, handle.Handle);
            blips.Verify(b => b.CreateBlipForPed(50, BlipColor.MichaelBlue), Times.Once);
            bridge.Verify(b => b.SetPedAsFriendly(50), Times.Once);
        }

        [Fact]
        public void Spawn_HostileCombatant_UsesItsOwnColourAndHostileConfig()
        {
            var bridge = new Mock<IGameBridge>();
            var spawning = new Mock<IPedSpawningService>();
            var blips = new Mock<IPedBlipService>();
            spawning.Setup(s => s.SpawnPed("model", It.IsAny<Vector3>(), "franklin", "z1"))
                    .Returns(new PedHandle(60));

            var sut = new ZoneCombatantSpawner(new AllegianceResolver(), spawning.Object, blips.Object, bridge.Object);

            var handle = sut.Spawn("franklin", "michael", "model", new Vector3(1, 2, 3), "z1");

            Assert.Equal(60, handle.Handle);
            blips.Verify(b => b.CreateBlipForPed(60, BlipColor.FranklinGreen), Times.Once);
            bridge.Verify(b => b.SetPedAsHostileWanderer(60), Times.Once);
        }

        [Fact]
        public void Spawn_WhenSpawnFails_ReturnsInvalidAndDoesNotBlip()
        {
            var bridge = new Mock<IGameBridge>();
            var spawning = new Mock<IPedSpawningService>();
            var blips = new Mock<IPedBlipService>();
            spawning.Setup(s => s.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(PedHandle.Invalid);

            var sut = new ZoneCombatantSpawner(new AllegianceResolver(), spawning.Object, blips.Object, bridge.Object);

            var handle = sut.Spawn("franklin", "michael", "model", new Vector3(1, 2, 3), "z1");

            Assert.False(handle.IsValid);
            blips.Verify(b => b.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()), Times.Never);
        }
    }
}
