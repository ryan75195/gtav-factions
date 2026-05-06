using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public bool CanControlCharacter()
        {
            try
            {
                var player = Game.Player;
                return player != null && player.CanControlCharacter;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool IsGamePaused()
        {
            try
            {
                return Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE);
            }
            catch (Exception ex)
            {
                FileLogger.Error("IsGamePaused exception", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public int GetWantedLevel()
        {
            try
            {
                var level = Game.Player.Wanted.WantedLevel;
                return level;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetWantedLevel exception", ex);
                return 0;
            }
        }

        /// <inheritdoc />
        public void ClearWantedLevel()
        {
            try
            {
                Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player.Handle);
            }
            catch (Exception ex)
            {
                FileLogger.Error("ClearWantedLevel exception", ex);
            }
        }

        /// <inheritdoc />
        public void SetPoliceSuppressionEnabled(bool enabled)
        {
            try
            {
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player.Handle, enabled);
                Function.Call(Hash.SET_DISPATCH_COPS_FOR_PLAYER, Game.Player.Handle, !enabled);
                if (enabled)
                {
                    ClearWantedLevel();
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPoliceSuppressionEnabled exception", ex);
            }
        }

        /// <inheritdoc />
        public bool ConsumePlayerDamagedByPedFlag()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                // HAS_ENTITY_BEEN_DAMAGED_BY_ANY_PED reads the engine-set "damaged by ped"
                // flag. Second argument (updateHasBeenDamagedThisFrame=true) marks the frame
                // as processed so subsequent same-tick reads don't see stale state, matching
                // the conventional one-shot consume behaviour. We pair it with
                // CLEAR_ENTITY_LAST_DAMAGE_ENTITY so the next call only returns true if NEW
                // damage has occurred.
                var damaged = Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ANY_PED, player.Handle, 1);
                if (damaged)
                {
                    FileLogger.Combat("ConsumePlayerDamagedByPedFlag: player damaged by ped, clearing flag");
                    Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, player.Handle);
                }
                return damaged;
            }
            catch (Exception ex)
            {
                FileLogger.Error("ConsumePlayerDamagedByPedFlag exception", ex);
                return false;
            }
        }

        /// <inheritdoc />
    }
}
