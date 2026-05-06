using System;
using FactionWars.ScriptHookV.Logging;
using GTA;
using GTA.Native;
using DomainVector3 = FactionWars.Core.Interfaces.Vector3;

namespace FactionWars.ScriptHookV
{
    public partial class GameBridge
    {
        public void SetPedHealth(int pedHandle, int health)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // CRITICAL: Disable instant headshot kills first
                // SET_PED_SUFFERS_CRITICAL_HITS(ped, false) - prevents one-shot headshot deaths
                Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, ped.Handle, false);

                // CRITICAL: Disable automatic death at low health
                // By default, GTA V kills peds when health drops below FatalInjuryHealthThreshold (~100)
                // This makes high health values ineffective since peds die at the threshold
                ped.DiesOnLowHealth = false;

                // Set injury thresholds very low so peds don't die until health is nearly zero
                // InjuryHealthThreshold: ped becomes "injured" (falls down) below this
                // FatalInjuryHealthThreshold: ped dies instantly when health drops below this
                ped.InjuryHealthThreshold = 1.0f;
                ped.FatalInjuryHealthThreshold = 0.0f;

                // Use natives directly for more reliable health setting
                // SET_PED_MAX_HEALTH is more reliable than setting MaxHealth property
                Function.Call(Hash.SET_PED_MAX_HEALTH, ped.Handle, health);
                Function.Call(Hash.SET_ENTITY_HEALTH, ped.Handle, health, 0);

                // Also set the property as backup
                ped.MaxHealth = health;
                ped.Health = health;

                FileLogger.Info($"SetPedHealth: Set ped {pedHandle} health to {health}, DiesOnLowHealth=false, FatalInjuryThreshold=0");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedHealth exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedCriticalHitsEnabled(int pedHandle, bool enabled)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, ped.Handle, enabled);
                FileLogger.Info($"SetPedCriticalHitsEnabled: ped {pedHandle}, enabled={enabled}");
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedCriticalHitsEnabled exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetPedCombatAttributes(int pedHandle, bool canUseCover, bool willFightArmedPeds)
        {
            try
            {
                var ped = Entity.FromHandle(pedHandle) as Ped;
                if (ped == null || !ped.Exists()) return;

                // Combat attribute flags:
                // 0: CanUseCover
                // 5: CanFightArmedPedsWhenNotArmed
                // 46: AlwaysFight
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 0, canUseCover);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 5, willFightArmedPeds);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, willFightArmedPeds);

                // Make them engage in combat when in combat with someone
                ped.CanSwitchWeapons = true;
            }
            catch (Exception ex)
            {
                FileLogger.Error($"SetPedCombatAttributes exception for ped {pedHandle}", ex);
            }
        }

        /// <inheritdoc />
        public void SetWaypoint(DomainVector3 position)
        {
            try
            {
                // SET_NEW_WAYPOINT native sets a waypoint on the map
                // The player must travel there manually - no teleportation
                Function.Call(Hash.SET_NEW_WAYPOINT, position.X, position.Y);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
        public void ClearWaypoint()
        {
            try
            {
                // SET_WAYPOINT_OFF native removes any active waypoint
                Function.Call(Hash.SET_WAYPOINT_OFF);
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <inheritdoc />
    }
}
