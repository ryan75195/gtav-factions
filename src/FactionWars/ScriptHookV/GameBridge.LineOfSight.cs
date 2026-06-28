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
                FileLogger.Debug($"HasClearLineOfSight: checking {fromPedHandle}->{toPedHandle}");

                var from = Entity.FromHandle(fromPedHandle) as Ped;
                var to = Entity.FromHandle(toPedHandle) as Ped;
                if (from == null || !from.Exists() || from.IsDead ||
                    to == null || !to.Exists() || to.IsDead)
                {
                    FileLogger.Debug($"HasClearLineOfSight: {fromPedHandle}->{toPedHandle} = false (invalid/dead handle)");
                    return false;
                }

                // HAS_ENTITY_CLEAR_LOS_TO_ENTITY: trace flag 17 = world geometry + vehicles + objects.
                var result = Function.Call<bool>(
                    Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, from.Handle, to.Handle, 17);
                FileLogger.Debug($"HasClearLineOfSight: {fromPedHandle}->{toPedHandle} = {result}");
                return result;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"HasClearLineOfSight exception for {fromPedHandle}->{toPedHandle}", ex);
                return false;
            }
        }
    }
}
