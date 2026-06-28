using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Represents a handle to a spawned ped (pedestrian/NPC) in the game world.
    /// Wraps the raw integer handle from GTA V native functions with additional metadata
    /// and state tracking for the ped pool system.
    /// </summary>
    public class PedHandle : IEquatable<PedHandle>
    {
        /// <summary>
        /// The raw handle value from GTA V native functions.
        /// </summary>
        public int Handle { get; }

        /// <summary>
        /// The faction this ped belongs to, if any.
        /// </summary>
        public string? FactionId { get; }

        /// <summary>
        /// The position where this ped was spawned.
        /// </summary>
        public Vector3 SpawnPosition { get; }

        /// <summary>
        /// The model name used to spawn this ped.
        /// </summary>
        public string? ModelName { get; }

        /// <summary>
        /// The zone this ped is assigned to, if any.
        /// </summary>
        public string? ZoneId { get; }

        /// <summary>
        /// The defender tier of this ped, if it's a defender.
        /// Null for attackers, followers, or other non-defender peds.
        /// </summary>
        public DefenderRole? DefenderRole { get; }

        /// <summary>
        /// The UTC time when this ped handle was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Whether this ped has been marked for deletion.
        /// </summary>
        public bool IsMarkedForDeletion { get; private set; }

        /// <summary>
        /// Whether this ped has been recycled for reuse.
        /// </summary>
        public bool IsRecycled { get; private set; }

        /// <summary>
        /// Returns true if this handle represents a valid ped (non-negative handle).
        /// </summary>
        public bool IsValid => Handle >= 0;

        /// <summary>
        /// Returns true if this ped is a defender (has a defender tier assigned).
        /// </summary>
        public bool IsDefender => DefenderRole.HasValue;

        /// <summary>
        /// Represents an invalid ped handle (handle = -1).
        /// </summary>
        public static PedHandle Invalid => new PedHandle(-1);

        /// <summary>
        /// Creates a new PedHandle with the specified handle and optional metadata.
        /// </summary>
        /// <param name="handle">The raw handle value from GTA V.</param>
        /// <param name="factionId">The faction this ped belongs to.</param>
        /// <param name="spawnPosition">The position where the ped was spawned.</param>
        /// <param name="modelName">The model name used for spawning.</param>
        /// <param name="zoneId">The zone this ped is assigned to.</param>
        /// <param name="defenderTier">The defender tier if this ped is a defender.</param>
        public PedHandle(
            int handle,
            string? factionId = null,
            Vector3 spawnPosition = default,
            string? modelName = null,
            string? zoneId = null,
            DefenderRole? defenderTier = null)
        {
            Handle = handle;
            FactionId = factionId;
            SpawnPosition = spawnPosition;
            ModelName = modelName;
            ZoneId = zoneId;
            DefenderRole = defenderTier;
            CreatedAt = DateTime.UtcNow;
            IsMarkedForDeletion = false;
            IsRecycled = false;
        }

        /// <summary>
        /// Marks this ped for deletion from the world.
        /// </summary>
        public void MarkForDeletion()
        {
            IsMarkedForDeletion = true;
        }

        /// <summary>
        /// Marks this ped as recycled for potential reuse.
        /// </summary>
        public void MarkAsRecycled()
        {
            IsRecycled = true;
        }

        /// <summary>
        /// Gets the time since this ped handle was created.
        /// </summary>
        /// <returns>The age of this ped handle.</returns>
        public TimeSpan GetAge()
        {
            return DateTime.UtcNow - CreatedAt;
        }

        #region Conversions

        /// <summary>
        /// Implicit conversion to int, returning the raw handle.
        /// </summary>
        public static implicit operator int(PedHandle pedHandle) => pedHandle.Handle;

        /// <summary>
        /// Explicit conversion from int, creating a basic PedHandle.
        /// </summary>
        public static explicit operator PedHandle(int handle) => new PedHandle(handle);

        #endregion

        #region Equality

        public bool Equals(PedHandle? other)
        {
            if (other is null) return false;
            return Handle == other.Handle;
        }

        public override bool Equals(object? obj)
        {
            return obj is PedHandle handle && Equals(handle);
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        public static bool operator ==(PedHandle? left, PedHandle? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PedHandle? left, PedHandle? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            if (!IsValid)
            {
                return "PedHandle[Invalid]";
            }

            var faction = FactionId != null ? $", Faction={FactionId}" : "";
            return $"PedHandle[{Handle}{faction}]";
        }
    }
}
