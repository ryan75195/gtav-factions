namespace FactionWars.Telemetry.Models
{
    public enum ZoneEventType { Captured, Lost, Neutralized }
    public enum BattleEventType { Started, Ended }
    public enum BattleOutcome { AttackerWon, DefenderWon, AttackerRetreated, Stalemate }
    public enum AIDecisionTypeMeta { Attack, Defend, Reinforce, Idle, Other }
    public enum MatchMetaEventType
    {
        MatchStart, Victory, Defeat, DifficultyChanged,
        ModSessionStart, ModSessionEnd
    }
    public enum PlayerEventType
    {
        Kill, Death, FollowerRecruited, FollowerDied,
        ZoneEntered, ZoneExited, RespawnAtHospital
    }
    public enum AllocationSource { Player, AI, Initial }
}
