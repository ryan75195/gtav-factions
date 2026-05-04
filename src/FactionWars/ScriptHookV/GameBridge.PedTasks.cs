using System;
using System.IO;
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
        public void SetPedToAttackPlayer(int pedHandle)
        {
            try
            {
                FileLogger.Combat($"SetPedToAttackPlayer called for handle {pedHandle}");

                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Error($"SetPedToAttackPlayer: Ped {pedHandle} doesn't exist");
                    return;
                }

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    FileLogger.Error("SetPedToAttackPlayer: Player doesn't exist");
                    return;
                }

                // Create or get an enemy relationship group for defenders
                var enemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
                var playerGroup = player.RelationshipGroup;

                // Set ped to enemy group
                ped.RelationshipGroup = enemyGroup;

                // Make the groups hate each other
                enemyGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Hate, true);

                // Configure ped for aggressive combat
                ped.IsPersistent = true;
                ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                ped.BlockPermanentEvents = false; // Allow them to react

                // Set combat attributes for aggressive behavior
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // BF_CanFightArmedPedsWhenNotArmed
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);  // BF_CanUseCover
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 0, false); // BF_CanUseCoverShootOnlyWhenAimingAtTarget
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // BF_CanFightArmedPedsWhenNotArmed

                // Set combat ability and range
                Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2); // Professional
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);   // Far
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped.Handle, 2); // Offensive

                // Set firing pattern for aggression
                ped.FiringPattern = FiringPattern.FullAuto;

                // CRITICAL: Give the ped a task to fight the player
                ped.Task.ClearAllImmediately();
                ped.Task.Combat(player);

                FileLogger.Combat($"Defender {pedHandle} set to attack player with Combat task");
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPedToAttackPlayer exception", ex);
            }
        }

        /// <inheritdoc />
        public int CreateBlipForPed(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return -1;

                var blip = ped.AddBlip();
                return blip?.Handle ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <inheritdoc />
        public void TaskPedWanderInArea(int pedHandle, DomainVector3 center, float radius)
        {
            FileLogger.AI($"TaskPedWanderInArea: CALLED for ped {pedHandle} at center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskPedWanderInArea: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                FileLogger.AI($"TaskPedWanderInArea: Ped {pedHandle} exists, position=({ped.Position.X:F1}, {ped.Position.Y:F1}, {ped.Position.Z:F1})");

                // Clear any existing tasks first
                ped.Task.ClearAllImmediately();
                FileLogger.AI($"TaskPedWanderInArea: Cleared existing tasks for ped {pedHandle}");

                // NOTE: Removed SET_BLOCKING_OF_NON_TEMPORARY_EVENTS - it may prevent wandering
                // Instead, let the ped be able to respond to events while wandering

                // Use TASK_WANDER_STANDARD instead - simpler and more reliable
                // Parameters: ped, heading (10.0 = random direction), flags (0 = default)
                Function.Call(Hash.TASK_WANDER_STANDARD, ped.Handle, 10.0f, 0);
                FileLogger.AI($"TaskPedWanderInArea: TASK_WANDER_STANDARD called for ped {pedHandle}");

                // Set movement blend ratio for walking (1.0 = walk)
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 1.0f);
                FileLogger.AI($"TaskPedWanderInArea: SET_PED_DESIRED_MOVE_BLEND_RATIO=1.0 (walk) for ped {pedHandle}");

                // Check if task is active (task ID 222 = wander)
                bool isWandering = Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped.Handle, 222);
                FileLogger.AI($"TaskPedWanderInArea: Task active check (222) = {isWandering} for ped {pedHandle}");

                FileLogger.AI($"TaskPedWanderInArea: COMPLETED successfully for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedWanderInArea exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskPedWanderInBoundedArea(int pedHandle, DomainVector3 center, float radius)
        {
            FileLogger.AI($"TaskPedWanderInBoundedArea: CALLED for ped {pedHandle} at center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskPedWanderInBoundedArea: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                ped.Task.ClearAllImmediately();

                // TASK_WANDER_IN_AREA params: ped, x, y, z, radius, minLength, timeBetweenWalks
                Function.Call(
                    Hash.TASK_WANDER_IN_AREA,
                    ped.Handle,
                    center.X, center.Y, center.Z,
                    radius,
                    10.0f, 10.0f);
                FileLogger.AI($"TaskPedWanderInBoundedArea: TASK_WANDER_IN_AREA called for ped {pedHandle}");

                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 1.0f);
                FileLogger.AI($"TaskPedWanderInBoundedArea: COMPLETED successfully for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedWanderInBoundedArea exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskPedWanderInAreaSprinting(int pedHandle, DomainVector3 center, float radius)
        {
            FileLogger.AI($"TaskPedWanderInAreaSprinting: CALLED for ped {pedHandle} at center ({center.X:F1}, {center.Y:F1}, {center.Z:F1}) radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskPedWanderInAreaSprinting: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                FileLogger.AI($"TaskPedWanderInAreaSprinting: Ped {pedHandle} exists, position=({ped.Position.X:F1}, {ped.Position.Y:F1}, {ped.Position.Z:F1})");

                // Clear any existing tasks first
                ped.Task.ClearAllImmediately();
                FileLogger.AI($"TaskPedWanderInAreaSprinting: Cleared existing tasks for ped {pedHandle}");

                // NOTE: Removed SET_BLOCKING_OF_NON_TEMPORARY_EVENTS - it prevents wandering from working
                // Use TASK_WANDER_STANDARD - simpler and more reliable
                Function.Call(Hash.TASK_WANDER_STANDARD, ped.Handle, 10.0f, 0);
                FileLogger.AI($"TaskPedWanderInAreaSprinting: TASK_WANDER_STANDARD called for ped {pedHandle}");

                // Set movement blend ratio for sprinting (3.0 = sprint)
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 3.0f);
                FileLogger.AI($"TaskPedWanderInAreaSprinting: SET_PED_DESIRED_MOVE_BLEND_RATIO=3.0 (sprint) for ped {pedHandle}");

                FileLogger.AI($"TaskPedWanderInAreaSprinting: COMPLETED successfully for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedWanderInAreaSprinting exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void TaskCombatHatedTargetsAroundPed(int pedHandle, float radius)
        {
            FileLogger.AI($"TaskCombatHatedTargetsAroundPed: CALLED for ped {pedHandle} radius {radius:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"TaskCombatHatedTargetsAroundPed: Ped {pedHandle} is null or doesn't exist, aborting");
                    return;
                }

                // Task the ped to actively seek out and fight any hated targets within range
                // This makes them run towards enemies and engage in combat
                Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped.Handle, radius, 0);

                // Set movement blend ratio for sprinting (3.0 = sprint) so they run towards enemies
                Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, ped.Handle, 3.0f);

                FileLogger.AI($"TaskCombatHatedTargetsAroundPed: COMPLETED for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskCombatHatedTargetsAroundPed exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
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
        public float GetGroundZ(float x, float y, float z)
        {
            try
            {
                var outArg = new OutputArgument();
                bool found = Function.Call<bool>(
                    Hash.GET_GROUND_Z_FOR_3D_COORD,
                    x, y, z + 100f,  // Start search from above
                    outArg,
                    false,  // ignoreWater
                    false); // ignoreDistToWaterLevelCheck

                if (found)
                {
                    return outArg.GetResult<float>();
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
        public void SetPedAsHostileWanderer(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // Create or get enemy relationship group
                var enemyGroup = World.AddRelationshipGroup("DEFENDER_ENEMIES");
                var playerGroup = player.RelationshipGroup;

                // Set ped to enemy group
                ped.RelationshipGroup = enemyGroup;

                // Make the groups hate each other (bidirectional)
                enemyGroup.SetRelationshipBetweenGroups(playerGroup, Relationship.Hate, true);

                // Configure ped for patrol + engage behavior
                ped.IsPersistent = true;
                ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
                ped.BlockPermanentEvents = false; // Allow reaction to enemies

                // Set combat attributes
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true); // CanFightArmedPedsWhenNotArmed
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);  // CanUseCover
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);  // CanDoDrivebys

                // Set combat ability and range
                Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2); // Professional
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);   // Far

                // Set alertness high so they notice enemies while wandering
                Function.Call(Hash.SET_PED_ALERTNESS, ped.Handle, 3); // Full alertness

                // DO NOT give Combat task - let them wander and engage via relationship system
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPedAsHostileWanderer exception", ex);
            }
        }

        /// <inheritdoc />
    }
}
