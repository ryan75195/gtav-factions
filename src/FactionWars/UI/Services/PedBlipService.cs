using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing minimap blips attached to peds.
    /// </summary>
    public class PedBlipService : IPedBlipService
    {
        private readonly IGameBridge _gameBridge;
        private readonly Dictionary<int, int> _pedToBlipMap; // pedHandle -> blipHandle

        public PedBlipService(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _pedToBlipMap = new Dictionary<int, int>();
        }

        public int CreateBlipForPed(int pedHandle, BlipColor color)
        {
            // Remove existing blip if any
            RemoveBlipForPed(pedHandle);

            var blipHandle = _gameBridge.CreateBlipForPed(pedHandle);
            if (blipHandle < 0)
                return -1;

            _gameBridge.SetBlipColor(blipHandle, color);
            _pedToBlipMap[pedHandle] = blipHandle;

            return blipHandle;
        }

        public void RemoveBlipForPed(int pedHandle)
        {
            if (_pedToBlipMap.TryGetValue(pedHandle, out var blipHandle))
            {
                _gameBridge.DeleteBlip(blipHandle);
                _pedToBlipMap.Remove(pedHandle);
            }
        }

        public bool HasBlipForPed(int pedHandle)
        {
            return _pedToBlipMap.ContainsKey(pedHandle);
        }

        public void RemoveAllBlips()
        {
            foreach (var blipHandle in _pedToBlipMap.Values)
            {
                _gameBridge.DeleteBlip(blipHandle);
            }
            _pedToBlipMap.Clear();
        }
    }
}
