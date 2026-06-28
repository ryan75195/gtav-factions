using System;
using GTA;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        // Read-only observability primitives for the behavior-telemetry sampler. These are
        // called at high frequency (once per tracked ped per sample), so they deliberately
        // do NOT log per call — the sampler records their results into behavior_trace.csv.
        // Each is defensive: a bad handle or native hiccup returns the "unknown" sentinel
        // rather than throwing into the sampler.

        /// <inheritdoc />
        public bool IsPedShooting(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return false;
                return Function.Call<bool>(Hash.IS_PED_SHOOTING, ped.Handle);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public string GetSelectedWeapon(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return string.Empty;
                return ped.Weapons.Current.Hash.ToString().ToUpperInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public int GetPedAmmo(int pedHandle)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return -1;
                return ped.Weapons.Current.Ammo;
            }
            catch
            {
                return -1;
            }
        }

        /// <inheritdoc />
        public int GetPedCombatAbilityValue(int pedHandle)
        {
            // GTA exposes SET_PED_COMBAT_ABILITY but no getter, so the live value cannot be
            // read back. Returns the "unknown" sentinel; MockGameBridge returns the value it
            // was configured with for offline tests.
            return -1;
        }
    }
}
