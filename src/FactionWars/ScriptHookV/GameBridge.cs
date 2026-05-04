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
    }
}
