using System;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Service that manages periodic resource generation ticks.
    /// Responsible for tracking time and triggering resource generation for all factions.
    /// </summary>
    public class ResourceTickService : IResourceTickService
    {
        private readonly IFactionService _factionService;
        private readonly IZoneService _zoneService;
        private readonly IZoneTraitResourceModifier _resourceModifier;
        private readonly ISupplyLineService _supplyLineService;

        private int _tickIntervalSeconds;
        private float _elapsedTime;
        private bool _isRunning;
        private float _aiIncomeMultiplier = 1.0f;
        private string? _playerFactionId;

        /// <inheritdoc />
        public event EventHandler<ResourceTickEventArgs>? OnResourceTick;

        /// <inheritdoc />
        public int TickIntervalSeconds => _tickIntervalSeconds;

        /// <inheritdoc />
        public float TimeUntilNextTick => _tickIntervalSeconds - _elapsedTime;

        /// <inheritdoc />
        public float TickProgress => (_elapsedTime / _tickIntervalSeconds) * 100f;

        /// <inheritdoc />
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Creates a new ResourceTickService instance.
        /// </summary>
        /// <param name="factionService">The faction service for managing faction resources.</param>
        /// <param name="zoneService">The zone service for retrieving zone information.</param>
        /// <param name="resourceModifier">The modifier for calculating trait-based resource bonuses.</param>
        /// <param name="supplyLineService">The supply line service for calculating zone connectivity efficiency.</param>
        /// <param name="tickIntervalSeconds">The interval between resource ticks in seconds.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if tickIntervalSeconds is not positive.</exception>
        public ResourceTickService(
            IFactionService factionService,
            IZoneService zoneService,
            IZoneTraitResourceModifier resourceModifier,
            ISupplyLineService supplyLineService,
            int tickIntervalSeconds)
        {
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _zoneService = zoneService ?? throw new ArgumentNullException(nameof(zoneService));
            _resourceModifier = resourceModifier ?? throw new ArgumentNullException(nameof(resourceModifier));
            _supplyLineService = supplyLineService ?? throw new ArgumentNullException(nameof(supplyLineService));

            if (tickIntervalSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickIntervalSeconds),
                    "Tick interval must be positive.");

            _tickIntervalSeconds = tickIntervalSeconds;
            _elapsedTime = 0f;
            _isRunning = false;
        }

        /// <inheritdoc />
        public void Start()
        {
            _isRunning = true;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _isRunning = false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _elapsedTime = 0f;
        }

        /// <inheritdoc />
        public void Update(float deltaTimeSeconds)
        {
            if (!_isRunning)
                return;

            if (deltaTimeSeconds <= 0f)
                return;

            _elapsedTime += deltaTimeSeconds;

            // Process ticks (may be multiple if large delta time)
            while (_elapsedTime >= _tickIntervalSeconds)
            {
                ExecuteTick();
                _elapsedTime -= _tickIntervalSeconds;
            }
        }

        /// <inheritdoc />
        public void ForceTick()
        {
            ExecuteTick();
        }

        /// <inheritdoc />
        public void SetTickInterval(int seconds)
        {
            if (seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds),
                    "Tick interval must be positive.");

            _tickIntervalSeconds = seconds;

            // If elapsed time exceeds new interval, trigger immediate tick
            if (_elapsedTime >= _tickIntervalSeconds)
            {
                while (_elapsedTime >= _tickIntervalSeconds)
                {
                    ExecuteTick();
                    _elapsedTime -= _tickIntervalSeconds;
                }
            }
        }

        /// <inheritdoc />
        public void SetAiIncomeMultiplier(float multiplier)
        {
            _aiIncomeMultiplier = multiplier;
        }

        /// <inheritdoc />
        public void SetPlayerFactionId(string? factionId)
        {
            _playerFactionId = factionId;
        }

        /// <summary>
        /// Executes a single resource tick for all active factions.
        /// </summary>
        private void ExecuteTick()
        {
            var activeFactions = _factionService.GetActiveFactions();

            foreach (var faction in activeFactions)
            {
                var resources = CalculateFactionResources(faction.Id);

                // Determine if this is the player faction
                bool isPlayerFaction = _playerFactionId != null && faction.Id == _playerFactionId;
                float incomeMultiplier = isPlayerFaction ? 1.0f : _aiIncomeMultiplier;

                // Apply AI income multiplier
                int finalCash = (int)(resources.cash * incomeMultiplier);
                int finalRecruitment = (int)(resources.recruitment * incomeMultiplier);
                int finalWeapons = (int)(resources.weapons * incomeMultiplier);

                // Add resources to faction
                if (finalCash > 0)
                    _factionService.AddCash(faction.Id, finalCash);

                if (finalRecruitment > 0)
                    _factionService.AddRecruitmentPoints(faction.Id, finalRecruitment);

                if (finalWeapons > 0)
                    _factionService.AddWeapons(faction.Id, finalWeapons);

                // Raise event
                var args = new ResourceTickEventArgs(
                    faction.Id,
                    finalCash,
                    finalRecruitment,
                    finalWeapons);

                OnResourceTick?.Invoke(this, args);
            }
        }

        /// <summary>
        /// Calculates total resources generated by a faction from all their zones.
        /// </summary>
        /// <param name="factionId">The faction ID to calculate resources for.</param>
        /// <returns>Tuple containing cash, recruitment, and weapons generated.</returns>
        private (int cash, int recruitment, int weapons) CalculateFactionResources(string factionId)
        {
            var zones = _zoneService.GetZonesByOwner(factionId);

            int totalCash = 0;
            int totalRecruitment = 0;
            int totalWeapons = 0;

            foreach (var zone in zones)
            {
                var zoneResources = CalculateZoneResources(factionId, zone);
                totalCash += zoneResources.cash;
                totalRecruitment += zoneResources.recruitment;
                totalWeapons += zoneResources.weapons;
            }

            return (totalCash, totalRecruitment, totalWeapons);
        }

        /// <summary>
        /// Calculates resources generated by a single zone, including supply line efficiency.
        /// </summary>
        /// <param name="factionId">The faction ID that owns the zone.</param>
        /// <param name="zone">The zone to calculate resources for.</param>
        /// <returns>Tuple containing cash, recruitment, and weapons generated.</returns>
        private (int cash, int recruitment, int weapons) CalculateZoneResources(string factionId, Territory.Models.Zone zone)
        {
            // Get base rates from ResourceTypeInfo
            var cashInfo = ResourceTypeInfo.GetInfo(ResourceType.Cash);
            var recruitmentInfo = ResourceTypeInfo.GetInfo(ResourceType.Recruitment);
            var weaponsInfo = ResourceTypeInfo.GetInfo(ResourceType.Weapons);

            // Calculate base generation with strategic value multiplier
            float baseCash = cashInfo.BaseGenerationRate * zone.StrategicValue;
            float baseRecruitment = recruitmentInfo.BaseGenerationRate * zone.StrategicValue;
            float baseWeapons = weaponsInfo.BaseGenerationRate * zone.StrategicValue;

            // Apply trait modifiers
            float cashModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Cash);
            float recruitmentModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Recruitment);
            float weaponsModifier = _resourceModifier.GetModifier(zone.Traits, ResourceType.Weapons);

            // Apply supply line efficiency (connected zones get full efficiency, disconnected get reduced)
            float supplyLineEfficiency = _supplyLineService.GetSupplyLineEfficiency(factionId, zone.Id);

            int finalCash = (int)(baseCash * cashModifier * supplyLineEfficiency);
            int finalRecruitment = (int)(baseRecruitment * recruitmentModifier * supplyLineEfficiency);
            int finalWeapons = (int)(baseWeapons * weaponsModifier * supplyLineEfficiency);

            return (finalCash, finalRecruitment, finalWeapons);
        }
    }
}
