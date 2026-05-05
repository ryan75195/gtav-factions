using System;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Interfaces
{
    public interface ITelemetryServiceOptions
    {
        Func<int>? GetPlayerPedHandle { get; }
        Func<bool>? IsPlayerDead { get; }
        Func<string?>? GetCurrentZoneId { get; }
        Func<Vector3>? GetPlayerPosition { get; }
        Func<string, bool>? IsFirstTimeSeenSave { get; }
        IZoneBattleManager? ZoneBattleManager { get; }
        IAIController? AIController { get; }
        IZoneDefenderAllocationService? AllocationService { get; }
        IResourceTickService? ResourceTickService { get; }
        BattleAttackerManager? BattleAttackerManager { get; }
        VictoryManager? VictoryManager { get; }
        IDifficultyService? DifficultyService { get; }
        NativeSaveWatcher? NativeSaveWatcher { get; }
    }
}
