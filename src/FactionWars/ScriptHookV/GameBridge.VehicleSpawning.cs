using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public int CreateVehicle(string modelName, DomainVector3 position)
        {
            FileLogger.Info($"CreateVehicle: model={modelName}, pos=({position.X:F1}, {position.Y:F1}, {position.Z:F1})");

            try
            {
                if (!TryLoadModel(modelName, "CreateVehicle", out var model))
                    return -1;

                var gtaPosition = new GTA.Math.Vector3(position.X, position.Y, position.Z);
                gtaPosition.Z = GetVehicleSpawnZ(position, gtaPosition.Z);

                var vehicle = World.CreateVehicle(model, gtaPosition);
                model.MarkAsNoLongerNeeded();

                if (vehicle == null || !vehicle.Exists())
                {
                    FileLogger.Error("CreateVehicle: World.CreateVehicle returned null or invalid");
                    return -1;
                }

                // Make vehicle persistent
                vehicle.IsPersistent = true;

                FileLogger.Info($"CreateVehicle: Vehicle created successfully, handle={vehicle.Handle}");
                return vehicle.Handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error("CreateVehicle exception", ex);
                return -1;
            }
        }

        private static void AddPedToPlayerGroup(Ped player, Ped ped, int pedHandle)
        {
            var pedGroup = player.PedGroup;
            if (pedGroup != null)
            {
                pedGroup.Add(ped, false);
                FileLogger.Info($"Added ped {pedHandle} to player's ped group");
                return;
            }

            pedGroup = new PedGroup();
            pedGroup.Add(player, true);
            pedGroup.Add(ped, false);
            FileLogger.Info($"Created new ped group with player as leader");
        }

        private bool ValidateCreatedPed(Ped? ped)
        {
            if (ped == null)
            {
                FileLogger.Error("World.CreatePed returned null!");
                ShowNotification("~r~Ped creation failed (null)!");
                return false;
            }

            if (ped.Exists())
                return true;

            FileLogger.Error("Ped created but doesn't exist!");
            ShowNotification("~r~Ped creation failed (not exists)!");
            return false;
        }

        private static void ConfigureFriendlyDefenderPed(Ped ped)
        {
            ped.IsPersistent = true;
            ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
            ped.BlockPermanentEvents = false;
            Function.Call(Hash.SET_PED_CONFIG_FLAG, ped.Handle, 42, true);
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped.Handle, 0, false);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 1, false);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2);
            Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);
            Function.Call(Hash.SET_PED_ALERTNESS, ped.Handle, 3);
        }

        private static void ConfigureFollowerCombat(Ped ped)
        {
            ped.IsPersistent = true;
            ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;
            ped.BlockPermanentEvents = false;
            ped.CanSwitchWeapons = true;
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 0, false);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 2, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 3, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 20, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 1, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 52, true);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped.Handle, 2);
            Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);
            Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped.Handle, 2);
            ped.FiringPattern = FiringPattern.FullAuto;
            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, ped.Handle, Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player.Handle));
            Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped.Handle, 100f, 0);
        }

    }
}
