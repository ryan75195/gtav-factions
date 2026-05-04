using System;

namespace FactionWars.Economy.Interfaces
{
    /// <summary>
    /// Event arguments for when a resource tick occurs.
    /// </summary>
    public class ResourceTickEventArgs : EventArgs
    {
        /// <summary>
        /// The faction ID that received resources.
        /// </summary>
        public string FactionId { get; }

        /// <summary>
        /// Amount of cash generated this tick.
        /// </summary>
        public int CashGenerated { get; }

        /// <summary>
        /// Amount of recruitment points generated this tick.
        /// </summary>
        public int RecruitmentGenerated { get; }

        /// <summary>
        /// Amount of weapons generated this tick.
        /// </summary>
        public int WeaponsGenerated { get; }

        /// <summary>
        /// Creates new resource tick event arguments.
        /// </summary>
        public ResourceTickEventArgs(string factionId, int cash, int recruitment, int weapons)
        {
            FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
            CashGenerated = cash;
            RecruitmentGenerated = recruitment;
            WeaponsGenerated = weapons;
        }
    }
}
