using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;
using DomainBlipColor = FactionWars.Core.Interfaces.BlipColor;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void SetPedAsFollower(int pedHandle)
        {
            try
            {
                FileLogger.Info($"SetPedAsFollower called for handle {pedHandle}");

                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Error($"SetPedAsFollower: Ped {pedHandle} doesn't exist");
                    return;
                }

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Error("SetPedAsFollower: Player doesn't exist");
                    return;
                }

                // Clear any existing tasks first
                ped.Task.ClearAllImmediately();

                // Set relationship to be friendly with the player
                var playerGroup = player.RelationshipGroup;
                ped.RelationshipGroup = playerGroup;
                FileLogger.Info($"Set ped {pedHandle} to player's relationship group");

                // Ensure followers hate the defender enemies group (used by hostile zone defenders)
                var defenderEnemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
                playerGroup.SetRelationshipBetweenGroups(defenderEnemyGroup, Relationship.Hate, true);

                AddPedToPlayerGroup(player, ped, pedHandle);

                ConfigureFollowerCombat(ped);

                FileLogger.Info($"Follower {pedHandle} configured: combat and follow behavior set");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedAsFollower exception", ex);
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
        public bool IsPedTryingToEnterVehicle(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return false;

                // Check if ped is getting into a vehicle using native
                return Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, ped.Handle);
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
        public int GetVehicleClass(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return -1;

                return Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicleHandle);
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetVehicleClass error", ex);
                return -1;
            }
        }

        /// <inheritdoc />
        public bool IsVehicleSeatTurret(int vehicleHandle, int seatIndex)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return false;

                // GTA V uses -1 for driver, 0+ for passengers
                // Our abstraction uses 0 for driver, 1+ for passengers
                var gtaSeatIndex = seatIndex - 1;
                return Function.Call<bool>(Hash.IS_TURRET_SEAT, vehicleHandle, gtaSeatIndex);
            }
            catch (Exception ex)
            {
                FileLogger.Error("IsVehicleSeatTurret error", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public DomainVector3 GetVehiclePosition(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return DomainVector3.Zero;

                var pos = vehicle.Position;
                return new DomainVector3(pos.X, pos.Y, pos.Z);
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetVehiclePosition error", ex);
                return DomainVector3.Zero;
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
                var gtaSeatIndex = seatIndex - 1;

                // Use native for better control - TASK_ENTER_VEHICLE with flags
                // Flags: 1 = warp if failed, 8 = don't wait for door to open, 16 = warp in
                // Timeout -1 = no timeout
                // Speed: 2.0 = run
                Function.Call(Hash.TASK_ENTER_VEHICLE,
                    ped.Handle,
                    vehicle.Handle,
                    -1,           // timeout - no timeout
                    gtaSeatIndex, // seat
                    2.0f,         // speed (run)
                    1,            // flag: 1 = normal entry
                    0);           // unknown
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
                var weaponHash = GetWeaponHash(weaponName);

                // Give the weapon with max ammo and equip it
                var weapon = ped.Weapons.Give(weaponHash, 9999, true, true);

                // Explicitly select the weapon to ensure they're holding it
                if (weapon != null)
                {
                    ped.Weapons.Select(weapon);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GivePedWeapon exception for ped {pedHandle}, weapon {weaponName}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedCanSwitchWeapons(int pedHandle, bool canSwitch)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                ped.CanSwitchWeapons = canSwitch;
                FileLogger.AI($"SetPedCanSwitchWeapons: ped {pedHandle} canSwitch={canSwitch}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedCanSwitchWeapons exception for ped {pedHandle}", ex);
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
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedAccuracy exception for ped {pedHandle}", ex);
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
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedArmor exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedHealth(int pedHandle, int health)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // CRITICAL: Disable instant headshot kills first
                // SET_PED_SUFFERS_CRITICAL_HITS(ped, false) - prevents one-shot headshot deaths
                Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, ped.Handle, false);

                // CRITICAL: Disable automatic death at low health
                // By default, GTA V kills peds when health drops below FatalInjuryHealthThreshold (~100)
                // This makes high health values ineffective since peds die at the threshold
                ped.DiesOnLowHealth = false;

                // Set injury thresholds very low so peds don't die until health is nearly zero
                // InjuryHealthThreshold: ped becomes "injured" (falls down) below this
                // FatalInjuryHealthThreshold: ped dies instantly when health drops below this
                ped.InjuryHealthThreshold = 1.0f;
                ped.FatalInjuryHealthThreshold = 0.0f;

                // Use natives directly for more reliable health setting
                // SET_PED_MAX_HEALTH is more reliable than setting MaxHealth property
                Function.Call(Hash.SET_PED_MAX_HEALTH, ped.Handle, health);
                Function.Call(Hash.SET_ENTITY_HEALTH, ped.Handle, health, 0);

                // Also set the property as backup
                ped.MaxHealth = health;
                ped.Health = health;

                FileLogger.Info($"SetPedHealth: Set ped {pedHandle} health to {health}, DiesOnLowHealth=false, FatalInjuryThreshold=0");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedHealth exception for ped {pedHandle}", ex);
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
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedCombatAttributes exception for ped {pedHandle}", ex);
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

        /// <inheritdoc />
    }
}
