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

        // Starting resources per faction - NORMALIZED
        private const int StartingCash = 5000;
        private const int StartingTroopsPerZone = 5;
        private const int StartingZonesPerFaction = 3;

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
            // All factions start equal: $5k cash, 0 reserve troops (all deployed)
            var michaelState = new FactionState(
                CharacterModelFactionDetector.MichaelFactionId,
                StartingCash,
                0  // No reserve troops - all deployed to zones
            );
            _factionRepository.SetState(michaelState);

            var trevorState = new FactionState(
                CharacterModelFactionDetector.TrevorFactionId,
                StartingCash,
                0
            );
            _factionRepository.SetState(trevorState);

            var franklinState = new FactionState(
                CharacterModelFactionDetector.FranklinFactionId,
                StartingCash,
                0
            );
            _factionRepository.SetState(franklinState);
        }

        private void AssignStartingZones()
        {
            // Michael's zones - wealthy west side near his mansion (3 zones)
            var michaelZones = new[]
            {
                "rockford_hills",
                "vinewood",
                "del_perro"
            };

            // Trevor's zones - Blaine County around his trailer (3 zones)
            var trevorZones = new[]
            {
                "sandy_shores",
                "harmony",
                "grapeseed"
            };

            // Franklin's zones - South LS around Forum Drive (3 zones)
            var franklinZones = new[]
            {
                "davis",
                "strawberry",
                "rancho"
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
