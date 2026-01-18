using System;

namespace FactionWars.Factions.Models
{
    /// <summary>
    /// Represents an RGBA color for faction identification and UI display.
    /// </summary>
    public readonly struct FactionColor : IEquatable<FactionColor>
    {
        /// <summary>
        /// Red component (0-255).
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Green component (0-255).
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Blue component (0-255).
        /// </summary>
        public byte B { get; }

        /// <summary>
        /// Alpha component (0-255), where 255 is fully opaque.
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// Creates a new FactionColor with the specified RGBA values.
        /// Values are clamped to the valid 0-255 range.
        /// </summary>
        /// <param name="r">Red component (0-255).</param>
        /// <param name="g">Green component (0-255).</param>
        /// <param name="b">Blue component (0-255).</param>
        /// <param name="a">Alpha component (0-255), defaults to 255 (fully opaque).</param>
        public FactionColor(int r, int g, int b, int a = 255)
        {
            R = ClampToByte(r);
            G = ClampToByte(g);
            B = ClampToByte(b);
            A = ClampToByte(a);
        }

        private static byte ClampToByte(int value)
        {
            return (byte)Math.Max(0, Math.Min(255, value));
        }

        #region Predefined Colors

        /// <summary>
        /// White color (255, 255, 255, 255).
        /// </summary>
        public static FactionColor White => new FactionColor(255, 255, 255);

        /// <summary>
        /// Black color (0, 0, 0, 255).
        /// </summary>
        public static FactionColor Black => new FactionColor(0, 0, 0);

        /// <summary>
        /// Red color (255, 0, 0, 255).
        /// </summary>
        public static FactionColor Red => new FactionColor(255, 0, 0);

        /// <summary>
        /// Green color (0, 255, 0, 255).
        /// </summary>
        public static FactionColor Green => new FactionColor(0, 255, 0);

        /// <summary>
        /// Blue color (0, 0, 255, 255).
        /// </summary>
        public static FactionColor Blue => new FactionColor(0, 0, 255);

        #endregion

        #region Equality

        public bool Equals(FactionColor other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object? obj)
        {
            return obj is FactionColor color && Equals(color);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + R.GetHashCode();
                hash = hash * 31 + G.GetHashCode();
                hash = hash * 31 + B.GetHashCode();
                hash = hash * 31 + A.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(FactionColor left, FactionColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FactionColor left, FactionColor right)
        {
            return !left.Equals(right);
        }

        #endregion

        public override string ToString()
        {
            return $"RGBA({R}, {G}, {B}, {A})";
        }
    }
}
