using System.Collections.Generic;
using FactionWars.Core.Interfaces;

namespace FactionWars.Core.Utils
{
    /// <summary>
    /// Mock implementation of IGameBridge for unit testing.
    /// Provides full control over game state and behavior verification.
    /// </summary>
    public class MockGameBridge : IGameBridge
    {
        private readonly Dictionary<int, PedState> _peds = new Dictionary<int, PedState>();
        private readonly Dictionary<int, BlipState> _blips = new Dictionary<int, BlipState>();
        private readonly List<string> _notifications = new List<string>();

        private int _nextPedHandle = 1;
        private int _nextBlipHandle = 1;

        /// <summary>
        /// Gets or sets the player position to return from GetPlayerPosition.
        /// </summary>
        public Vector3 PlayerPosition { get; set; } = Vector3.Zero;

        /// <summary>
        /// Gets or sets the game time to return from GetGameTime.
        /// </summary>
        public int GameTime { get; set; } = 0;

        /// <summary>
        /// Gets the list of notifications shown.
        /// </summary>
        public IReadOnlyList<string> Notifications => _notifications;

        /// <summary>
        /// Gets the count of notifications shown.
        /// </summary>
        public int NotificationCount => _notifications.Count;

        public Vector3 GetPlayerPosition() => PlayerPosition;

        public int CreatePed(string modelName, Vector3 position)
        {
            var handle = _nextPedHandle++;
            _peds[handle] = new PedState
            {
                ModelName = modelName,
                Position = position,
                IsAlive = true,
                RelationshipGroup = string.Empty
            };
            return handle;
        }

        public void DeletePed(int pedHandle)
        {
            _peds.Remove(pedHandle);
        }

        public bool IsPedAlive(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) && ped.IsAlive;
        }

        public void SetPedRelationshipGroup(int pedHandle, string groupName)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.RelationshipGroup = groupName;
            }
        }

        public int CreateBlip(Vector3 position)
        {
            var handle = _nextBlipHandle++;
            _blips[handle] = new BlipState
            {
                Position = position,
                Color = BlipColor.White
            };
            return handle;
        }

        public void DeleteBlip(int blipHandle)
        {
            _blips.Remove(blipHandle);
        }

        public void SetBlipColor(int blipHandle, BlipColor color)
        {
            if (_blips.TryGetValue(blipHandle, out var blip))
            {
                blip.Color = color;
            }
        }

        public void ShowNotification(string message)
        {
            _notifications.Add(message);
        }

        public int GetGameTime() => GameTime;

        public bool RevivePed(int pedHandle)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.IsAlive = true;
                return true;
            }
            return false;
        }

        public void SetPedPosition(int pedHandle, Vector3 position)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Position = position;
            }
        }

        public bool SetPedModel(int pedHandle, string modelName)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.ModelName = modelName;
                return true;
            }
            return false;
        }

        // Additional helper methods for testing

        /// <summary>
        /// Checks if a ped exists (regardless of alive state).
        /// </summary>
        public bool PedExists(int pedHandle) => _peds.ContainsKey(pedHandle);

        /// <summary>
        /// Kills a ped (sets IsAlive to false but keeps the ped).
        /// </summary>
        public void KillPed(int pedHandle)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.IsAlive = false;
            }
        }

        /// <summary>
        /// Gets the relationship group of a ped.
        /// </summary>
        public string GetPedRelationshipGroup(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.RelationshipGroup : string.Empty;
        }

        /// <summary>
        /// Gets the position of a ped.
        /// </summary>
        public Vector3 GetPedPosition(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Position : Vector3.Zero;
        }

        /// <summary>
        /// Gets the model name of a ped.
        /// </summary>
        public string GetPedModel(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.ModelName : string.Empty;
        }

        /// <summary>
        /// Checks if a blip exists.
        /// </summary>
        public bool BlipExists(int blipHandle) => _blips.ContainsKey(blipHandle);

        /// <summary>
        /// Gets the color of a blip.
        /// </summary>
        public BlipColor GetBlipColor(int blipHandle)
        {
            return _blips.TryGetValue(blipHandle, out var blip) ? blip.Color : BlipColor.White;
        }

        /// <summary>
        /// Advances the game time by the specified amount.
        /// </summary>
        public void AdvanceGameTime(int milliseconds)
        {
            GameTime += milliseconds;
        }

        /// <summary>
        /// Resets all mock state to initial values.
        /// </summary>
        public void Reset()
        {
            _peds.Clear();
            _blips.Clear();
            _notifications.Clear();
            _nextPedHandle = 1;
            _nextBlipHandle = 1;
            PlayerPosition = Vector3.Zero;
            GameTime = 0;
        }

        private class PedState
        {
            public string ModelName { get; set; } = string.Empty;
            public Vector3 Position { get; set; }
            public bool IsAlive { get; set; }
            public string RelationshipGroup { get; set; } = string.Empty;
        }

        private class BlipState
        {
            public Vector3 Position { get; set; }
            public BlipColor Color { get; set; }
        }
    }
}
