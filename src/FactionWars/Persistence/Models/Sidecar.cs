using System;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Sidecar JSON payload — mirrors a single GTA V save. Keyed on disk by
    /// the fingerprint's TotalPlayTimeSeconds.
    /// </summary>
    public sealed class Sidecar
    {
        public int Version { get; set; } = 1;

        public SaveFingerprint Fingerprint { get; set; } = new SaveFingerprint();

        public DateTime WrittenAtUtc { get; set; }

        /// <summary>Best-effort: filename of the GTA save this sidecar was written for. Not authoritative.</summary>
        public string NativeSaveFilename { get; set; } = string.Empty;

        /// <summary>Recorded for a future restore-position feature; not consumed in v1.</summary>
        public PlayerPosition PlayerPosition { get; set; } = new PlayerPosition();

        public GameState GameState { get; set; } = new GameState();
    }
}
