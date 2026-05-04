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
        public int GetPlayerPedHandle()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return -1;
                return player.Handle;
            }
            catch (Exception ex)
            {
                FileLogger.Error("GetPlayerPedHandle exception", ex);
                return -1;
            }
        }

        /// <inheritdoc />
    }
}
