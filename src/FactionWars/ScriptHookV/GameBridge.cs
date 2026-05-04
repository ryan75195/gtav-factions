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
    public partial class GameBridge : IGameBridge
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
                if (!TryLoadPedModel(modelName, out var model))
                    return -1;

                // Get ground Z coordinate for proper placement
                var gtaPosition = new GTA.Math.Vector3(position.X, position.Y, position.Z);
                FileLogger.Spawn($"Initial position: ({gtaPosition.X:F1}, {gtaPosition.Y:F1}, {gtaPosition.Z:F1})");

                bool gotGround = TryAdjustPedSpawnHeight(position, ref gtaPosition, out var groundZ);

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

                if (ped == null || !ValidateCreatedPed(ped))
                    return -1;

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
                ShowNotification($"~r~CreatePed error: {ex.Message}");
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
        public bool DoesPedExist(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                return ped != null && ped.Exists();
            }
            catch (Exception ex)
            {
                FileLogger.Error($"DoesPedExist exception for ped {pedHandle}", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public int GetPedKiller(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return 0;

                var killer = ped.Killer;
                if (killer == null || !killer.Exists())
                {
                    FileLogger.AI($"GetPedKiller: ped {pedHandle} has no resolvable killer (environmental death or unknown)");
                    return 0;
                }

                FileLogger.AI($"GetPedKiller: ped {pedHandle} was killed by ped {killer.Handle}");
                return killer.Handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GetPedKiller exception for ped {pedHandle}", ex);
                return 0;
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
                GTA.UI.Notification.PostTicker(message, false, false);
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
        public bool CanControlCharacter()
        {
            try
            {
                var player = Game.Player;
                return player != null && player.CanControlCharacter;
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
                var level = Game.Player.Wanted.WantedLevel;
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
                var weaponHash = GetWeaponHash(weaponName);

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
    }
}
