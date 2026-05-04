using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
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
        public void TaskGoToCoord(int pedHandle, DomainVector3 destination)
        {
            FileLogger.AI($"TaskGoToCoord: CALLED for ped {pedHandle} to ({destination.X:F1}, {destination.Y:F1}, {destination.Z:F1})");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskGoToCoord: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // TASK_GO_TO_COORD_ANY_MEANS:
                //   ped, x, y, z, moveSpeed, vehicle, useLongRangePath, drivingFlags, finalHeading
                Function.Call(
                    Hash.TASK_GO_TO_COORD_ANY_MEANS,
                    ped.Handle,
                    destination.X, destination.Y, destination.Z,
                    2.0f, 0, false, 786603, 0.0f);
                FileLogger.AI($"TaskGoToCoord: TASK_GO_TO_COORD_ANY_MEANS issued for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskGoToCoord exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
    }
}
