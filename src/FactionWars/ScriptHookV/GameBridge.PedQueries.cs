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
        public string GetScriptsDirectory()
        {
            // ScriptHookVDotNet runs with GTA V installation folder as working directory
            // The scripts folder is a subdirectory of that
            return Path.Combine(Environment.CurrentDirectory, "scripts");
        }

        /// <inheritdoc />
        public bool IsPlayerFreeAiming()
        {
            try
            {
                return Game.Player.IsAiming;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public int GetEntityPlayerIsAimingAt()
        {
            try
            {
                var outEntity = new OutputArgument();
                bool aiming = Function.Call<bool>(Hash.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT, Game.Player.Handle, outEntity);

                if (aiming)
                {
                    return outEntity.GetResult<int>();
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public void DisplayHelpText(string text)
        {
            try
            {
                // BEGIN_TEXT_COMMAND_DISPLAY_HELP + ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME + END_TEXT_COMMAND_DISPLAY_HELP
                // This shows help text at the bottom of the screen (like "Press E to...")
                Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
                Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, false, true, -1);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public DomainVector3 GetPedPosition(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return DomainVector3.Zero;

                var pos = ped.Position;
                return new DomainVector3(pos.X, pos.Y, pos.Z);
            }
            catch
            {
                return DomainVector3.Zero;
            }
        }

        /// <inheritdoc />
        public void ClearPedTasks(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                ped.Task.ClearAllImmediately();
                FileLogger.AI($"ClearPedTasks: Cleared tasks for ped {pedHandle}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"ClearPedTasks exception for ped {pedHandle}", ex);
            }
        }

        public void SetPlayerPosition(DomainVector3 position)
        {
            SetPedPosition(GetPlayerPedHandle(), position);
        }

        /// <inheritdoc />
        public void TaskPedTurnToFacePosition(int pedHandle, DomainVector3 position)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                    return;

                // TASK_TURN_PED_TO_FACE_COORD makes the ped turn to face a position
                // After turning, the ped will stand idle
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_COORD, ped.Handle, position.X, position.Y, position.Z, -1);
                FileLogger.AI($"TaskPedTurnToFacePosition: Ped {pedHandle} turning to face ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"TaskPedTurnToFacePosition exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedSeeingRange(int pedHandle, float range)
        {
            FileLogger.AI($"SetPedSeeingRange: CALLED for ped {pedHandle} range {range:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"SetPedSeeingRange: Ped {pedHandle} is null or doesn't exist");
                    return;
                }

                // SET_PED_SEEING_RANGE sets how far the ped can visually detect enemies
                // Default is around 70 meters
                Function.Call(Hash.SET_PED_SEEING_RANGE, ped.Handle, range);

                FileLogger.AI($"SetPedSeeingRange: COMPLETED for ped {pedHandle} - set to {range:F1}m");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedSeeingRange exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedHearingRange(int pedHandle, float range)
        {
            FileLogger.AI($"SetPedHearingRange: CALLED for ped {pedHandle} range {range:F1}");

            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists())
                {
                    FileLogger.Warn($"SetPedHearingRange: Ped {pedHandle} is null or doesn't exist");
                    return;
                }

                // SET_PED_HEARING_RANGE sets how far the ped can detect enemies by sound
                // Default is around 60 meters
                Function.Call(Hash.SET_PED_HEARING_RANGE, ped.Handle, range);

                FileLogger.AI($"SetPedHearingRange: COMPLETED for ped {pedHandle} - set to {range:F1}m");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedHearingRange exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
    }
}
