using System;
using System.IO;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
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
            FileLogger.Spawn($"CreatePed called: model={modelName}, pos=({position.X:F1}, {position.Y:F1}, {position.Z:F1})");

            try
            {
                var model = new Model(modelName);
                FileLogger.Spawn($"Model created: IsValid={model.IsValid}, Hash={model.Hash}");

                if (!model.IsValid)
                {
                    FileLogger.Error($"Model '{modelName}' is not valid!");
                    GTA.UI.Notification.Show($"~r~Invalid model: {modelName}");
                    return -1;
                }

                // Request model with longer timeout
                model.Request(5000);
                FileLogger.Spawn($"Model requested, waiting for load...");

                // Wait for model to load (blocking call)
                int waitCounter = 0;
                while (!model.IsLoaded && waitCounter < 100)
                {
                    Script.Wait(10);
                    waitCounter++;
                }

                FileLogger.Spawn($"Model load wait complete: waitCounter={waitCounter}, IsLoaded={model.IsLoaded}");

                if (!model.IsLoaded)
                {
                    FileLogger.Error($"Model '{modelName}' failed to load after {waitCounter * 10}ms");
                    GTA.UI.Notification.Show($"~r~Model failed to load: {modelName}");
                    return -1;
                }

                // Get ground Z coordinate for proper placement
                var gtaPosition = new GTA.Math.Vector3(position.X, position.Y, position.Z);
                FileLogger.Spawn($"Initial position: ({gtaPosition.X:F1}, {gtaPosition.Y:F1}, {gtaPosition.Z:F1})");

                // Try to get ground Z at position
                float groundZ = 0f;
                bool gotGround = World.GetGroundHeight(new GTA.Math.Vector3(position.X, position.Y, position.Z + 100f), out groundZ);
                FileLogger.Spawn($"Ground height check: success={gotGround}, groundZ={groundZ:F1}");

                if (gotGround && groundZ > 0)
                {
                    gtaPosition.Z = groundZ + 1f; // Spawn slightly above ground
                    FileLogger.Spawn($"Adjusted Z to ground: {gtaPosition.Z:F1}");
                }
                else
                {
                    FileLogger.Warn($"Could not get ground height, using original Z={gtaPosition.Z:F1}");
                }

                // Create ped facing player
                var playerPos = Game.Player.Character.Position;
                var toPlayer = playerPos - gtaPosition;
                float heading = (float)(Math.Atan2(toPlayer.X, toPlayer.Y) * 180.0 / Math.PI);
                FileLogger.Spawn($"Creating ped at ({gtaPosition.X:F1}, {gtaPosition.Y:F1}, {gtaPosition.Z:F1}), heading={heading:F1}");

                var ped = World.CreatePed(model, gtaPosition, heading);

                model.MarkAsNoLongerNeeded();

                if (ped == null)
                {
                    FileLogger.Error("World.CreatePed returned null!");
                    GTA.UI.Notification.Show("~r~Ped creation failed (null)!");
                    return -1;
                }

                if (!ped.Exists())
                {
                    FileLogger.Error("Ped created but doesn't exist!");
                    GTA.UI.Notification.Show("~r~Ped creation failed (not exists)!");
                    return -1;
                }

                FileLogger.Spawn($"Ped created successfully: Handle={ped.Handle}, Health={ped.Health}");

                // Make ped persistent so they don't despawn
                ped.IsPersistent = true;

                // NOTE: Don't set combat task here - caller will configure based on ped type
                // (enemy defenders vs friendly followers have different behaviors)
                FileLogger.Spawn($"Ped configured: Persistent=true, ready for combat configuration");

                return ped.Handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error("Exception in CreatePed", ex);
                GTA.UI.Notification.Show($"~r~CreatePed error: {ex.Message}");
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
            catch (Exception ex)
            {
                FileLogger.Error($"DeletePed exception for ped {pedHandle}", ex);
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
            catch (Exception ex)
            {
                FileLogger.Error($"IsPedAlive exception for ped {pedHandle}", ex);
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
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedRelationshipGroup exception for ped {pedHandle}, group {groupName}", ex);
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
        public void SetBlipSprite(int blipHandle, int spriteId)
        {
            try
            {
                var blip = new Blip(blipHandle);
                if (!blip.Exists()) return;

                blip.Sprite = (BlipSprite)spriteId;
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetBlipName(int blipHandle, string name)
        {
            try
            {
                var blip = new Blip(blipHandle);
                if (!blip.Exists()) return;

                blip.Name = name;
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

                // Add to player's ped group so they move together
                var pedGroup = player.PedGroup;
                if (pedGroup != null)
                {
                    pedGroup.Add(ped, false);
                    FileLogger.Info($"Added ped {pedHandle} to player's ped group");
                }
                else
                {
                    // Create a new group if player doesn't have one
                    pedGroup = new PedGroup();
                    pedGroup.Add(player, true); // Player is leader
                    pedGroup.Add(ped, false);
                    FileLogger.Info($"Created new ped group with player as leader");
                }

                // Configure ped for bodyguard behavior
                ped.IsPersistent = true;
                ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                ped.BlockPermanentEvents = false; // Allow them to react to combat
                ped.CanSwitchWeapons = true;

                // Set combat attributes for aggressive defense
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);  // BF_CanFightArmedPedsWhenNotArmed
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);   // BF_CanUseCover
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 0, false);  // BF_CanUseCoverShootOnlyWhenAimingAtTarget - false for more aggressive
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);   // BF_CanDoDrivebys
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 3, true);   // BF_CanLeaveVehicle
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 20, true);  // BF_CanTauntInVehicle
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 1, true);   // BF_CanBeTargetedWhenInjured
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 52, true);  // BF_CanBeTargetedByScriptedPeds

                // Set combat ability and range
                Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2); // Professional
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);   // Far
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped.Handle, 2); // Offensive

                // Set firing pattern
                ped.FiringPattern = FiringPattern.FullAuto;

                // Register as group member
                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, ped.Handle, Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player.Handle));

                // Use TASK_COMBAT_HATED_TARGETS_AROUND_PED to fight any hostile peds nearby
                // This makes them automatically engage enemies
                Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped.Handle, 100f, 0);

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
                var weaponHash = (WeaponHash)Game.GenerateHash(weaponName.ToUpperInvariant());

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

                // Use natives directly for more reliable health setting
                // SET_PED_MAX_HEALTH is more reliable than setting MaxHealth property
                Function.Call(Hash.SET_PED_MAX_HEALTH, ped.Handle, health);
                Function.Call(Hash.SET_ENTITY_HEALTH, ped.Handle, health, 0);

                // Also set the property as backup
                ped.MaxHealth = health;
                ped.Health = health;

                FileLogger.Info($"SetPedHealth: Set ped {pedHandle} health to {health}, critical hits disabled");
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
        public void SetPedToAttackPlayer(int pedHandle)
        {
            try
            {
                FileLogger.Combat($"SetPedToAttackPlayer called for handle {pedHandle}");

                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Error($"SetPedToAttackPlayer: Ped {pedHandle} doesn't exist");
                    return;
                }

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Error("SetPedToAttackPlayer: Player doesn't exist");
                    return;
                }

                // Create or get an enemy relationship group for defenders
                var enemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
                var playerGroup = player.RelationshipGroup;

                // Set ped to enemy group
                ped.RelationshipGroup = enemyGroup;

                // Make the groups hate each other
                enemyGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Hate, true);

                // Configure ped for aggressive combat
                ped.IsPersistent = true;
                ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                ped.BlockPermanentEvents = false; // Allow them to react

                // Set combat attributes for aggressive behavior
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // BF_CanFightArmedPedsWhenNotArmed
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);  // BF_CanUseCover
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 0, false); // BF_CanUseCoverShootOnlyWhenAimingAtTarget
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // BF_CanFightArmedPedsWhenNotArmed

                // Set combat ability and range
                Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2); // Professional
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);   // Far
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped.Handle, 2); // Offensive

                // Set firing pattern for aggression
                ped.FiringPattern = FiringPattern.FullAuto;

                // CRITICAL: Give the ped a task to fight the player
                ped.Task.ClearAllImmediately();
                ped.Task.Combat(player);

                FileLogger.Combat($"Defender {pedHandle} set to attack player with Combat task");
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPedToAttackPlayer exception", ex);
            }
        }

        /// <inheritdoc />
        public int CreateBlipForPed(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return -1;

                var blip = ped.AddBlip();
                return blip?.Handle ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <inheritdoc />
        public void TaskPedWanderInArea(int pedHandle, DomainVector3 center, float radius)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                var gtaCenter = new GTA.Math.Vector3(center.X, center.Y, center.Z);
                Function.Call(Hash.TASK_WANDER_IN_AREA, ped.Handle, gtaCenter.X, gtaCenter.Y, gtaCenter.Z, radius, 0f, 0f);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void SetPedAsFriendly(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // Set relationship to the player's group so they are friendly with player and followers
                var playerGroup = player.RelationshipGroup;
                ped.RelationshipGroup = playerGroup;

                // Ensure they hate the defender enemies group (hostile zone defenders)
                var defenderEnemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
                playerGroup.SetRelationshipBetweenGroups(defenderEnemyGroup, Relationship.Hate, true);

                // Configure combat attributes to help the player
                ped.IsPersistent = true;
                ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                ped.BlockPermanentEvents = false;

                // Enable combat attributes
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // Can fight armed peds
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);  // Can use cover
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public float GetGroundZ(float x, float y, float z)
        {
            try
            {
                var outArg = new OutputArgument();
                bool found = Function.Call<bool>(
                    Hash.GET_GROUND_Z_FOR_3D_COORD,
                    x, y, z + 100f,  // Start search from above
                    outArg,
                    false,  // ignoreWater
                    false); // ignoreDistToWaterLevelCheck

                if (found)
                {
                    return outArg.GetResult<float>();
                }

                FileLogger.Warn($"GetGroundZ: No ground found at ({x:F1}, {y:F1}, {z:F1}), returning input Z");
                return z;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GetGroundZ exception at ({x:F1}, {y:F1}, {z:F1})", ex);
                return z;
            }
        }

        /// <inheritdoc />
        public DomainVector3 GetSafeCoordForPed(DomainVector3 position)
        {
            try
            {
                // GET_SAFE_COORD_FOR_PED uses navmesh to find walkable ground positions
                // Native signature: BOOL GET_SAFE_COORD_FOR_PED(float x, float y, float z, BOOL onGround, Vector3* outPosition, int flags)
                var outCoord = new OutputArgument();
                bool found = Function.Call<bool>(
                    Hash.GET_SAFE_COORD_FOR_PED,
                    position.X, position.Y, position.Z,
                    true,       // onGround - only return ground-level positions
                    outCoord,   // Vector3 output
                    0);         // flags - pedestrian mode

                if (found)
                {
                    var result = outCoord.GetResult<GTA.Math.Vector3>();
                    FileLogger.Spawn($"GetSafeCoordForPed: Found safe coord ({result.X:F1}, {result.Y:F1}, {result.Z:F1}) for input ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");
                    return new DomainVector3(result.X, result.Y, result.Z);
                }

                // Fallback: try GET_CLOSEST_VEHICLE_NODE_WITH_HEADING which often has better coverage
                // Native signature: BOOL GET_CLOSEST_VEHICLE_NODE_WITH_HEADING(float x, float y, float z, Vector3* outPosition, float* outHeading, int nodeType, float p7, int p8)
                var outNodeCoord = new OutputArgument();
                var outHeading = new OutputArgument();
                found = Function.Call<bool>(
                    Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,
                    position.X, position.Y, position.Z,
                    outNodeCoord,
                    outHeading,
                    1,      // nodeType - any road
                    3.0f,   // p7
                    0);     // p8

                if (found)
                {
                    var nodeResult = outNodeCoord.GetResult<GTA.Math.Vector3>();
                    FileLogger.Spawn($"GetSafeCoordForPed: Using vehicle node ({nodeResult.X:F1}, {nodeResult.Y:F1}, {nodeResult.Z:F1}) as fallback");
                    return new DomainVector3(nodeResult.X, nodeResult.Y, nodeResult.Z);
                }

                // Final fallback: use GetGroundZ
                FileLogger.Warn($"GetSafeCoordForPed: No safe coord found, using GetGroundZ fallback");
                var groundZ = GetGroundZ(position.X, position.Y, position.Z);
                return new DomainVector3(position.X, position.Y, groundZ);
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetSafeCoordForPed exception", ex);
                return position;
            }
        }

        /// <inheritdoc />
        public void SetPedAsHostileWanderer(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // Create or get enemy relationship group
                var enemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
                var playerGroup = player.RelationshipGroup;

                // Set ped to enemy group
                ped.RelationshipGroup = enemyGroup;

                // Make the groups hate each other (bidirectional)
                enemyGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Hate, true);

                // Configure ped for patrol + engage behavior
                ped.IsPersistent = true;
                ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                ped.BlockPermanentEvents = false; // Allow reaction to enemies

                // Set combat attributes
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // CanFightArmedPedsWhenNotArmed
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);  // CanUseCover
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);  // CanDoDrivebys

                // Set combat ability and range
                Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2); // Professional
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);   // Far

                // Set alertness high so they notice enemies while wandering
                Function.Call(Hash.SET_PED_ALERTNESS, ped.Handle, 3); // Full alertness

                // DO NOT give Combat task - let them wander and engage via relationship system
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPedAsHostileWanderer exception", ex);
            }
        }

        /// <inheritdoc />
        public string GetScriptsDirectory()
        {
            // ScriptHookVDotNet runs with GTA V installation folder as working directory
            // The scripts folder is a subdirectory of that
            return Path.Combine(Environment.CurrentDirectory, "scripts");
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
