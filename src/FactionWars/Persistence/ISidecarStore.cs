using FactionWars.Persistence.Models;
using System.Collections.Generic;

namespace FactionWars.Persistence
{
    /// <summary>
    /// Read/write store for sidecar JSON files keyed by the primary fingerprint
    /// (TotalPlayTimeSeconds).
    /// </summary>
    public interface ISidecarStore
    {
        /// <summary>
        /// Writes a sidecar to disk. Overwrites any existing sidecar with the same
        /// primary key. Atomic via tmp+rename. Failures are caught and logged;
        /// this method does not throw on IO errors.
        /// </summary>
        void WriteSidecar(Sidecar sidecar);

        /// <summary>
        /// Looks up a sidecar by fingerprint. Performs an O(1) filename lookup
        /// keyed on TotalPlayTimeSeconds, then validates ExactMatch on tiebreakers.
        /// </summary>
        /// <returns>True if a fully-matching sidecar was found.</returns>
        bool TryFindByFingerprint(SaveFingerprint fingerprint, out Sidecar sidecar);

        /// <summary>
        /// Returns all sidecar files currently on disk (in arbitrary order).
        /// </summary>
        IReadOnlyList<Sidecar> ListAll();
    }
}
