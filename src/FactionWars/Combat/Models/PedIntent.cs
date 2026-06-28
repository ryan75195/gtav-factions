using System;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// Describes what a single ped should be doing this tick. Equality is over
    /// (<see cref="Kind"/>, <see cref="Discriminator"/>) only — position/radius ride along for
    /// application but do not, by themselves, force a re-task (mirrors the proven SquadStance
    /// discriminator dedup). The reconciler uses this equality to skip unchanged intents.
    /// </summary>
    public readonly struct PedIntent : IEquatable<PedIntent>
    {
        private PedIntent(PedIntentKind kind, int discriminator, Vector3 position, float radius)
        {
            Kind = kind;
            Discriminator = discriminator;
            Position = position;
            Radius = radius;
        }

        /// <summary>The kind of action.</summary>
        public PedIntentKind Kind { get; }

        /// <summary>Identity of the action within its kind: target handle, ring index, or 0.</summary>
        public int Discriminator { get; }

        /// <summary>Center or destination for area / go-to intents (unused otherwise).</summary>
        public Vector3 Position { get; }

        /// <summary>Radius for area intents (unused otherwise).</summary>
        public float Radius { get; }

        /// <summary>Do nothing this tick.</summary>
        public static PedIntent Idle() => new PedIntent(PedIntentKind.Idle, 0, default, 0f);

        /// <summary>Join the player's group and follow.</summary>
        public static PedIntent FollowPlayer() => new PedIntent(PedIntentKind.FollowPlayer, 0, default, 0f);

        /// <summary>Guard a fixed point. <paramref name="ringIndex"/> distinguishes slots on a ring.</summary>
        public static PedIntent GuardArea(Vector3 center, float radius, int ringIndex)
            => new PedIntent(PedIntentKind.GuardArea, ringIndex, center, radius);

        /// <summary>Run to and fight a specific target ped.</summary>
        public static PedIntent CombatTarget(int targetHandle)
            => new PedIntent(PedIntentKind.CombatTarget, targetHandle, default, 0f);

        /// <summary>Run toward a target ped, stopping at <paramref name="stoppingRange"/> metres,
        /// without engaging yet. Tracks the moving target (TASK_GO_TO_ENTITY).</summary>
        public static PedIntent AdvanceOnTarget(int targetHandle, float stoppingRange)
            => new PedIntent(PedIntentKind.AdvanceOnTarget, targetHandle, default, stoppingRange);

        /// <summary>Seek and fight any hated targets within radius of a center.</summary>
        public static PedIntent SeekHatedTargets(Vector3 center, float radius)
            => new PedIntent(PedIntentKind.SeekHatedTargets, 0, center, radius);

        /// <summary>Wander within a bounded circular area.</summary>
        public static PedIntent WanderArea(Vector3 center, float radius)
            => new PedIntent(PedIntentKind.WanderArea, 0, center, radius);

        /// <summary>Walk to a fixed world coordinate.</summary>
        public static PedIntent GoToCoord(Vector3 destination)
            => new PedIntent(PedIntentKind.GoToCoord, 0, destination, 0f);

        /// <summary>Leave the current vehicle.</summary>
        public static PedIntent LeaveVehicle() => new PedIntent(PedIntentKind.LeaveVehicle, 0, default, 0f);

        /// <inheritdoc />
        public bool Equals(PedIntent other) => Kind == other.Kind && Discriminator == other.Discriminator;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is PedIntent other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Kind * 397) ^ Discriminator;

        public static bool operator ==(PedIntent left, PedIntent right) => left.Equals(right);

        public static bool operator !=(PedIntent left, PedIntent right) => !left.Equals(right);
    }
}
