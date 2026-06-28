using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Combat
{
    /// <inheritdoc />
    public class ZoneCombatantSpawner : IZoneCombatantSpawner
    {
        private readonly IAllegianceResolver _resolver;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IGameBridge _gameBridge;

        public ZoneCombatantSpawner(
            IAllegianceResolver resolver,
            IPedSpawningService pedSpawningService,
            IPedBlipService pedBlipService,
            IGameBridge gameBridge)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _pedBlipService = pedBlipService ?? throw new ArgumentNullException(nameof(pedBlipService));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        public PedHandle Spawn(string factionId, string playerFactionId, string model, Vector3 position, string zoneId)
        {
            var profile = _resolver.Resolve(factionId, playerFactionId);

            var handle = _pedSpawningService.SpawnPed(model, position, factionId, zoneId);
            if (!handle.IsValid)
                return PedHandle.Invalid;

            ConfigureCombat(handle.Handle, profile.Allegiance);
            _pedBlipService.CreateBlipForPed(handle.Handle, profile.BlipColor);
            return handle;
        }

        private void ConfigureCombat(int pedHandle, Allegiance allegiance)
        {
            if (allegiance == Allegiance.Friendly)
                _gameBridge.SetPedAsFriendly(pedHandle);
            else
                _gameBridge.SetPedAsHostileWanderer(pedHandle);
        }
    }
}
