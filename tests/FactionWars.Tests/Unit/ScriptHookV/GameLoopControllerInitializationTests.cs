using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController initialization behaviour. The mod no longer
    /// owns weapons/cash — those live in the GTA save now — so init must not
    /// destroy whatever the loaded save brought.
    /// </summary>
    public class GameLoopControllerInitializationTests
    {
        private MockGameBridge SetupAndInit()
        {
            var bridge = new MockGameBridge { PlayerCharacterModel = "player_zero" };
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            // Pre-populate state that init must not destroy.
            bridge.PlayerMoney = 100000;
            bridge.GivePlayerWeapon("weapon_pistol", 50);

            controller.OnTick();
            return bridge;
        }

        [Fact]
        public void Init_DoesNotStripPlayerWeapons()
        {
            var bridge = SetupAndInit();

            Assert.False(bridge.WereAllPlayerWeaponsRemoved);
            Assert.Contains("weapon_pistol", bridge.PlayerWeapons);
        }

        [Fact]
        public void Init_DoesNotResetPlayerCash()
        {
            var bridge = SetupAndInit();

            Assert.Equal(100000, bridge.PlayerMoney);
        }
    }
}
