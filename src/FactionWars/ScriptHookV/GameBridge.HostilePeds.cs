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

                // Preserve the faction relationship group assigned by PedSpawningService
                // so AI factions can hate each other during 3-way battles.
                var enemyGroup = ped.RelationshipGroup;
                var playerGroup = player.RelationshipGroup;

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
