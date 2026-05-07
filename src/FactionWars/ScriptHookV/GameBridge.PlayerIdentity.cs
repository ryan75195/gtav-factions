using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public string GetPlayerCharacterModel()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return string.Empty;

                // Get the model hash and convert to model name
                var model = player.Model;

                // GTA V protagonist model hashes - we compare against known protagonist hashes
                // Michael: 225514697 (player_zero)
                // Franklin: 2602752943 (player_one)
                // Trevor: 2608926626 (player_two)
                var modelHash = model.Hash;

                // Check against known protagonist models
                if (model == new Model("player_zero")) return "player_zero";
                if (model == new Model("player_one")) return "player_one";
                if (model == new Model("player_two")) return "player_two";

                // For other ped models, return the hash as a string for debugging
                return modelHash.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public float GetPlayerHeading()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return 0f;

                return player.Heading;
            }
            catch
            {
                return 0f;
            }
        }

        public void SetPlayerHeading(float heading)
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                player.Heading = heading;
            }
            catch (Exception ex)
            {
                FileLogger.Error("SetPlayerHeading exception", ex);
            }
        }

        /// <inheritdoc />
        public bool IsPlayerDead()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                return player.IsDead;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
    }
}
