using System;
using GTA;
using GTA.Native;
using FactionWars.ScriptHookV.Logging;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void TaskGuardArea(int pedHandle, DomainVector3 center, float radius)
        {
            FileLogger.AI($"TaskGuardArea: CALLED for ped {pedHandle} center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskGuardArea: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // Defend a sphere centred on the hold point. The defensive area keeps the ped
                // anchored near the point; the guard task makes it hold and engage from cover.
                // NOTE: confirm the TASK_GUARD_SPHERE_DEFENSIVE_AREA parameter order against the
                // installed SHVDN Hash enum via in-game logs and adjust if peds wander off.
                Function.Call(Hash.SET_PED_SPHERE_DEFENSIVE_AREA, ped.Handle, center.X, center.Y, center.Z, radius, false, 0);
                Function.Call(
                    Hash.TASK_GUARD_SPHERE_DEFENSIVE_AREA,
                    ped.Handle,
                    center.X, center.Y, center.Z,
                    0.0f,
                    radius,
                    -1,
                    center.X, center.Y, center.Z,
                    radius);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true); // BF_CanUseCover

                FileLogger.AI($"TaskGuardArea: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskGuardArea exception for ped {pedHandle}", ex);
            }
        }

        public void TaskCombatPed(int pedHandle, int targetPedHandle)
        {
            FileLogger.AI($"TaskCombatPed: CALLED for ped {pedHandle} -> target {targetPedHandle}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                var target = Entity.FromHandle(targetPedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskCombatPed: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }
                if (target == null || !target.Exists())
                {
                    FileLogger.Warn($"TaskCombatPed: Target {targetPedHandle} is null or doesn't exist, aborting");
                    return;
                }

                Function.Call(Hash.TASK_COMBAT_PED, ped.Handle, target.Handle, 0, 16);
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 3.0f);

                FileLogger.AI($"TaskCombatPed: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskCombatPed exception for ped {pedHandle}", ex);
            }
        }
    }
}
