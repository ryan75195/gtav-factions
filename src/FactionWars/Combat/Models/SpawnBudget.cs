namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Manages the shared ped pool allocation for battle spawning.
    /// Enforces both total ped limits and per-side limits.
    /// </summary>
    public class SpawnBudget
    {
        /// <summary>
        /// Default maximum total peds that can be spawned across all battles.
        /// </summary>
        public const int DefaultMaxTotalPeds = 30;

        /// <summary>
        /// Default maximum peds per side (attackers or defenders) in a single battle.
        /// </summary>
        public const int DefaultMaxPerSide = 12;

        /// <summary>
        /// Maximum total peds that can be spawned.
        /// </summary>
        public int MaxTotalPeds { get; }

        /// <summary>
        /// Maximum peds per side in a single battle.
        /// </summary>
        public int MaxPerSide { get; }

        /// <summary>
        /// Number of attacker slots currently allocated.
        /// </summary>
        public int AllocatedAttackers { get; private set; }

        /// <summary>
        /// Number of defender slots currently allocated.
        /// </summary>
        public int AllocatedDefenders { get; private set; }

        /// <summary>
        /// Gets the number of available slots remaining.
        /// </summary>
        public int Available => MaxTotalPeds - AllocatedAttackers - AllocatedDefenders;

        /// <summary>
        /// Creates a new SpawnBudget with default limits.
        /// </summary>
        public SpawnBudget()
            : this(DefaultMaxTotalPeds, DefaultMaxPerSide)
        {
        }

        /// <summary>
        /// Creates a new SpawnBudget with custom limits.
        /// </summary>
        /// <param name="maxTotalPeds">Maximum total peds that can be spawned.</param>
        /// <param name="maxPerSide">Maximum peds per side.</param>
        public SpawnBudget(int maxTotalPeds, int maxPerSide)
        {
            MaxTotalPeds = maxTotalPeds;
            MaxPerSide = maxPerSide;
            AllocatedAttackers = 0;
            AllocatedDefenders = 0;
        }

        /// <summary>
        /// Checks if an attacker can be spawned (under both limits).
        /// </summary>
        public bool CanSpawnAttacker()
        {
            return AllocatedAttackers < MaxPerSide && Available > 0;
        }

        /// <summary>
        /// Checks if a defender can be spawned (under both limits).
        /// </summary>
        public bool CanSpawnDefender()
        {
            return AllocatedDefenders < MaxPerSide && Available > 0;
        }

        /// <summary>
        /// Allocates one attacker slot.
        /// </summary>
        /// <returns>True if allocation succeeded, false if at limit.</returns>
        public bool AllocateAttacker()
        {
            if (!CanSpawnAttacker())
            {
                return false;
            }

            AllocatedAttackers++;
            return true;
        }

        /// <summary>
        /// Allocates one defender slot.
        /// </summary>
        /// <returns>True if allocation succeeded, false if at limit.</returns>
        public bool AllocateDefender()
        {
            if (!CanSpawnDefender())
            {
                return false;
            }

            AllocatedDefenders++;
            return true;
        }

        /// <summary>
        /// Releases one attacker slot.
        /// </summary>
        /// <returns>True if release succeeded, false if none allocated.</returns>
        public bool ReleaseAttacker()
        {
            if (AllocatedAttackers <= 0)
            {
                return false;
            }

            AllocatedAttackers--;
            return true;
        }

        /// <summary>
        /// Releases one defender slot.
        /// </summary>
        /// <returns>True if release succeeded, false if none allocated.</returns>
        public bool ReleaseDefender()
        {
            if (AllocatedDefenders <= 0)
            {
                return false;
            }

            AllocatedDefenders--;
            return true;
        }

        /// <summary>
        /// Resets all allocations to zero.
        /// </summary>
        public void Reset()
        {
            AllocatedAttackers = 0;
            AllocatedDefenders = 0;
        }
    }
}
