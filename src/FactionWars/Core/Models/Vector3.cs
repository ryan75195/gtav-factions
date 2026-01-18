namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// A simple 3D vector structure for position and coordinate calculations.
    /// Provides an abstraction over GTA V's native Vector3 for testability.
    /// </summary>
    public readonly struct Vector3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 Zero => new Vector3(0, 0, 0);

        public float DistanceTo(Vector3 other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public float DistanceTo2D(Vector3 other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3 other && X == other.X && Y == other.Y && Z == other.Z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X.GetHashCode();
                hash = hash * 31 + Y.GetHashCode();
                hash = hash * 31 + Z.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
        public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
