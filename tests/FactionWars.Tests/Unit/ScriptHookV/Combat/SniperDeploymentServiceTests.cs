using System.Collections.Generic;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class SniperDeploymentServiceTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly SniperDeploymentService _service;

        public SniperDeploymentServiceTests()
        {
            _service = new SniperDeploymentService(new PerchResolver(), _bridge);
        }

        private DefenderRoleConfig SniperConfig() =>
            new DefenderRoleConfig(DefenderRole.Sniper, 1500, 275, 50, "WEAPON_SNIPERRIFLE", 0.8f, 2.2f, false);

        private DefenderRoleConfig GruntConfig() =>
            new DefenderRoleConfig(DefenderRole.Grunt, 200, 200, 50, "weapon_pistol", 0.3f, 1.0f, true);

        [Fact]
        public void DeployIfSniper_Sniper_GuardsResolvedPerch()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));
            var center = new Vector3(100f, 100f, 20f);
            // East sample (x≈125) is highest.
            _bridge.GroundZResolver = (x, y, z) => System.Math.Abs(x - 125f) < 1f ? 60f : 21f;

            _service.DeployIfSniper(ped, SniperConfig(), center);

            Assert.True(_bridge.IsPedGuardingArea(ped));
            Assert.Equal(125f, _bridge.GetGuardAreaCenter(ped).X, 0);
        }

        [Fact]
        public void DeployIfSniper_NonSniper_DoesNothing()
        {
            int ped = _bridge.CreatePed("a_m_m_business_01", new Vector3(0f, 0f, 0f));

            _service.DeployIfSniper(ped, GruntConfig(), new Vector3(100f, 100f, 20f));

            Assert.False(_bridge.IsPedGuardingArea(ped));
        }

        [Fact]
        public void UpdateCloseDefense_ThreatClose_SwitchesToSidearm()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));

            _service.UpdateCloseDefense(ped, new List<Vector3> { new Vector3(5f, 0f, 0f) });

            Assert.Equal("weapon_pistol", _bridge.GetPedActiveWeapon(ped));
        }

        [Fact]
        public void UpdateCloseDefense_ThreatFar_UsesRifle()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));

            _service.UpdateCloseDefense(ped, new List<Vector3> { new Vector3(40f, 0f, 0f) });

            Assert.Equal("WEAPON_SNIPERRIFLE", _bridge.GetPedActiveWeapon(ped));
        }

        [Fact]
        public void UpdateCloseDefense_SameFarThreat_SetActiveWeaponCalledOnlyOnce()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));
            var farThreats = new List<Vector3> { new Vector3(40f, 0f, 0f) };

            _service.UpdateCloseDefense(ped, farThreats);
            _service.UpdateCloseDefense(ped, farThreats);

            Assert.Equal(1, _bridge.GetActiveWeaponSetCount(ped));
        }

        [Fact]
        public void UpdateCloseDefense_AfterSniperLeavesActiveSet_ReassertsRifle()
        {
            // Repro: a follower sniper rides in the player's vehicle (drops out of the on-foot set,
            // the engine holsters the rifle), then returns on foot. The weapon cache must not stay
            // stale, or the rifle is never forced back into the sniper's hand after exiting.
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));
            var farThreats = new List<Vector3> { new Vector3(40f, 0f, 0f) };

            _service.UpdateCloseDefense(ped, farThreats);   // on foot: rifle forced (count 1)
            Assert.Equal(1, _bridge.GetActiveWeaponSetCount(ped));

            _service.RetainOnly(new List<int>());           // sniper boards -> drops out of the active set

            _service.UpdateCloseDefense(ped, farThreats);   // back on foot: rifle must be re-forced
            Assert.Equal(2, _bridge.GetActiveWeaponSetCount(ped));
        }

        [Fact]
        public void RetainOnly_KeepsStillActiveSniper_DoesNotReassert()
        {
            // Pruning must not churn snipers that are continuously on foot — the de-dup still holds.
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));
            var farThreats = new List<Vector3> { new Vector3(40f, 0f, 0f) };

            _service.UpdateCloseDefense(ped, farThreats);   // count 1
            _service.RetainOnly(new List<int> { ped });     // still active
            _service.UpdateCloseDefense(ped, farThreats);   // de-dup holds

            Assert.Equal(1, _bridge.GetActiveWeaponSetCount(ped));
        }

        [Fact]
        public void UpdateCloseDefense_NullThreats_SetsRifleAndDoesNotThrow()
        {
            int ped = _bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));

            _service.UpdateCloseDefense(ped, null!);

            Assert.Equal("WEAPON_SNIPERRIFLE", _bridge.GetPedActiveWeapon(ped));
        }
    }
}
