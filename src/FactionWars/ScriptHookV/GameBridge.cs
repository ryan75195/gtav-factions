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

                // CRITICAL: Request collision at the spawn location first
                // GET_GROUND_Z_FOR_3D_COORD fails when collisions aren't loaded
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, position.X, position.Y, position.Z);

                // Wait a bit for collision to load
                int collisionWait = 0;
                while (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, Game.Player.Character.Handle) && collisionWait < 10)
                {
                    Script.Wait(10);
                    collisionWait++;
                }
                FileLogger.Spawn($"Collision request complete after {collisionWait * 10}ms");

                // Try to get ground Z at position using native with OutputArgument
                float groundZ = 0f;
                bool gotGround = false;
                var outArg = new OutputArgument();
                gotGround = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
                    position.X, position.Y, position.Z + 100f,
                    outArg,
                    false,  // ignoreWater
                    false); // ignoreDistToWaterLevelCheck

                if (gotGround)
                {
                    groundZ = outArg.GetResult<float>();
                }
                FileLogger.Spawn($"Ground height check: success={gotGround}, groundZ={groundZ:F1}");

                if (gotGround && groundZ > 0)
                {
                    // IMPORTANT: Don't add offset - World.CreatePed may already adjust Z
                    // Spawn at exact ground level, game will place ped properly
                    gtaPosition.Z = groundZ;
                    FileLogger.Spawn($"Adjusted Z to ground: {gtaPosition.Z:F1}");
                }
                else
                {
                    // Fallback: use player's Z as reference if nearby
                    var fallbackPlayerPos = Game.Player.Character.Position;
                    float distToPlayer = (float)Math.Sqrt(
                        Math.Pow(position.X - fallbackPlayerPos.X, 2) +
                        Math.Pow(position.Y - fallbackPlayerPos.Y, 2));

                    if (distToPlayer < 300f)
                    {
                        gtaPosition.Z = fallbackPlayerPos.Z;
                        FileLogger.Spawn($"Using player Z as fallback: {gtaPosition.Z:F1}");
                    }
                    else
                    {
                        FileLogger.Warn($"Could not get ground height, using original Z={gtaPosition.Z:F1}");
                    }
                }

                // Create ped facing player
                var playerPos = Game.Player.Character.Position;
                var toPlayer = playerPos - gtaPosition;
                float heading = (float)(Math.Atan2(toPlayer.X, toPlayer.Y) * 180.0 / Math.PI);
                FileLogger.Spawn($"Creating ped at ({gtaPosition.X:F1}, {gtaPosition.Y:F1}, {gtaPosition.Z:F1}), heading={heading:F1}");

                var ped = World.CreatePed(model, gtaPosition, heading);

                // After creating ped, force them to ground level using SET_ENTITY_COORDS
                // This ensures they're on the navmesh and can walk
                if (ped != null && ped.Exists() && gotGround && groundZ > 0)
                {
                    Function.Call(Hash.SET_ENTITY_COORDS, ped.Handle, gtaPosition.X, gtaPosition.Y, groundZ, false, false, false, true);
                    FileLogger.Spawn($"Forced ped to ground Z={groundZ:F1}");
                }

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
                if (ped == null || !ped.Exists())
                    return false;

                // Check multiple conditions since DiesOnLowHealth=false can affect IsAlive behavior
                // A ped is dead if: IsDead is true, Health <= 0, or IsAlive is false
                if (ped.IsDead || ped.Health <= 0)
                    return false;

                return ped.IsAlive;
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
        public int CreateRadiusBlip(DomainVector3 center, float radius)
        {
            try
            {
                int handle = Function.Call<int>(Hash.ADD_BLIP_FOR_RADIUS, center.X, center.Y, center.Z, radius);
                return handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error("CreateRadiusBlip exception", ex);
                return -1;
            }
        }

        /// <inheritdoc />
        public void SetBlipAlpha(int blipHandle, int alpha)
        {
            try
            {
                Function.Call(Hash.SET_BLIP_ALPHA, blipHandle, alpha);
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetBlipAlpha exception", ex);
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
        public int GetWantedLevel()
        {
            try
            {
                var level = Game.Player.WantedLevel;
                if (level > 0)
                    FileLogger.Combat($"GetWantedLevel: player has wanted level {level}");
                return level;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetWantedLevel exception", ex);
                return 0;
            }
        }

        /// <inheritdoc />
        public bool ConsumePlayerDamagedByPedFlag()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                // HAS_ENTITY_BEEN_DAMAGED_BY_ANY_PED reads the engine-set "damaged by ped"
                // flag. Second argument (updateHasBeenDamagedThisFrame=true) marks the frame
                // as processed so subsequent same-tick reads don't see stale state, matching
                // the conventional one-shot consume behaviour. We pair it with
                // CLEAR_ENTITY_LAST_DAMAGE_ENTITY so the next call only returns true if NEW
                // damage has occurred.
                var damaged = Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ANY_PED, player.Handle, 1);
                if (damaged)
                {
                    FileLogger.Combat("ConsumePlayerDamagedByPedFlag: player damaged by ped, clearing flag");
                    Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, player.Handle);
                }
                return damaged;
            }
            catch (Exception ex)
            {
                FileLogger.Error("ConsumePlayerDamagedByPedFlag exception", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public int GetPlayerPedHandle()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return -1;
                return player.Handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetPlayerPedHandle exception", ex);
                return -1;
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
        public void SetPlayerMoney(int amount)
        {
            try
            {
                Game.Player.Money = amount;
                FileLogger.Info($"SetPlayerMoney: Set player money to ${amount:N0}");
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPlayerMoney exception", ex);
            }
        }

        /// <inheritdoc />
        public long? GetTotalPlayTimeSeconds()
        {
            try
            {
                int activeChar = GetActiveSpCharacterIndex();
                string statName = $"SP{activeChar}_TOTAL_PLAYING_TIME";
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, statName);
                var outArg = new OutputArgument();
                bool ok = Function.Call<bool>(Hash.STAT_GET_INT, hash, outArg, -1);
                if (!ok) return null;
                return outArg.GetResult<int>() / 1000L;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetTotalPlayTimeSeconds exception", ex);
                return null;
            }
        }

        public int GetActiveCharacterIndex() => GetActiveSpCharacterIndex();

        /// <inheritdoc />
        public int GetCompletedMissionCount()
        {
            try
            {
                int activeChar = GetActiveSpCharacterIndex();
                string statName = $"SP{activeChar}_TOTAL_MISSIONS_PASSED";
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, statName);
                var outArg = new OutputArgument();
                bool ok = Function.Call<bool>(Hash.STAT_GET_INT, hash, outArg, -1);
                int valueOut = ok ? outArg.GetResult<int>() : 0;
                FileLogger.Debug($"GetCompletedMissionCount: stat={statName} ok={ok} value={valueOut}");
                return valueOut;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetCompletedMissionCount exception", ex);
                return 0;
            }
        }

        /// <inheritdoc />
        public int GetInGameClockMinutes()
        {
            try
            {
                int hours = Function.Call<int>(Hash.GET_CLOCK_HOURS);
                int minutes = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
                int total = hours * 60 + minutes;
                FileLogger.Debug($"GetInGameClockMinutes: {hours:D2}:{minutes:D2} = {total}");
                return total;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetInGameClockMinutes exception", ex);
                return 0;
            }
        }

        private int GetActiveSpCharacterIndex()
        {
            string model = GetPlayerCharacterModel();
            if (string.Equals(model, "player_one", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(model, "player_two", StringComparison.OrdinalIgnoreCase)) return 2;
            return 0;
        }

        /// <inheritdoc />
        public void RemoveAllPlayerWeapons()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("RemoveAllPlayerWeapons: Player doesn't exist");
                    return;
                }

                // REMOVE_ALL_PED_WEAPONS native removes all weapons from a ped
                Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, player.Handle, true);
                FileLogger.Info("RemoveAllPlayerWeapons: Removed all player weapons");
            }
            catch (Exception ex)
            {
                FileLogger.Error("RemoveAllPlayerWeapons exception", ex);
            }
        }

        /// <inheritdoc />
        public void GivePlayerWeapon(string weaponName, int ammo)
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("GivePlayerWeapon: Player doesn't exist");
                    return;
                }

                // Get weapon hash from name
                var weaponHash = (WeaponHash)Game.GenerateHash(weaponName.ToUpperInvariant());

                // Give the weapon with specified ammo and equip it
                var weapon = player.Weapons.Give(weaponHash, ammo, true, true);

                // Explicitly select the weapon
                if (weapon != null)
                {
                    player.Weapons.Select(weapon);
                }

                FileLogger.Info($"GivePlayerWeapon: Gave player {weaponName} with {ammo} ammo");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GivePlayerWeapon exception for weapon {weaponName}", ex);
            }
        }

        /// <inheritdoc />
        public void ConfigurePlayerSettings()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("ConfigurePlayerSettings: Player doesn't exist");
                    return;
                }

                // Prevent player from dropping weapons when killed
                // This makes weapons persist across deaths
                Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD, player.Handle, false);

                FileLogger.Info("ConfigurePlayerSettings: Player weapon drop on death disabled");
            }
            catch (Exception ex)
            {
                FileLogger.Error("ConfigurePlayerSettings exception", ex);
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
            FileLogger.AI($"TaskPedWanderInArea: CALLED for ped {pedHandle} at center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskPedWanderInArea: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                FileLogger.AI($"TaskPedWanderInArea: Ped {pedHandle} exists, position=({ped.Position.X:F1}, {ped.Position.Y:F1}, {ped.Position.Z:F1})");

                // Clear any existing tasks first
                ped.Task.ClearAllImmediately();
                FileLogger.AI($"TaskPedWanderInArea: Cleared existing tasks for ped {pedHandle}");

                // NOTE: Removed SET_BLOCKING_OF_NON_TEMPORARY_EVENTS - it may prevent wandering
                // Instead, let the ped be able to respond to events while wandering

                // Use TASK_WANDER_STANDARD instead - simpler and more reliable
                // Parameters: ped, heading (10.0 = random direction), flags (0 = default)
                Function.Call(Hash.TASK_WANDER_STANDARD, ped.Handle, 10.0f, 0);
                FileLogger.AI($"TaskPedWanderInArea: TASK_WANDER_STANDARD called for ped {pedHandle}");

                // Set movement blend ratio for walking (1.0 = walk)
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 1.0f);
                FileLogger.AI($"TaskPedWanderInArea: SET_PED_DESIRED_MOVE_BLEND_RATIO=1.0 (walk) for ped {pedHandle}");

                // Check if task is active (task ID 222 = wander)
                bool isWandering = Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped.Handle, 222);
                FileLogger.AI($"TaskPedWanderInArea: Task active check (222) = {isWandering} for ped {pedHandle}");

                FileLogger.AI($"TaskPedWanderInArea: COMPLETED successfully for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedWanderInArea exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskPedWanderInAreaSprinting(int pedHandle, DomainVector3 center, float radius)
        {
            FileLogger.AI($"TaskPedWanderInAreaSprinting: CALLED for ped {pedHandle} at center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskPedWanderInAreaSprinting: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                FileLogger.AI($"TaskPedWanderInAreaSprinting: Ped {pedHandle} exists, position=({ped.Position.X:F1}, {ped.Position.Y:F1}, {ped.Position.Z:F1})");

                // Clear any existing tasks first
                ped.Task.ClearAllImmediately();
                FileLogger.AI($"TaskPedWanderInAreaSprinting: Cleared existing tasks for ped {pedHandle}");

                // NOTE: Removed SET_BLOCKING_OF_NON_TEMPORARY_EVENTS - it prevents wandering from working
                // Use TASK_WANDER_STANDARD - simpler and more reliable
                Function.Call(Hash.TASK_WANDER_STANDARD, ped.Handle, 10.0f, 0);
                FileLogger.AI($"TaskPedWanderInAreaSprinting: TASK_WANDER_STANDARD called for ped {pedHandle}");

                // Set movement blend ratio for sprinting (3.0 = sprint)
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 3.0f);
                FileLogger.AI($"TaskPedWanderInAreaSprinting: SET_PED_DESIRED_MOVE_BLEND_RATIO=3.0 (sprint) for ped {pedHandle}");

                FileLogger.AI($"TaskPedWanderInAreaSprinting: COMPLETED successfully for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedWanderInAreaSprinting exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskCombatHatedTargetsAroundPed(int pedHandle, float radius)
        {
            FileLogger.AI($"TaskCombatHatedTargetsAroundPed: CALLED for ped {pedHandle} radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskCombatHatedTargetsAroundPed: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // Task the ped to actively seek out and fight any hated targets within range
                // This makes them run towards enemies and engage in combat
                Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped.Handle, radius, 0);

                // Set movement blend ratio for sprinting (3.0 = sprint) so they run towards enemies
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 3.0f);

                FileLogger.AI($"TaskCombatHatedTargetsAroundPed: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskCombatHatedTargetsAroundPed exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskGoToEntity(int pedHandle, int targetEntityHandle, float stoppingRange)
        {
            FileLogger.AI($"TaskGoToEntity: CALLED for ped {pedHandle} -> entity {targetEntityHandle} stopRange={stoppingRange:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskGoToEntity: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // TASK_GO_TO_ENTITY signature: ped, target, duration (-1=indefinite),
                // stoppingRange, speed (3.0=sprint), targetOffset (0=current pos), flags.
                Function.Call(
                    Hash.TASK_GO_TO_ENTITY,
                    ped.Handle,
                    targetEntityHandle,
                    -1,
                    stoppingRange,
                    3.0f,
                    0f,
                    0);

                FileLogger.AI($"TaskGoToEntity: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskGoToEntity exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskFollowToOffsetFromEntity(int pedHandle, int targetEntityHandle, DomainVector3 offset, float moveBlendRatio, float stoppingRadius, bool persistFollowing)
        {
            FileLogger.AI($"TaskFollowToOffsetFromEntity: CALLED for ped {pedHandle} -> entity {targetEntityHandle} radius={stoppingRadius:F1} persist={persistFollowing}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskFollowToOffsetFromEntity: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // TASK_FOLLOW_TO_OFFSET_OF_ENTITY signature:
                // (ped, entity, offsetX, offsetY, offsetZ, moveBlendRatio, timer (-1=indefinite),
                //  stoppingRadius, persistFollowing).
                Function.Call(
                    Hash.TASK_FOLLOW_TO_OFFSET_OF_ENTITY,
                    ped.Handle,
                    targetEntityHandle,
                    offset.X, offset.Y, offset.Z,
                    moveBlendRatio,
                    -1,
                    stoppingRadius,
                    persistFollowing);

                FileLogger.AI($"TaskFollowToOffsetFromEntity: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskFollowToOffsetFromEntity exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedAsFriendly(int pedHandle)
        {
            FileLogger.AI($"SetPedAsFriendly: CALLED for ped {pedHandle}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"SetPedAsFriendly: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn($"SetPedAsFriendly: Player doesn't exist, aborting");
                    return;
                }

                FileLogger.AI($"SetPedAsFriendly: Ped {pedHandle} and player exist, setting up relationship groups");

                // Create a separate FRIENDLY_DEFENDERS group (NOT the player's group)
                // Being in the player's group makes GTA V treat peds as companions with special behavior
                var friendlyDefendersGroup = World.AddRelationshipGroup("FRIENDLY_DEFENDERS");
                var playerGroup = player.RelationshipGroup;
                var defenderEnemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");

                // Set ped to friendly defenders group
                ped.RelationshipGroup = friendlyDefendersGroup;
                FileLogger.AI($"SetPedAsFriendly: Ped {pedHandle} set to FRIENDLY_DEFENDERS group");

                // FRIENDLY_DEFENDERS likes player (won't attack, even when damaged)
                friendlyDefendersGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Companion, true);

                // FRIENDLY_DEFENDERS hates enemy defenders (will attack)
                friendlyDefendersGroup.SetRelationshipBetweenGroups(defenderEnemyGroup, Relationship.Hate, true);
                FileLogger.AI($"SetPedAsFriendly: Relationship groups configured (Companion to player, Hate to enemies)");

                // Configure ped for patrol + engage behavior
                ped.IsPersistent = true;
                ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                ped.BlockPermanentEvents = false; // Allow reaction to events

                // CRITICAL: Prevent ped from becoming hostile when damaged by player
                Function.Call(Hash.SET_PED_CONFIG_FLAG, ped.Handle, 42, true);  // CPED_CONFIG_FLAG_NeverLeavesGroup
                Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped.Handle, 0, false); // Don't flee from anything

                // Enable combat attributes
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);  // CanFightArmedPedsWhenNotArmed
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);   // CanUseCover
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);   // CanDoDrivebys
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 1, false);  // BF_CanBeTargettedByPlayer = false (won't retaliate)

                // Set combat ability and range
                Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2); // Professional
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);   // Far

                // Set alertness high so they notice enemies while wandering
                Function.Call(Hash.SET_PED_ALERTNESS, ped.Handle, 3); // Full alertness

                FileLogger.AI($"SetPedAsFriendly: COMPLETED for ped {pedHandle} - persistent, combat configured, alertness=3");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedAsFriendly exception for ped {pedHandle}", ex);
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

        /// <inheritdoc />
        public bool IsPlayerFreeAiming()
        {
            try
            {
                return Game.Player.IsAiming;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int GetEntityPlayerIsAimingAt()
        {
            try
            {
                var outEntity = new OutputArgument();
                bool aiming = Function.Call<bool>(Hash.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, Game.Player.Handle, outEntity);

                if (aiming)
                {
                    return outEntity.GetResult<int>();
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public void DisplayHelpText(string text)
        {
            try
            {
                // BEGIN_TEXT_COMMAND_DISPLAY_HELP + ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME + END_TEXT_COMMAND_DISPLAY_HELP
                // This shows help text at the bottom of the screen (like "Press E to...")
                Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
                Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, false, true, -1);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public DomainVector3 GetPedPosition(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return DomainVector3.Zero;

                var pos = ped.Position;
                return new DomainVector3(pos.X, pos.Y, pos.Z);
            }
            catch
            {
                return DomainVector3.Zero;
            }
        }

        /// <inheritdoc />
        public void ClearPedTasks(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                ped.Task.ClearAllImmediately();
                FileLogger.AI($"ClearPedTasks: Cleared tasks for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"ClearPedTasks exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskPedTurnToFacePosition(int pedHandle, DomainVector3 position)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                // TASK_TURN_PED_TO_FACE_COORD makes the ped turn to face a position
                // After turning, the ped will stand idle
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_COORD, ped.Handle, position.X, position.Y, position.Z, -1);
                FileLogger.AI($"TaskPedTurnToFacePosition: Ped {pedHandle} turning to face ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedTurnToFacePosition exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedSeeingRange(int pedHandle, float range)
        {
            FileLogger.AI($"SetPedSeeingRange: CALLED for ped {pedHandle} range {range:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"SetPedSeeingRange: Ped {pedHandle} is null or doesn't exist");
                    return;
                }

                // SET_PED_SEEING_RANGE sets how far the ped can visually detect enemies
                // Default is around 70 meters
                Function.Call(Hash.SET_PED_SEEING_RANGE, ped.Handle, range);

                FileLogger.AI($"SetPedSeeingRange: COMPLETED for ped {pedHandle} - set to {range:F1}m");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedSeeingRange exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedHearingRange(int pedHandle, float range)
        {
            FileLogger.AI($"SetPedHearingRange: CALLED for ped {pedHandle} range {range:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"SetPedHearingRange: Ped {pedHandle} is null or doesn't exist");
                    return;
                }

                // SET_PED_HEARING_RANGE sets how far the ped can detect enemies by sound
                // Default is around 60 meters
                Function.Call(Hash.SET_PED_HEARING_RANGE, ped.Handle, range);

                FileLogger.AI($"SetPedHearingRange: COMPLETED for ped {pedHandle} - set to {range:F1}m");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedHearingRange exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public int CreateVehicle(string modelName, DomainVector3 position)
        {
            FileLogger.Info($"CreateVehicle: model={modelName}, pos=({position.X:F1}, {position.Y:F1}, {position.Z:F1})");

            try
            {
                var model = new Model(modelName);

                if (!model.IsValid)
                {
                    FileLogger.Error($"CreateVehicle: Model '{modelName}' is not valid");
                    return -1;
                }

                // Request model with timeout
                model.Request(5000);

                // Wait for model to load
                int waitCounter = 0;
                while (!model.IsLoaded && waitCounter < 100)
                {
                    Script.Wait(10);
                    waitCounter++;
                }

                if (!model.IsLoaded)
                {
                    FileLogger.Error($"CreateVehicle: Model '{modelName}' failed to load");
                    return -1;
                }

                var gtaPosition = new GTA.Math.Vector3(position.X, position.Y, position.Z);

                // Get ground Z for proper placement
                var outArg = new OutputArgument();
                bool gotGround = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
                    position.X, position.Y, position.Z + 100f,
                    outArg, false, false);

                if (gotGround)
                {
                    gtaPosition.Z = outArg.GetResult<float>();
                }

                var vehicle = World.CreateVehicle(model, gtaPosition);
                model.MarkAsNoLongerNeeded();

                if (vehicle == null || !vehicle.Exists())
                {
                    FileLogger.Error("CreateVehicle: World.CreateVehicle returned null or invalid");
                    return -1;
                }

                // Make vehicle persistent
                vehicle.IsPersistent = true;

                FileLogger.Info($"CreateVehicle: Vehicle created successfully, handle={vehicle.Handle}");
                return vehicle.Handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error("CreateVehicle exception", ex);
                return -1;
            }
        }

        /// <inheritdoc />
        public int CreateBlipForVehicle(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                    return -1;

                var blip = vehicle.AddBlip();
                if (blip == null || !blip.Exists())
                    return -1;

                FileLogger.Info($"CreateBlipForVehicle: Blip created for vehicle {vehicleHandle}, blip handle={blip.Handle}");
                return blip.Handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"CreateBlipForVehicle exception for vehicle {vehicleHandle}", ex);
                return -1;
            }
        }

        /// <inheritdoc />
        public DomainVector3 GetNearestRoadPosition(DomainVector3 position)
        {
            FileLogger.Info($"GetNearestRoadPosition: Searching near ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");

            try
            {
                // Use GET_CLOSEST_VEHICLE_NODE_WITH_HEADING to find nearest road
                var outNodeCoord = new OutputArgument();
                var outHeading = new OutputArgument();
                bool found = Function.Call<bool>(
                    Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,
                    position.X, position.Y, position.Z,
                    outNodeCoord,
                    outHeading,
                    1,      // nodeType - any road
                    3.0f,   // p7 - search radius multiplier
                    0);     // p8

                if (found)
                {
                    var result = outNodeCoord.GetResult<GTA.Math.Vector3>();
                    FileLogger.Info($"GetNearestRoadPosition: Found road at ({result.X:F1}, {result.Y:F1}, {result.Z:F1})");
                    return new DomainVector3(result.X, result.Y, result.Z);
                }

                FileLogger.Warn($"GetNearestRoadPosition: No road found, using ground Z fallback");
                var groundZ = GetGroundZ(position.X, position.Y, position.Z);
                return new DomainVector3(position.X, position.Y, groundZ);
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetNearestRoadPosition exception", ex);
                return position;
            }
        }

        /// <inheritdoc />
        public string GetVehicleModelName(int vehicleHandle)
        {
            try
            {
                var vehicle = Entity.FromHandle(vehicleHandle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                {
                    return string.Empty;
                }

                // Get the model and convert to lowercase name
                var model = vehicle.Model;
                var modelName = model.ToString().ToLowerInvariant();

                FileLogger.AI($"GetVehicleModelName: Vehicle {vehicleHandle} model = {modelName}");
                return modelName;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GetVehicleModelName exception for vehicle {vehicleHandle}", ex);
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public System.Collections.Generic.Dictionary<string, int> GetPlayerWeapons()
        {
            var weapons = new System.Collections.Generic.Dictionary<string, int>();

            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn("GetPlayerWeapons: Player doesn't exist");
                    return weapons;
                }

                // Common weapons to check - GTA V has many weapons, we check the most common ones
                var weaponHashes = new[]
                {
                    // Melee
                    WeaponHash.Knife, WeaponHash.Nightstick, WeaponHash.Hammer, WeaponHash.Bat,
                    WeaponHash.GolfClub, WeaponHash.Crowbar, WeaponHash.Bottle, WeaponHash.SwitchBlade,
                    WeaponHash.Dagger, WeaponHash.Hatchet, WeaponHash.Machete, WeaponHash.Flashlight,
                    WeaponHash.KnuckleDuster, WeaponHash.PoolCue, WeaponHash.Wrench, WeaponHash.BattleAxe,
                    // Handguns
                    WeaponHash.Pistol, WeaponHash.CombatPistol, WeaponHash.APPistol, WeaponHash.Pistol50,
                    WeaponHash.SNSPistol, WeaponHash.HeavyPistol, WeaponHash.VintagePistol, WeaponHash.MarksmanPistol,
                    WeaponHash.Revolver, WeaponHash.DoubleActionRevolver, WeaponHash.FlareGun, WeaponHash.StunGun,
                    WeaponHash.CeramicPistol, WeaponHash.NavyRevolver, WeaponHash.PericoPistol,
                    // SMG
                    WeaponHash.MicroSMG, WeaponHash.SMG, WeaponHash.AssaultSMG, WeaponHash.CombatPDW,
                    WeaponHash.MachinePistol, WeaponHash.MiniSMG, WeaponHash.Gusenberg,
                    // Shotguns
                    WeaponHash.PumpShotgun, WeaponHash.SawnOffShotgun, WeaponHash.AssaultShotgun,
                    WeaponHash.BullpupShotgun, WeaponHash.HeavyShotgun, WeaponHash.DoubleBarrelShotgun,
                    WeaponHash.SweeperShotgun, WeaponHash.CombatShotgun,
                    // Assault Rifles
                    WeaponHash.AssaultRifle, WeaponHash.CarbineRifle, WeaponHash.AdvancedRifle,
                    WeaponHash.SpecialCarbine, WeaponHash.BullpupRifle, WeaponHash.CompactRifle,
                    WeaponHash.MilitaryRifle, WeaponHash.HeavyRifle,
                    // MG
                    WeaponHash.MG, WeaponHash.CombatMG, WeaponHash.Gusenberg,
                    // Sniper
                    WeaponHash.SniperRifle, WeaponHash.HeavySniper, WeaponHash.MarksmanRifle,
                    // Heavy
                    WeaponHash.RPG, WeaponHash.GrenadeLauncher, WeaponHash.Minigun, WeaponHash.Firework,
                    WeaponHash.Railgun, WeaponHash.HomingLauncher, WeaponHash.CompactGrenadeLauncher,
                    // Throwables
                    WeaponHash.Grenade, WeaponHash.SmokeGrenade, WeaponHash.BZGas, WeaponHash.Molotov,
                    WeaponHash.StickyBomb, WeaponHash.ProximityMine, WeaponHash.Snowball, WeaponHash.PipeBomb,
                    WeaponHash.Ball, WeaponHash.Flare
                };

                foreach (var weaponHash in weaponHashes)
                {
                    if (player.Weapons.HasWeapon(weaponHash))
                    {
                        var weapon = player.Weapons[weaponHash];
                        if (weapon != null)
                        {
                            var ammo = weapon.Ammo;
                            var weaponName = weaponHash.ToString().ToLowerInvariant();
                            // Convert enum name to weapon hash format (e.g., "pistol" -> "weapon_pistol")
                            weapons[$"weapon_{weaponName}"] = ammo;
                        }
                    }
                }

                FileLogger.Info($"GetPlayerWeapons: Found {weapons.Count} weapons");
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetPlayerWeapons exception", ex);
            }

            return weapons;
        }

        /// <inheritdoc />
        public bool IsControlPressed(int control)
        {
            return Game.IsControlPressed((GTA.Control)control);
        }

        /// <inheritdoc />
        public bool IsControlJustPressed(int control)
        {
            return Game.IsControlJustPressed((GTA.Control)control);
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
