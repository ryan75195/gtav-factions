using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>Applies player combat stats from config to the game bridge at init and on respawn.</summary>
    public sealed class PlayerStatsApplier
    {
        private readonly IGameBridge _bridge;
        private readonly ICombatantStatsProvider _stats;

        public PlayerStatsApplier(IGameBridge bridge, ICombatantStatsProvider stats)
        {
            _bridge = bridge;
            _stats = stats;
        }

        public void Apply()
        {
            var p = _stats.GetPlayerStats();
            _bridge.SetPlayerMaxHealth(p.MaxHealth);
            _bridge.SetPedArmor(_bridge.GetPlayerPedHandle(), p.SpawnArmor);
            _bridge.SetPlayerWeaponDamageModifier(p.OutgoingDamageMultiplier);
            _bridge.SetPlayerWeaponDefenseModifier(p.IncomingDamageMultiplier);
            FileLogger.Info($"PlayerStatsApplier: maxHealth={p.MaxHealth} armor={p.SpawnArmor} dmgMod={p.OutgoingDamageMultiplier:F2} defMod={p.IncomingDamageMultiplier:F2}");
        }
    }
}
