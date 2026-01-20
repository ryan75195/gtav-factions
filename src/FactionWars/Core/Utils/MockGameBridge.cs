using System;
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
        private readonly List<int> _blipsCreated = new List<int>();
        private readonly List<int> _blipsDeleted = new List<int>();
        private readonly Dictionary<int, BlipColor> _blipColors = new Dictionary<int, BlipColor>();

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
        /// Gets or sets the player character model to return from GetPlayerCharacterModel.
        /// </summary>
        public string PlayerCharacterModel { get; set; } = "player_zero";

        /// <summary>
        /// Gets or sets the player heading (direction facing) in degrees to return from GetPlayerHeading.
        /// 0 = North, 90 = East, 180 = South, 270 = West.
        /// </summary>
        public float PlayerHeading { get; set; } = 0f;

        /// <summary>
        /// Gets or sets whether the player is dead.
        /// </summary>
        public bool IsPlayerDeadValue { get; set; } = false;

        /// <summary>
        /// Gets or sets the player's money amount.
        /// </summary>
        public int PlayerMoney { get; set; } = 0;

        /// <summary>
        /// Gets the list of notifications shown.
        /// </summary>
        public IReadOnlyList<string> Notifications => _notifications;

        /// <summary>
        /// Gets the count of notifications shown.
        /// </summary>
        public int NotificationCount => _notifications.Count;

        /// <summary>
        /// Gets the list of blip handles that were created.
        /// </summary>
        public IReadOnlyList<int> BlipsCreated => _blipsCreated;

        /// <summary>
        /// Gets the list of blip handles that were deleted.
        /// </summary>
        public IReadOnlyList<int> BlipsDeleted => _blipsDeleted;

        /// <summary>
        /// Gets the dictionary of blip handle to color mappings.
        /// </summary>
        public IReadOnlyDictionary<int, BlipColor> BlipColors => _blipColors;

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
            _blipsCreated.Add(handle);
            return handle;
        }

        public void DeleteBlip(int blipHandle)
        {
            _blips.Remove(blipHandle);
            _blipsDeleted.Add(blipHandle);
        }

        public void SetBlipColor(int blipHandle, BlipColor color)
        {
            if (_blips.TryGetValue(blipHandle, out var blip))
            {
                blip.Color = color;
            }
            _blipColors[blipHandle] = color;
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

        public string GetPlayerCharacterModel() => PlayerCharacterModel;

        public float GetPlayerHeading() => PlayerHeading;

        public bool IsPlayerDead() => IsPlayerDeadValue;

        public int GetPlayerMoney() => PlayerMoney;

        public void AddPlayerMoney(int amount)
        {
            PlayerMoney += amount;
        }

        private readonly List<int> _followingPeds = new List<int>();
        private readonly Dictionary<int, VehicleState> _vehicles = new Dictionary<int, VehicleState>();
        private readonly Dictionary<int, int> _pedsInVehicles = new Dictionary<int, int>(); // pedHandle -> vehicleHandle
        private readonly HashSet<int> _pedsTryingToEnterVehicle = new HashSet<int>();
        private int _nextVehicleHandle = 1000;

        /// <summary>
        /// Gets or sets whether the player is in a vehicle.
        /// </summary>
        public bool IsPlayerInVehicleValue { get; set; } = false;

        /// <summary>
        /// Gets or sets the vehicle handle the player is currently in.
        /// </summary>
        public int PlayerVehicleHandle { get; set; } = -1;

        /// <summary>
        /// Gets the list of ped handles that have been set to follow the player.
        /// </summary>
        public IReadOnlyList<int> FollowingPeds => _followingPeds;

        public void SetPedAsFollower(int pedHandle)
        {
            if (_peds.ContainsKey(pedHandle))
            {
                _followingPeds.Add(pedHandle);
            }
        }

        public bool IsPlayerInVehicle() => IsPlayerInVehicleValue;

        public int GetPlayerVehicle() => PlayerVehicleHandle;

        public bool IsPedInVehicle(int pedHandle)
        {
            return _pedsInVehicles.ContainsKey(pedHandle);
        }

        public bool IsPedTryingToEnterVehicle(int pedHandle)
        {
            return _pedsTryingToEnterVehicle.Contains(pedHandle);
        }

        public int[] GetVehicleFreeSeats(int vehicleHandle)
        {
            if (!_vehicles.TryGetValue(vehicleHandle, out var vehicle))
            {
                return Array.Empty<int>();
            }

            return vehicle.GetFreeSeats();
        }

        public void TaskPedEnterVehicle(int pedHandle, int vehicleHandle, int seatIndex)
        {
            if (!_peds.ContainsKey(pedHandle) || !_vehicles.ContainsKey(vehicleHandle))
            {
                return;
            }

            // Mark ped as in vehicle and occupy seat
            _pedsInVehicles[pedHandle] = vehicleHandle;
            _vehicles[vehicleHandle].OccupySeat(seatIndex, pedHandle);
        }

        public void TaskPedLeaveVehicle(int pedHandle)
        {
            if (_pedsInVehicles.TryGetValue(pedHandle, out var vehicleHandle))
            {
                _pedsInVehicles.Remove(pedHandle);
                if (_vehicles.TryGetValue(vehicleHandle, out var vehicle))
                {
                    vehicle.FreeSeatByPed(pedHandle);
                }
            }
        }

        public void GivePedWeapon(int pedHandle, string weaponName)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Weapon = weaponName;
            }
        }

        public void SetPedAccuracy(int pedHandle, float accuracy)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Accuracy = accuracy;
            }
        }

        public void SetPedArmor(int pedHandle, int armor)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Armor = armor;
            }
        }

        public void SetPedHealth(int pedHandle, int health)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.Health = health;
            }
        }

        public void SetPedCombatAttributes(int pedHandle, bool canUseCover, bool willFightArmedPeds)
        {
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.CanUseCover = canUseCover;
                ped.WillFightArmedPeds = willFightArmedPeds;
            }
        }

        /// <summary>
        /// Gets or sets the current waypoint position, or null if no waypoint is set.
        /// </summary>
        public Vector3? WaypointPosition { get; private set; }

        /// <summary>
        /// Gets whether a waypoint is currently set.
        /// </summary>
        public bool HasWaypointSet => WaypointPosition.HasValue;

        public void SetWaypoint(Vector3 position)
        {
            WaypointPosition = position;
        }

        public void ClearWaypoint()
        {
            WaypointPosition = null;
        }

        public void SetPedToAttackPlayer(int pedHandle)
        {
            // Mock implementation - track that ped was set to attack
            if (_peds.TryGetValue(pedHandle, out var ped))
            {
                ped.IsAttackingPlayer = true;
            }
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
        /// Gets the weapon of a ped.
        /// </summary>
        public string GetPedWeapon(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Weapon : string.Empty;
        }

        /// <summary>
        /// Gets the accuracy of a ped.
        /// </summary>
        public float GetPedAccuracy(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Accuracy : 0.5f;
        }

        /// <summary>
        /// Gets the armor of a ped.
        /// </summary>
        public int GetPedArmor(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Armor : 0;
        }

        /// <summary>
        /// Gets the health of a ped.
        /// </summary>
        public int GetPedHealth(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) ? ped.Health : 100;
        }

        /// <summary>
        /// Gets whether a ped can use cover.
        /// </summary>
        public bool GetPedCanUseCover(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) && ped.CanUseCover;
        }

        /// <summary>
        /// Gets whether a ped will fight armed peds.
        /// </summary>
        public bool GetPedWillFightArmedPeds(int pedHandle)
        {
            return _peds.TryGetValue(pedHandle, out var ped) && ped.WillFightArmedPeds;
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
            _blipsCreated.Clear();
            _blipsDeleted.Clear();
            _blipColors.Clear();
            _followingPeds.Clear();
            _vehicles.Clear();
            _pedsInVehicles.Clear();
            _nextPedHandle = 1;
            _nextBlipHandle = 1;
            _nextVehicleHandle = 1000;
            PlayerPosition = Vector3.Zero;
            GameTime = 0;
            PlayerCharacterModel = "player_zero";
            PlayerHeading = 0f;
            IsPlayerDeadValue = false;
            PlayerMoney = 0;
            IsPlayerInVehicleValue = false;
            PlayerVehicleHandle = -1;
            WaypointPosition = null;
        }

        /// <summary>
        /// Creates a vehicle and returns its handle. For testing purposes.
        /// </summary>
        /// <param name="totalSeats">Total number of seats including driver.</param>
        /// <returns>The vehicle handle.</returns>
        public int CreateVehicle(int totalSeats = 4)
        {
            var handle = _nextVehicleHandle++;
            _vehicles[handle] = new VehicleState(totalSeats);
            return handle;
        }

        /// <summary>
        /// Sets up a vehicle for the player with the specified number of seats.
        /// </summary>
        /// <param name="totalSeats">Total number of seats including driver.</param>
        /// <returns>The vehicle handle.</returns>
        public int SetPlayerInVehicle(int totalSeats = 4)
        {
            var handle = CreateVehicle(totalSeats);
            IsPlayerInVehicleValue = true;
            PlayerVehicleHandle = handle;
            // Occupy driver seat (index 0) by player
            _vehicles[handle].OccupySeat(0, -1); // -1 for player
            return handle;
        }

        /// <summary>
        /// Puts a ped in a vehicle at a specific seat.
        /// </summary>
        public void PutPedInVehicle(int pedHandle, int vehicleHandle, int seatIndex)
        {
            _pedsInVehicles[pedHandle] = vehicleHandle;
            if (_vehicles.TryGetValue(vehicleHandle, out var vehicle))
            {
                vehicle.OccupySeat(seatIndex, pedHandle);
            }
        }

        private class PedState
        {
            public string ModelName { get; set; } = string.Empty;
            public Vector3 Position { get; set; }
            public bool IsAlive { get; set; }
            public string RelationshipGroup { get; set; } = string.Empty;
            public string Weapon { get; set; } = string.Empty;
            public float Accuracy { get; set; } = 0.5f;
            public int Armor { get; set; } = 0;
            public int Health { get; set; } = 100;
            public bool CanUseCover { get; set; } = false;
            public bool WillFightArmedPeds { get; set; } = false;
            public bool IsAttackingPlayer { get; set; } = false;
        }

        private class BlipState
        {
            public Vector3 Position { get; set; }
            public BlipColor Color { get; set; }
        }

        private class VehicleState
        {
            private readonly Dictionary<int, int> _occupiedSeats = new Dictionary<int, int>(); // seatIndex -> pedHandle (-1 for player)
            private readonly int _totalSeats;

            public VehicleState(int totalSeats)
            {
                _totalSeats = totalSeats;
            }

            public int[] GetFreeSeats()
            {
                var freeSeats = new List<int>();
                // Seat 0 is driver, 1+ are passengers
                for (int i = 0; i < _totalSeats; i++)
                {
                    if (!_occupiedSeats.ContainsKey(i))
                    {
                        freeSeats.Add(i);
                    }
                }
                return freeSeats.ToArray();
            }

            public void OccupySeat(int seatIndex, int pedHandle)
            {
                if (seatIndex >= 0 && seatIndex < _totalSeats)
                {
                    _occupiedSeats[seatIndex] = pedHandle;
                }
            }

            public void FreeSeatByPed(int pedHandle)
            {
                var seatToRemove = -1;
                foreach (var kvp in _occupiedSeats)
                {
                    if (kvp.Value == pedHandle)
                    {
                        seatToRemove = kvp.Key;
                        break;
                    }
                }
                if (seatToRemove >= 0)
                {
                    _occupiedSeats.Remove(seatToRemove);
                }
            }
        }
    }
}
