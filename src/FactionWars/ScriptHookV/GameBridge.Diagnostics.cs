using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    // TEMPORARY diagnostic instrumentation for the bug:
    // "friendly defenders turn hostile after the player dies and the defended battle is won".
    // These are read-only probes consumed only by logging. Delete this whole file once the
    // root cause is confirmed.
    public partial class GameBridge
    {
        /// <inheritdoc />
        public int GetGroupRelationship(string groupName1, string groupName2)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupName1) || string.IsNullOrWhiteSpace(groupName2))
                    return -1;

                var g1 = World.AddRelationshipGroup(groupName1.ToUpperInvariant());
                var g2 = World.AddRelationshipGroup(groupName2.ToUpperInvariant());
                return Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_GROUPS, g1.Hash, g2.Hash);
            }
            catch (Exception ex)
            {
                FileLogger.Error($"GetRelationshipBetweenGroups failed for {groupName1}->{groupName2}", ex);
                return -1;
            }
        }

        /// <inheritdoc />
        public bool IsPedInCombatWithPlayer(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return false;

                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                return Function.Call<bool>(Hash.IS_PED_IN_COMBAT, ped.Handle, player.Handle);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int GetPedRelationshipGroupHash(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return 0;

                return ped.RelationshipGroup.Hash;
            }
            catch
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public int GetPlayerRelationshipGroupHash()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return 0;

                return player.RelationshipGroup.Hash;
            }
            catch
            {
                return 0;
            }
        }
    }
}
