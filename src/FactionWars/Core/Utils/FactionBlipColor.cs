using FactionWars.Core.Interfaces;

namespace FactionWars.Core.Utils
{
    /// <summary>
    /// Maps a faction id to the corresponding <see cref="BlipColor"/> for HUD/map rendering.
    /// Centralizes the "michael → blue, trevor → orange, franklin → green" convention so all
    /// blip sites use it. Unknown faction ids fall back to <see cref="BlipColor.Red"/>
    /// (hostile-by-default), matching pre-faction-color behaviour. Null falls back to White.
    /// </summary>
    public static class FactionBlipColor
    {
        public static BlipColor ForFactionId(string? factionId)
        {
            if (factionId == null) return BlipColor.White;
            return factionId.ToLowerInvariant() switch
            {
                "michael" => BlipColor.MichaelBlue,
                "trevor" => BlipColor.TrevorOrange,
                "franklin" => BlipColor.FranklinGreen,
                _ => BlipColor.Red
            };
        }
    }
}
