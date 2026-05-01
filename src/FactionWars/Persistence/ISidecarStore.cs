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
        /// primary key. Atomic via tmp+File.Replace. Failures are caught and logged;
        /// this method does not throw on IO errors.
        /// </summary>
        /// <returns>True if the sidecar was successfully written to disk.</returns>
        bool WriteSidecar(Sidecar sidecar);

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

        /// <summary>
        /// Finds the sidecar with the largest TotalPlayTimeSeconds that is &lt;= currentPlayTime
        /// and within (currentPlayTime - maxBackwardSeconds, currentPlayTime]. Used at load time
        /// because GTA's TOTAL_PLAYING_TIME stat advances during the post-load animation, so the
        /// value the script reads is slightly larger than the value embedded in the save file.
        /// </summary>
        bool TryFindClosestByPlayTime(long currentPlayTime, long maxBackwardSeconds, out Sidecar sidecar);
    }
}
