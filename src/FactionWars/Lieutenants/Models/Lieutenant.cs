using System;
using System.Collections.Generic;

namespace FactionWars.Lieutenants.Models
{
    /// <summary>
    /// Represents a lieutenant (commander) that can be assigned to zones.
    /// Lieutenants provide bonuses based on their traits and level.
    /// They can defect between factions and have loyalty that affects their behavior.
    /// </summary>
    public class Lieutenant : IEquatable<Lieutenant>
    {
        private const int MaxLoyalty = 100;
        private const int MinLoyalty = 0;
        private const int LoyaltyThreshold = 30;
        private const int DefectionRiskThreshold = 20;
        private const int ExperiencePerLevel = 1000;
        private const int MaxLevel = 10;
        private const int DefectionLoyaltyPenalty = 50;

        private string _factionId;
        private int _loyalty;
        private int _experience;
        private LieutenantStatus _status;
        private readonly HashSet<LieutenantTrait> _traits;

        /// <summary>
        /// Unique identifier for this lieutenant.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Display name for this lieutenant.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The faction this lieutenant currently belongs to.
        /// </summary>
        public string FactionId => _factionId;

        /// <summary>
        /// The original faction this lieutenant belonged to (before any defections).
        /// </summary>
        public string OriginalFactionId { get; }

        /// <summary>
        /// The zone this lieutenant is currently assigned to, if any.
        /// </summary>
        public string? AssignedZoneId { get; private set; }

        /// <summary>
        /// Whether this lieutenant is currently assigned to a zone.
        /// </summary>
        public bool IsAssigned => AssignedZoneId != null;

        /// <summary>
        /// The current loyalty of this lieutenant (0-100).
        /// Higher loyalty means less chance of defection.
        /// </summary>
        public int Loyalty => _loyalty;

        /// <summary>
        /// Whether this lieutenant is considered loyal (above threshold).
        /// </summary>
        public bool IsLoyal => _loyalty >= LoyaltyThreshold;

        /// <summary>
        /// Whether this lieutenant is at risk of defection (very low loyalty).
        /// </summary>
        public bool IsAtRiskOfDefection => _loyalty < DefectionRiskThreshold;

        /// <summary>
        /// The current experience points of this lieutenant.
        /// </summary>
        public int Experience => _experience;

        /// <summary>
        /// The current level of this lieutenant (1-10).
        /// </summary>
        public int Level
        {
            get
            {
                int level = 1 + (_experience / ExperiencePerLevel);
                return Math.Min(level, MaxLevel);
            }
        }

        /// <summary>
        /// The current status of this lieutenant.
        /// </summary>
        public LieutenantStatus Status => _status;

        /// <summary>
        /// The faction that captured this lieutenant, if any.
        /// </summary>
        public string? CapturedByFactionId { get; private set; }

        /// <summary>
        /// Whether this lieutenant has ever defected from their original faction.
        /// </summary>
        public bool HasDefected => _factionId != OriginalFactionId;

        /// <summary>
        /// Whether this lieutenant is available for assignment.
        /// </summary>
        public bool IsAvailable => _status == LieutenantStatus.Active && !IsAssigned;

        /// <summary>
        /// The traits this lieutenant possesses.
        /// </summary>
        public IReadOnlyCollection<LieutenantTrait> Traits => _traits;

        /// <summary>
        /// Creates a new lieutenant with the specified properties.
        /// </summary>
        /// <param name="id">Unique identifier for the lieutenant.</param>
        /// <param name="name">Display name for the lieutenant.</param>
        /// <param name="factionId">The faction this lieutenant belongs to.</param>
        /// <param name="loyalty">Initial loyalty value (0-100, defaults to 100).</param>
        /// <exception cref="ArgumentNullException">Thrown if required parameters are null.</exception>
        /// <exception cref="ArgumentException">Thrown if required parameters are empty/whitespace.</exception>
        public Lieutenant(
            string id,
            string name,
            string factionId,
            int loyalty = 100)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));

            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty or whitespace.", nameof(name));

            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (string.IsNullOrWhiteSpace(factionId))
                throw new ArgumentException("Faction ID cannot be empty or whitespace.", nameof(factionId));

            Id = id;
            Name = name;
            _factionId = factionId;
            OriginalFactionId = factionId;
            _loyalty = Math.Max(MinLoyalty, Math.Min(loyalty, MaxLoyalty));
            _experience = 0;
            _status = LieutenantStatus.Active;
            _traits = new HashSet<LieutenantTrait>();
        }

        /// <summary>
        /// Assigns this lieutenant to a zone.
        /// </summary>
        /// <param name="zoneId">The zone to assign to.</param>
        public void AssignToZone(string zoneId)
        {
            AssignedZoneId = zoneId;
        }

        /// <summary>
        /// Unassigns this lieutenant from their current zone.
        /// </summary>
        public void Unassign()
        {
            AssignedZoneId = null;
        }

        /// <summary>
        /// Adjusts this lieutenant's loyalty by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to adjust (positive or negative).</param>
        public void AdjustLoyalty(int amount)
        {
            int newLoyalty = _loyalty + amount;
            _loyalty = Math.Max(MinLoyalty, Math.Min(newLoyalty, MaxLoyalty));
        }

        /// <summary>
        /// Adds experience to this lieutenant.
        /// </summary>
        /// <param name="amount">The amount of experience to add (negative values are ignored).</param>
        public void GainExperience(int amount)
        {
            if (amount > 0)
            {
                _experience += amount;
            }
        }

        /// <summary>
        /// Kills this lieutenant, setting their status to Deceased.
        /// </summary>
        public void Kill()
        {
            _status = LieutenantStatus.Deceased;
            Unassign();
        }

        /// <summary>
        /// Captures this lieutenant by the specified faction.
        /// </summary>
        /// <param name="capturingFactionId">The faction that captured this lieutenant.</param>
        public void Capture(string capturingFactionId)
        {
            _status = LieutenantStatus.Captured;
            CapturedByFactionId = capturingFactionId;
            Unassign();
        }

        /// <summary>
        /// Releases this lieutenant from capture.
        /// </summary>
        public void Release()
        {
            _status = LieutenantStatus.Active;
            CapturedByFactionId = null;
        }

        /// <summary>
        /// Adds a trait to this lieutenant.
        /// </summary>
        /// <param name="trait">The trait to add.</param>
        public void AddTrait(LieutenantTrait trait)
        {
            _traits.Add(trait);
        }

        /// <summary>
        /// Removes a trait from this lieutenant.
        /// </summary>
        /// <param name="trait">The trait to remove.</param>
        public void RemoveTrait(LieutenantTrait trait)
        {
            _traits.Remove(trait);
        }

        /// <summary>
        /// Checks if this lieutenant has the specified trait.
        /// </summary>
        /// <param name="trait">The trait to check for.</param>
        /// <returns>True if the lieutenant has this trait.</returns>
        public bool HasTrait(LieutenantTrait trait)
        {
            return _traits.Contains(trait);
        }

        /// <summary>
        /// Makes this lieutenant defect to another faction.
        /// </summary>
        /// <param name="newFactionId">The faction to defect to.</param>
        /// <exception cref="InvalidOperationException">Thrown if defecting to same faction or if deceased.</exception>
        public void Defect(string newFactionId)
        {
            if (_status == LieutenantStatus.Deceased)
                throw new InvalidOperationException("Cannot defect when deceased.");

            if (_factionId == newFactionId)
                throw new InvalidOperationException("Cannot defect to the same faction.");

            // If captured, release when defecting to the capturing faction
            if (_status == LieutenantStatus.Captured)
            {
                Release();
            }

            _factionId = newFactionId;
            Unassign();

            // Defectors start with reduced loyalty
            _loyalty = DefectionLoyaltyPenalty;
        }

        #region Equality

        public bool Equals(Lieutenant? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is Lieutenant lieutenant && Equals(lieutenant);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Lieutenant? left, Lieutenant? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Lieutenant? left, Lieutenant? right)
        {
            return !(left == right);
        }

        #endregion

        public override string ToString()
        {
            return $"Lieutenant[{Id}]: {Name} (Faction: {FactionId}, Level: {Level})";
        }
    }
}
