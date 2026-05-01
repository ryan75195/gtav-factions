namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// Colors available for map blips.
    /// Values correspond to GTA V's native blip color IDs.
    /// </summary>
    public enum BlipColor
    {
        White = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
        Yellow = 66,
        Orange = 17,
        LightBlue = 18,  // Cyan/Light Blue for friendly defenders
        Purple = 27,
        Pink = 41,
        MichaelBlue = 42,
        FranklinGreen = 43,
        TrevorOrange = 44,

        // Muted versions for territory markers (distinct from ped blips)
        MutedBlue = 38,      // Dark blue for Michael's territories
        MutedGreen = 25,     // Olive/dark green for Franklin's territories
        MutedOrange = 47     // Dark orange/brown for Trevor's territories
    }
}
