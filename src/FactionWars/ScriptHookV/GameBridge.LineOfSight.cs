using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        /// <inheritdoc />
        public bool HasClearLineOfSight(int fromPedHandle, int toPedHandle)
        {
            try
            {
                var from = Entity.FromHandle(fromPedHandle) as Ped;
                var to = Entity.FromHandle(toPedHandle) as Ped;
                if (from == null || !from.Exists() || to == null || !to.Exists())
                {
                    return false;
                }

                // HAS_ENTITY_CLEAR_LOS_TO_ENTITY: trace flag 17 = world geometry + vehicles + objects.
                return Function.Call<bool>(
                    Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, from.Handle, to.Handle, 17);
            }
            catch (Exception ex)
            {
                FileLogger.Error($"HasClearLineOfSight exception for {fromPedHandle}->{toPedHandle}", ex);
                return false;
            }
        }
    }
}
