namespace FactionWars.Combat.Models
{
    /// <summary>Minimal per-tick view of a vehicle the ambient-traffic suppressor reasons about:
    /// its handle, whether it is a persistent (mod/scripted/player) entity, and its driver ped
    /// handle (-1 when empty).</summary>
    public readonly struct VehicleSnapshot
    {
        public VehicleSnapshot(int handle, bool isPersistent, int driver)
        {
            Handle = handle;
            IsPersistent = isPersistent;
            Driver = driver;
        }

        public int Handle { get; }
        public bool IsPersistent { get; }
        public int Driver { get; } // ped handle, or -1 when the vehicle is empty
    }
}
