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
        private static bool TryAdjustPedSpawnHeight(
            DomainVector3 position,
            ref GTA.Math.Vector3 gtaPosition,
            out float groundZ)
        {
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, position.X, position.Y, position.Z);

            int collisionWait = 0;
            while (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, Game.Player.Character.Handle) && collisionWait < 10)
            {
                Script.Wait(10);
                collisionWait++;
            }

            var outArg = new OutputArgument();
            bool gotGround = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
                position.X, position.Y, position.Z + 100f, outArg, false, false);
            groundZ = gotGround ? outArg.GetResult<float>() : 0f;

            if (gotGround && groundZ > 0)
            {
                gtaPosition.Z = groundZ;
                return true;
            }

            FileLogger.Warn($"Could not get ground height, keeping original Z={gtaPosition.Z:F1}");
            return false;
        }

        private bool TryLoadPedModel(string modelName, out Model model)
        {
            model = new Model(modelName);
            FileLogger.Spawn($"Model created: IsValid={model.IsValid}, Hash={model.Hash}");

            if (!model.IsValid)
            {
                FileLogger.Error($"Model '{modelName}' is not valid!");
                ShowNotification($"~r~Invalid model: {modelName}");
                return false;
            }

            model.Request(5000);
            int waitCounter = 0;
            while (!model.IsLoaded && waitCounter < 100)
            {
                Script.Wait(10);
                waitCounter++;
            }

            if (model.IsLoaded)
                return true;

            FileLogger.Error($"Model '{modelName}' failed to load after {waitCounter * 10}ms");
            ShowNotification($"~r~Model failed to load: {modelName}");
            return false;
        }

        private static bool TryLoadModel(string modelName, string logPrefix, out Model model)
        {
            model = new Model(modelName);
            if (!model.IsValid)
            {
                FileLogger.Error($"{logPrefix}: Model '{modelName}' is not valid");
                return false;
            }

            model.Request(5000);
            int waitCounter = 0;
            while (!model.IsLoaded && waitCounter < 100)
            {
                Script.Wait(10);
                waitCounter++;
            }

            if (model.IsLoaded)
                return true;

            FileLogger.Error($"{logPrefix}: Model '{modelName}' failed to load");
            return false;
        }

        private static float GetVehicleSpawnZ(DomainVector3 position, float fallbackZ)
        {
            var outArg = new OutputArgument();
            bool gotGround = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
                position.X, position.Y, position.Z + 100f,
                outArg, false, false);
            return gotGround ? outArg.GetResult<float>() : fallbackZ;
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
    }
}
