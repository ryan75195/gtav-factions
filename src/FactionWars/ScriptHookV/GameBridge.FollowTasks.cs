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

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Warn($"SetPedAsFriendly: Player doesn't exist, aborting");
                    return;
                }

                FileLogger.AI($"SetPedAsFriendly: Ped {pedHandle} and player exist, setting up relationship groups");

                // Create a separate FRIENDLY_DEFENDERS group (NOT the player's group)
                // Being in the player's group makes GTA V treat peds as companions with special behavior
                var friendlyDefendersGroup = World.AddRelationshipGroup("FRIENDLY_DEFENDERS");
                var playerGroup = player.RelationshipGroup;
                var defenderEnemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");

                // Set ped to friendly defenders group
                ped.RelationshipGroup = friendlyDefendersGroup;
                FileLogger.AI($"SetPedAsFriendly: Ped {pedHandle} set to FRIENDLY_DEFENDERS group");

                // FRIENDLY_DEFENDERS likes player (won't attack, even when damaged)
                friendlyDefendersGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Companion, true);

                // FRIENDLY_DEFENDERS hates enemy defenders (will attack)
                friendlyDefendersGroup.SetRelationshipBetweenGroups(defenderEnemyGroup, Relationship.Hate, true);
                FileLogger.AI($"SetPedAsFriendly: Relationship groups configured (Companion to player, Hate to enemies)");

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
