using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class AmbientTrafficControllerTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private bool _gate = true;

        private AmbientTrafficController Build()
            => new AmbientTrafficController(_bridge, new AmbientTrafficSuppressor(), () => _gate);

        private int AmbientCarWithDriver(Vector3 pos)
        {
            int veh = _bridge.CreateAmbientVehicle("adder", pos);
            int driver = _bridge.CreatePed("d", pos);
            _bridge.SetVehicleDriver(veh, driver);
            _bridge.PutPedInVehicle(driver, veh, -1); // so a leave-vehicle task is observable
            return veh;
        }

        [Fact]
        public void GateClosed_DoesNotEvict()
        {
            _gate = false;
            int veh = AmbientCarWithDriver(new Vector3(0f, 0f, 0f));
            int driver = _bridge.GetVehicleDriver(veh);

            Build().Update();

            Assert.True(_bridge.IsPedInVehicle(driver));
        }

        [Fact]
        public void GateOpen_EvictsAmbientDriver_AndHandbrakes()
        {
            int veh = AmbientCarWithDriver(new Vector3(0f, 0f, 0f));
            int driver = _bridge.GetVehicleDriver(veh);

            Build().Update();

            Assert.False(_bridge.IsPedInVehicle(driver));
            Assert.True(_bridge.GetVehicleHandbrakeForTest(veh));
        }

        [Fact]
        public void GateOpen_LeavesPlayerVehicleAndPersistentCar()
        {
            int playerVeh = _bridge.CreateVehicle("adder", new Vector3(0f, 0f, 0f)); // persistent
            _bridge.PlayerVehicleHandle = playerVeh;
            int pDriver = _bridge.CreatePed("p", new Vector3(0f, 0f, 0f));
            _bridge.SetVehicleDriver(playerVeh, pDriver);
            _bridge.PutPedInVehicle(pDriver, playerVeh, -1);

            Build().Update();

            Assert.True(_bridge.IsPedInVehicle(pDriver)); // untouched: persistent AND the player's vehicle
            Assert.False(_bridge.GetVehicleHandbrakeForTest(playerVeh));
        }

        [Fact]
        public void Throttle_SkipsSecondScanWithinWindow()
        {
            var controller = Build();
            controller.Update(); // first scan at t=0, no ambient car present yet

            int veh = AmbientCarWithDriver(new Vector3(0f, 0f, 0f));
            int driver = _bridge.GetVehicleDriver(veh);
            _bridge.GameTime = 100; // < 750ms throttle window

            controller.Update();

            Assert.True(_bridge.IsPedInVehicle(driver)); // not re-scanned yet
        }
    }
}
