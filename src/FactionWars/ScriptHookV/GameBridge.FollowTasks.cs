using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
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

                // Relationship group and allegiance are owned by the relationship matrix, wired
                // once at init (RelationshipMatrixInitializer). A friendly combatant keeps the
                // player's faction group it was spawned in; here we only apply combat config so it
                // fights, holds task, and never flees.
                ConfigureFriendlyDefenderPed(ped);

                FileLogger.AI($"SetPedAsFriendly: COMPLETED for ped {pedHandle} - persistent, combat configured, alertness=3");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedAsFriendly exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
    }
}
