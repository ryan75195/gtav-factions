namespace FactionWars.Core.Interfaces
{
    /// <summary>
    /// What killed the player: the killing weapon's normalized name and the killer ped's handle.
    /// Read from GTA's death natives at the alive-to-dead transition. <see cref="WeaponName"/> is
    /// empty and <see cref="KillerHandle"/> is -1 when the cause is unknown.
    /// </summary>
    public readonly struct PlayerDeathCause
    {
        public PlayerDeathCause(string weaponName, int killerHandle)
        {
            WeaponName = weaponName;
            KillerHandle = killerHandle;
        }

        public string WeaponName { get; }

        public int KillerHandle { get; }
    }
}
