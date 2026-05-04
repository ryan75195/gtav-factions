using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Economy.Models;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.UI
{
    public partial class ResourcesMenuController
    {
        private IncomeSummary CalculateTotalIncome(string? factionId, List<Zone> zones)
        {
            int totalCash = 0;
            int totalRecruitment = 0;
            int totalWeapons = 0;
            var zoneIncomes = new List<ZoneIncomeInfo>();

            if (factionId == null || zones.Count == 0)
            {
                return new IncomeSummary(totalCash, totalRecruitment, totalWeapons, zoneIncomes);
            }

            var cashInfo = ResourceTypeInfo.GetInfo(ResourceType.Cash);
            var recruitmentInfo = ResourceTypeInfo.GetInfo(ResourceType.Recruitment);
            var weaponsInfo = ResourceTypeInfo.GetInfo(ResourceType.Weapons);

            foreach (var zone in zones)
            {
                // Calculate base generation with strategic value
                float baseCash = cashInfo.BaseGenerationRate * zone.StrategicValue;
                float baseRecruitment = recruitmentInfo.BaseGenerationRate * zone.StrategicValue;
                float baseWeapons = weaponsInfo.BaseGenerationRate * zone.StrategicValue;

                // Apply trait modifiers
                float cashModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Cash);
                float recruitmentModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Recruitment);
                float weaponsModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Weapons);

                // Apply supply line efficiency
                float efficiency = _supplyLineService.GetSupplyLineEfficiency(factionId, zone.Id);

                int zoneCash = (int)(baseCash * cashModifier * efficiency);
                int zoneRecruitment = (int)(baseRecruitment * recruitmentModifier * efficiency);
                int zoneWeapons = (int)(baseWeapons * weaponsModifier * efficiency);

                totalCash += zoneCash;
                totalRecruitment += zoneRecruitment;
                totalWeapons += zoneWeapons;

                zoneIncomes.Add(new ZoneIncomeInfo
                {
                    ZoneId = zone.Id,
                    ZoneName = zone.Name,
                    Cash = zoneCash,
                    Recruitment = zoneRecruitment,
                    Weapons = zoneWeapons,
                    Efficiency = efficiency
                });
            }

            return new IncomeSummary(totalCash, totalRecruitment, totalWeapons, zoneIncomes);
        }

        /// <summary>
        /// Handles menu item selection events.
        /// </summary>
        private void OnItemSelected(object? sender, MenuItemSelectedEventArgs e)
        {
            if (e.MenuId != ResourcesMenuId)
                return;

            if (e.ItemId == BackItemId)
            {
                _menuProvider.CloseMenu();
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Internal class to hold zone income information.
        /// </summary>
        private class ZoneIncomeInfo
        {
            public string ZoneId { get; set; } = string.Empty;
            public string ZoneName { get; set; } = string.Empty;
            public int Cash { get; set; }
            public int Recruitment { get; set; }
            public int Weapons { get; set; }
            public float Efficiency { get; set; }
        }

        private sealed class IncomeSummary
        {
            public IncomeSummary(int totalCash, int totalRecruitment, int totalWeapons, List<ZoneIncomeInfo> zoneIncomes)
            {
                TotalCash = totalCash;
                TotalRecruitment = totalRecruitment;
                TotalWeapons = totalWeapons;
                ZoneIncomes = zoneIncomes;
            }

            public int TotalCash { get; }
            public int TotalRecruitment { get; }
            public int TotalWeapons { get; }
            public List<ZoneIncomeInfo> ZoneIncomes { get; }
        }
    }
}
