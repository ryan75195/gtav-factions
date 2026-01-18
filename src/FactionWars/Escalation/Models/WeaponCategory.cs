namespace FactionWars.Escalation.Models
{
    /// <summary>
    /// Represents the category of a weapon.
    /// Each category contains different weapons that fit a similar role in combat.
    /// </summary>
    public enum WeaponCategory
    {
        /// <summary>
        /// Pistols and handguns - basic sidearms available early.
        /// </summary>
        Pistol = 0,

        /// <summary>
        /// Submachine guns - rapid fire, short range weapons.
        /// </summary>
        SMG = 1,

        /// <summary>
        /// Shotguns - high damage at close range.
        /// </summary>
        Shotgun = 2,

        /// <summary>
        /// Assault rifles - versatile medium-range weapons.
        /// </summary>
        AssaultRifle = 3,

        /// <summary>
        /// Light machine guns - high capacity, sustained fire.
        /// </summary>
        LMG = 4,

        /// <summary>
        /// Sniper rifles - long range precision weapons.
        /// </summary>
        Sniper = 5,

        /// <summary>
        /// Heavy weapons - explosives, launchers, miniguns.
        /// </summary>
        Heavy = 6,

        /// <summary>
        /// Melee weapons - knives, bats, and other close combat items.
        /// </summary>
        Melee = 7,

        /// <summary>
        /// Thrown weapons - grenades, molotovs, and other throwables.
        /// </summary>
        Thrown = 8
    }
}
