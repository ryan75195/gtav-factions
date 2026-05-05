using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public float GetGroundZ(float x, float y, float z)
        {
            try
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, x, y, z);

                foreach (float probeZ in GetGroundSearchHeights(z))
                {
                    var outArg = new OutputArgument();
                    bool found = Function.Call<bool>(
                        Hash.GET_GROUND_Z_FOR_3D_COORD,
                        x, y, probeZ,
                        outArg,
                        false,  // ignoreWater
                        false); // ignoreDistToWaterLevelCheck

                    if (found)
                    {
                        return outArg.GetResult<float>();
                    }
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

        private static float[] GetGroundSearchHeights(float z)
        {
            return new[]
            {
                z + 100f,
                z + 300f,
                1000f,
                700f,
                500f,
                300f,
                150f,
                75f,
                30f
            };
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
    }
}
