using System;
using FactionWars.Core.Interfaces;
using GTA;
using GTA.Math;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;
using DomainBlipColor = FactionWars.Core.Interfaces.BlipColor;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Implementation of IGameBridge using ScriptHookVDotNet3 native calls.
    /// This bridges the domain layer to actual GTA V game functionality.
    /// </summary>
    public class GameBridge : IGameBridge
    {
        /// <inheritdoc />
        public DomainVector3 GetPlayerPosition()
        {
            var pos = Game.Player.Character.Position;
            return new DomainVector3(pos.X, pos.Y, pos.Z);
        }

        /// <inheritdoc />
        public int CreatePed(string modelName, DomainVector3 position)
        {
            try
            {
                var model = new Model(modelName);
                model.Request(1000);

                if (!model.IsLoaded)
                {
                    return -1;
                }

                var gtaPosition = new GTA.Math.Vector3(position.X, position.Y, position.Z);
                var ped = World.CreatePed(model, gtaPosition);

                model.MarkAsNoLongerNeeded();

                if (ped == null)
                {
                    return -1;
                }

                return ped.Handle;
            }
            catch
            {
                return -1;
            }
        }

        /// <inheritdoc />
        public void DeletePed(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped != null && ped.Exists())
                {
                    ped.Delete();
                }
            }
            catch
            {
                // Silently ignore - ped may already be gone
            }
        }

        /// <inheritdoc />
        public bool IsPedAlive(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                return ped != null && ped.Exists() && ped.IsAlive;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public void SetPedRelationshipGroup(int pedHandle, string groupName)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // Get or create the relationship group hash
                var groupHash = World.AddRelationshipGroup(groupName);
                ped.RelationshipGroup = groupHash;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public int CreateBlip(DomainVector3 position)
        {
            try
            {
                var gtaPosition = new GTA.Math.Vector3(position.X, position.Y, position.Z);
                var blip = World.CreateBlip(gtaPosition);
                return blip?.Handle ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <inheritdoc />
        public void DeleteBlip(int blipHandle)
        {
            try
            {
                var blip = new Blip(blipHandle);
                if (blip.Exists())
                {
                    blip.Delete();
                }
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetBlipColor(int blipHandle, DomainBlipColor color)
        {
            try
            {
                var blip = new Blip(blipHandle);
                if (!blip.Exists()) return;

                // Convert our BlipColor enum to GTA's BlipColor
                blip.Color = ConvertBlipColor(color);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void ShowNotification(string message)
        {
            try
            {
                GTA.UI.Notification.Show(message);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public int GetGameTime()
        {
            return Game.GameTime;
        }

        /// <inheritdoc />
        public bool RevivePed(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return false;

                if (ped.IsDead)
                {
                    ped.Resurrect();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public void SetPedPosition(int pedHandle, DomainVector3 position)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                var gtaPosition = new GTA.Math.Vector3(position.X, position.Y, position.Z);
                ped.Position = gtaPosition;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public bool SetPedModel(int pedHandle, string modelName)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return false;

                var model = new Model(modelName);
                model.Request(1000);

                if (!model.IsLoaded)
                {
                    return false;
                }

                // Note: SetPedModel is complex in GTA V, typically you'd delete and recreate
                // For now, return false as this operation is non-trivial
                model.MarkAsNoLongerNeeded();
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public string GetPlayerCharacterModel()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return string.Empty;

                // Get the model hash and convert to model name
                var model = player.Model;

                // GTA V protagonist model hashes - we compare against known protagonist hashes
                // Michael: 225514697 (player_zero)
                // Franklin: 2602752943 (player_one)
                // Trevor: 2608926626 (player_two)
                var modelHash = model.Hash;

                // Check against known protagonist models
                if (model == new Model("player_zero")) return "player_zero";
                if (model == new Model("player_one")) return "player_one";
                if (model == new Model("player_two")) return "player_two";

                // For other ped models, return the hash as a string for debugging
                return modelHash.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public float GetPlayerHeading()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return 0f;

                return player.Heading;
            }
            catch
            {
                return 0f;
            }
        }

        /// <inheritdoc />
        public bool IsPlayerDead()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                return player.IsDead;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int GetPlayerMoney()
        {
            try
            {
                return Game.Player.Money;
            }
            catch
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public void AddPlayerMoney(int amount)
        {
            try
            {
                Game.Player.Money += amount;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetPedAsFollower(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                var player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                // Set relationship to be friendly with the player
                var playerGroup = player.RelationshipGroup;
                ped.RelationshipGroup = playerGroup;

                // Add to player's ped group
                var pedGroup = player.PedGroup;
                if (pedGroup != null)
                {
                    pedGroup.Add(ped, false);
                }

                // Set as companion - task them to follow and guard the player
                ped.Task.ClearAllImmediately();
                ped.AlwaysKeepTask = true;
                ped.BlockPermanentEvents = true;

                // Make the ped follow the player
                // Use native call for TASK_FOLLOW_TO_OFFSET_OF_ENTITY with combat behavior
                Function.Call(Hash.TASK_FOLLOW_TO_OFFSET_OF_ENTITY,
                    ped.Handle,
                    player.Handle,
                    0f, -2f, 0f, // Offset: slightly behind player
                    5.0f, // Movement speed
                    -1, // Timeout (-1 = indefinite)
                    2.0f, // Stop range
                    true // Persist following
                );

                // Make them fight any enemies
                ped.FiringPattern = FiringPattern.FullAuto;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public bool IsPlayerInVehicle()
        {
            try
            {
                var player = Game.Player.Character;
                return player != null && player.Exists() && player.IsInVehicle();
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int GetPlayerVehicle()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists() || !player.IsInVehicle())
                    return -1;

                var vehicle = player.CurrentVehicle;
                return vehicle?.Handle ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <inheritdoc />
        public bool IsPedInVehicle(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                return ped != null && ped.Exists() && ped.IsInVehicle();
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int[] GetVehicleFreeSeats(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return Array.Empty<int>();

                var freeSeats = new System.Collections.Generic.List<int>();
                var passengerCapacity = vehicle.PassengerCapacity;

                // Check passenger seats (indices 0-N, but index -1 is driver in GTA V native calls)
                // In our abstraction, 0 = driver, 1+ = passengers
                // Note: driver seat index in natives is -1, passenger seats are 0-based
                for (int i = 0; i < passengerCapacity; i++)
                {
                    if (vehicle.IsSeatFree((VehicleSeat)i))
                    {
                        // Map GTA V seat index (0-based passengers) to our abstraction (1-based for passengers)
                        freeSeats.Add(i + 1);
                    }
                }

                return freeSeats.ToArray();
            }
            catch
            {
                return Array.Empty<int>();
            }
        }

        /// <inheritdoc />
        public void TaskPedEnterVehicle(int pedHandle, int vehicleHandle, int seatIndex)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;

                if (ped == null || !ped.Exists() || vehicle == null || !vehicle.Exists())
                    return;

                // Convert our seat index (1-based for passengers) to GTA V seat index (0-based)
                // Our 0 = driver (-1 in GTA), our 1+ = passengers (0+ in GTA)
                var gtaSeatIndex = (VehicleSeat)(seatIndex - 1);

                ped.Task.EnterVehicle(vehicle, gtaSeatIndex);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void TaskPedLeaveVehicle(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists() || !ped.IsInVehicle())
                    return;

                ped.Task.LeaveVehicle();
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void GivePedWeapon(int pedHandle, string weaponName)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // Get weapon hash from name
                var weaponHash = (WeaponHash)Game.GenerateHash(weaponName.ToUpperInvariant());

                // Give the weapon with max ammo
                ped.Weapons.Give(weaponHash, 9999, true, true);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetPedAccuracy(int pedHandle, float accuracy)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // GTA V accuracy is 0-100 integer, we use 0.0-1.0 float
                int gtaAccuracy = (int)(accuracy * 100f);
                ped.Accuracy = Math.Max(0, Math.Min(100, gtaAccuracy));
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetPedArmor(int pedHandle, int armor)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                ped.Armor = Math.Max(0, armor);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetPedHealth(int pedHandle, int health)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // Set max health first, then current health
                ped.MaxHealth = health;
                ped.Health = health;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetPedCombatAttributes(int pedHandle, bool canUseCover, bool willFightArmedPeds)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // Combat attribute flags:
                // 0: CanUseCover
                // 5: CanFightArmedPedsWhenNotArmed
                // 46: AlwaysFight
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 0, canUseCover);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, willFightArmedPeds);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, willFightArmedPeds);

                // Make them engage in combat when in combat with someone
                ped.CanSwitchWeapons = true;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetWaypoint(DomainVector3 position)
        {
            try
            {
                // SET_NEW_WAYPOINT native sets a waypoint on the map
                // The player must travel there manually - no teleportation
                Function.Call(Hash.SET_NEW_WAYPOINT, position.X, position.Y);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void ClearWaypoint()
        {
            try
            {
                // SET_WAYPOINT_OFF native removes any active waypoint
                Function.Call(Hash.SET_WAYPOINT_OFF);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <summary>
        /// Converts our domain BlipColor enum to GTA V's BlipColor.
        /// </summary>
        private GTA.BlipColor ConvertBlipColor(DomainBlipColor color)
        {
            return color switch
            {
                DomainBlipColor.White => GTA.BlipColor.White,
                DomainBlipColor.Red => GTA.BlipColor.Red,
                DomainBlipColor.Green => GTA.BlipColor.Green,
                DomainBlipColor.Blue => GTA.BlipColor.Blue,
                DomainBlipColor.Yellow => GTA.BlipColor.Yellow,
                DomainBlipColor.Orange => GTA.BlipColor.Orange,
                DomainBlipColor.Purple => GTA.BlipColor.Purple,
                DomainBlipColor.Pink => GTA.BlipColor.Pink,
                DomainBlipColor.MichaelBlue => GTA.BlipColor.Michael,
                DomainBlipColor.FranklinGreen => GTA.BlipColor.Franklin,
                DomainBlipColor.TrevorOrange => GTA.BlipColor.Trevor,
                _ => GTA.BlipColor.White
            };
        }
    }
}
