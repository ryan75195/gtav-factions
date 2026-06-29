using FactionWars.Core.Models;

namespace FactionWars.Telemetry.Models
{
    /// <summary>
    /// One observed per-ped behavior sample. Built by <c>CombatBehaviorSampler</c> and serialized by
    /// the behavior trace sink (which frames it with session id and timestamps). Uses property
    /// initialization rather than a constructor to avoid an unwieldy parameter list.
    /// </summary>
    public class BehaviorSampleRow
    {
        /// <summary>Game time (ms) at which the sample was taken.</summary>
        public int SampleMs { get; set; }

        public int Handle { get; set; }

        public CombatantKind Kind { get; set; }

        public DefenderRole Role { get; set; }

        /// <summary>Equipped weapon name, normalized upper-case; empty if unknown.</summary>
        public string Weapon { get; set; } = string.Empty;

        public bool IsShooting { get; set; }

        public bool InCombat { get; set; }

        /// <summary>Nearest tracked hostile's handle; -1 when none tracked.</summary>
        public int TargetHandle { get; set; } = -1;

        /// <summary>Distance to the nearest tracked hostile; -1 when none tracked.</summary>
        public float DistToTarget { get; set; } = -1f;

        public float DistToPlayer { get; set; }

        public float PosX { get; set; }

        public float PosY { get; set; }

        public float PosZ { get; set; }

        public bool InVehicle { get; set; }

        public bool IsFollowingPlayer { get; set; }

        public int Health { get; set; }

        /// <summary>Combat ability read-back (0/1/2); -1 if unknown.</summary>
        public int CombatAbility { get; set; } = -1;

        /// <summary>Squad engagement: true if the ped held line of sight to its target this sample.
        /// Only meaningful for followers in Search &amp; Destroy; false when no engagement state.</summary>
        public bool HasLineOfSight { get; set; }

        /// <summary>Engagement phase name ("Advance"/"Engage"); empty when no engagement state.</summary>
        public string EnginePhase { get; set; } = string.Empty;

        /// <summary>Milliseconds since the ped last held line of sight; -1 when no engagement state.</summary>
        public int MsSinceLos { get; set; } = -1;
    }
}
