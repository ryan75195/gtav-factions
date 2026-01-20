using System;
using System.Collections.Generic;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Initializes the three factions with their starting conditions.
    /// Sets up Michael, Trevor, and Franklin factions with their initial
    /// zones, troops, and cash as per game design requirements.
    /// </summary>
    public class FactionInitializer
    {
        private readonly IFactionRepository _factionRepository;
        private readonly IZoneRepository _zoneRepository;
        private bool _isInitialized;

        // Starting resources per faction
        private const int MichaelStartingCash = 10000;
        private const int MichaelStartingTroops = 50;
        private const int MichaelStartingZones = 8;

        private const int TrevorStartingCash = 8000;
        private const int TrevorStartingTroops = 60;
        private const int TrevorStartingZones = 10;

        private const int FranklinStartingCash = 5000;
        private const int FranklinStartingTroops = 30;
        private const int FranklinStartingZones = 5;

        /// <summary>
        /// Gets whether the factions have been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Creates a new FactionInitializer.
        /// </summary>
        /// <param name="factionRepository">The faction repository to populate.</param>
        /// <param name="zoneRepository">The zone repository to read zones from.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public FactionInitializer(IFactionRepository factionRepository, IZoneRepository zoneRepository)
        {
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
        }

        /// <summary>
        /// Initializes all three factions with their starting conditions.
        /// Creates the factions, sets up their initial states, and assigns starting zones.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if Initialize has already been called.</exception>
        public void Initialize()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Factions have already been initialized.");
            }

            // Create the three factions
            CreateFactions();

            // Initialize faction states with starting resources
            InitializeFactionStates();

            // Assign starting zones to each faction
            AssignStartingZones();

            _isInitialized = true;
        }

        private void CreateFactions()
        {
            // Michael's faction - Blue color (professional, organized)
            var michaelFaction = new Faction(
                CharacterModelFactionDetector.MichaelFactionId,
                "Michael",
                "Michael De Santa",
                "A professional operation focused on high-value targets and strategic expansion.",
                new FactionColor(0, 128, 255) // Blue
            );

            // Trevor's faction - Orange color (chaotic, aggressive)
            var trevorFaction = new Faction(
                CharacterModelFactionDetector.TrevorFactionId,
                "Trevor",
                "Trevor Philips",
                "A chaotic force of destruction with sheer numbers and aggression.",
                new FactionColor(255, 128, 0) // Orange
            );

            // Franklin's faction - Green color (street smart, growing)
            var franklinFaction = new Faction(
                CharacterModelFactionDetector.FranklinFactionId,
                "Franklin",
                "Franklin Clinton",
                "A rising power built from the streets with loyalty and determination.",
                new FactionColor(0, 200, 0) // Green
            );

            _factionRepository.Add(michaelFaction);
            _factionRepository.Add(trevorFaction);
            _factionRepository.Add(franklinFaction);
        }

        private void InitializeFactionStates()
        {
            // Michael: 8 zones, 50 troops, $10k
            var michaelState = new FactionState(
                CharacterModelFactionDetector.MichaelFactionId,
                MichaelStartingCash,
                MichaelStartingTroops
            );
            _factionRepository.SetState(michaelState);

            // Trevor: 10 zones, 60 troops, $8k
            var trevorState = new FactionState(
                CharacterModelFactionDetector.TrevorFactionId,
                TrevorStartingCash,
                TrevorStartingTroops
            );
            _factionRepository.SetState(trevorState);

            // Franklin: 5 zones, 30 troops, $5k
            var franklinState = new FactionState(
                CharacterModelFactionDetector.FranklinFactionId,
                FranklinStartingCash,
                FranklinStartingTroops
            );
            _factionRepository.SetState(franklinState);
        }

        private void AssignStartingZones()
        {
            // Michael's zones - wealthy Los Santos areas (8 zones)
            var michaelZones = new[]
            {
                "rockford_hills",    // Wealthy residential
                "vinewood",          // Entertainment district
                "richman",           // High-value residential
                "del_perro",         // Coastal commercial
                "morningwood",       // Residential
                "pillbox_hill",      // Downtown commercial
                "downtown",          // City center
                "vespucci"           // Coastal area
            };

            // Trevor's zones - Blaine County and industrial areas (10 zones)
            var trevorZones = new[]
            {
                "sandy_shores",          // Trevor's home base
                "grapeseed",             // Rural area
                "harmony",               // Small town
                "alamo_sea",             // Lake area
                "grand_senora_desert",   // Desert
                "trevor_airfield",       // Trevor's airfield
                "paleto_bay",            // Northern town
                "paleto_forest",         // Forest area
                "chiliad_wilderness",    // Mountain area
                "cypress_flats"          // Industrial
            };

            // Franklin's zones - South Los Santos and port areas (5 zones)
            var franklinZones = new[]
            {
                "davis",                 // Franklin's home area
                "strawberry",            // South LS residential
                "rancho",                // South LS
                "port_of_los_santos",    // Port
                "elysian_island"         // Port area
            };

            AssignZonesToFaction(CharacterModelFactionDetector.MichaelFactionId, michaelZones);
            AssignZonesToFaction(CharacterModelFactionDetector.TrevorFactionId, trevorZones);
            AssignZonesToFaction(CharacterModelFactionDetector.FranklinFactionId, franklinZones);
        }

        private void AssignZonesToFaction(string factionId, IEnumerable<string> zoneIds)
        {
            var state = _factionRepository.GetState(factionId);
            if (state == null)
            {
                throw new InvalidOperationException($"Faction state not found for faction: {factionId}");
            }

            foreach (var zoneId in zoneIds)
            {
                var zone = _zoneRepository.GetById(zoneId);
                if (zone != null)
                {
                    // Set the zone owner
                    zone.OwnerFactionId = factionId;
                    _zoneRepository.Update(zone);

                    // Add zone to faction state
                    state.AddZone(zoneId);
                }
            }

            // Update the faction state
            _factionRepository.SetState(state);
        }
    }
}
