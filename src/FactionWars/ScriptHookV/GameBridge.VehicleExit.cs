using System;
using FactionWars.ScriptHookV.Logging;
using GTA;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
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
    }
}
