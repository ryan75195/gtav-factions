# GTA V Faction Wars - Territory Control Mod

A strategic territory control modification for Grand Theft Auto V that introduces faction warfare across Los Santos. Three factions, each led by one of the game's protagonists, compete for control of the map's zones through combat, resource management, and tactical AI.

## Features

### Faction System
Three distinct factions based on GTA V's protagonists, each with unique AI strategies:

- **Michael's Organization** - Calculated and defense-oriented. Focuses on high-value targets and protecting valuable holdings.
- **Trevor's Enterprises** - Aggressive and chaotic. High risk tolerance with combat bonuses and relentless expansion.
- **Franklin's Crew** - Opportunistic and mobile. Balanced approach that exploits openings and adapts to situations.

### Territory Control
- **Zone System** - Los Santos divided into controllable regions with circular or polygon boundaries
- **Zone Traits** - Special characteristics (Commercial, Industrial, Residential, etc.) that affect resource generation
- **Adjacency System** - Strategic considerations for zone connectivity and supply lines
- **Takeover Mechanics** - Gradual control percentage shifts during combat

### Economy
- **Resource Types**
  - **Cash** - Primary currency for operations and expenses
  - **Recruitment** - Points for hiring troops (from residential zones)
  - **Weapons** - Military stockpile (from industrial zones)
- **Resource Generation** - Periodic ticks based on controlled zones
- **Supply Lines** - Connected zones provide bonus resources
- **Storage Caps** - Maximum resource limits per faction

### Combat System
- **Ped Pool Management** - Efficient spawning/despawning within GTA V engine limits (~30 active combat peds)
- **Combat Encounters** - Tracked battles with participating peds and outcomes
- **Control Percentage** - Real-time calculation of zone control during fights
- **Reinforcements** - Automatic troop deployment during extended battles
- **Ped Recycling** - Performance optimization through ped reuse

### AI System
- **Strategy Pattern** - Pluggable AI strategies per faction type
- **Zone Evaluation** - Scoring algorithm for target prioritization
- **Resource Allocation** - Smart distribution between attack and defense
- **Aggression Response** - AI reacts to player actions and threats
- **Difficulty Scaling** - Configurable AI challenge levels

### User Interface
- **Map Blips** - Color-coded zone ownership indicators
- **Zone Boundaries** - Visual rendering of territory borders
- **Faction Menus** - NativeUI integration for faction management
- **Combat HUD** - Real-time control percentage and reinforcement timers
- **Notifications** - Event alerts for zone captures and attacks
- **Phone Commands** - Quick actions via in-game phone

### Persistence
- **JSON Save System** - Full game state serialization
- **Save Slots** - Multiple save file support
- **Auto-Save** - Configurable automatic saving
- **Validation** - Save file integrity checking

## Installation

### Prerequisites
- GTA V (PC version)
- [Script Hook V](http://www.dev-c.com/gtav/scripthookv/)
- [Script Hook V .NET](https://github.com/scripthookvdotnet/scripthookvdotnet)

### Setup
1. Install Script Hook V and Script Hook V .NET following their respective instructions
2. Copy `FactionWars.dll` to your GTA V `scripts` folder
3. Launch GTA V - the mod will initialize automatically

## Building from Source

### Requirements
- .NET Framework 4.8 SDK
- Visual Studio 2022 or later (recommended)
- ScriptHookVDotNet3.dll (place in `lib/` folder)

### Build Steps
```bash
# Clone the repository
git clone https://github.com/yourusername/gtav-factions.git
cd gtav-factions

# Restore packages and build
dotnet build FactionWars.sln

# Run tests
dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj
```

## Project Structure

```
gtav-factions/
├── src/
│   └── FactionWars/
│       ├── Core/
│       │   ├── Interfaces/         # Core abstractions (IGameBridge, etc.)
│       │   ├── Models/             # Shared models (Vector3, BlipColor)
│       │   └── Utils/              # Utilities and mock implementations
│       ├── Territory/              # Zone management
│       │   ├── Interfaces/         # IZoneRepository, IZoneService
│       │   ├── Models/             # Zone, ZoneBoundary, ZoneTrait
│       │   ├── Repositories/       # Zone data access
│       │   └── Services/           # Zone business logic
│       ├── Factions/               # Faction state and logic
│       │   ├── Interfaces/         # IFactionRepository, IFactionService
│       │   ├── Models/             # Faction, FactionType, FactionState
│       │   ├── Repositories/       # Faction data access
│       │   └── Services/           # Faction business logic
│       ├── Economy/                # Resource system
│       │   ├── Interfaces/         # IResourceStorage, ISupplyLineService
│       │   ├── Models/             # ResourceType, ResourceTypeInfo
│       │   └── Services/           # Resource generation and management
│       ├── Combat/                 # Combat and ped management
│       │   ├── Interfaces/         # IPedPool, ICombatResultHandler
│       │   ├── Models/             # CombatEncounter, PedHandle
│       │   ├── Pools/              # Ped pool implementations
│       │   └── Services/           # Combat logic services
│       ├── AI/                     # AI decision making
│       │   ├── Interfaces/         # IAIStrategy, IZoneEvaluationService
│       │   ├── Models/             # AIDecision, AIContext
│       │   ├── Strategies/         # Michael/Trevor/Franklin AI
│       │   └── Services/           # AI support services
│       ├── Persistence/            # Save/Load system
│       │   ├── Models/             # GameState, SaveSlotInfo
│       │   └── Services/           # JSON persistence, auto-save
│       ├── UI/                     # NativeUI integration
│       │   ├── Interfaces/         # Menu and display interfaces
│       │   ├── Menus/              # NativeUI menu implementations
│       │   └── Services/           # HUD, notifications, phone
│       ├── Balance/                # Game balance configuration
│       └── Performance/            # Optimization utilities
│           ├── Interfaces/         # IObjectPool, ICacheService
│           ├── Models/             # PerformanceMetric
│           └── Services/           # Pooling, caching, lazy loading
├── tests/
│   └── FactionWars.Tests/
│       ├── Unit/                   # Unit tests by module
│       └── Integration/            # End-to-end scenario tests
├── lib/                            # External dependencies
├── FactionWars.sln
└── README.md
```

## Architecture

### Design Principles
- **SOLID** - Single responsibility, interface segregation, dependency inversion throughout
- **TDD** - All code written test-first with comprehensive coverage
- **Dependency Injection** - All dependencies injected via interfaces for testability
- **Repository Pattern** - Data access abstracted behind repository interfaces
- **Strategy Pattern** - AI behaviors implemented as interchangeable strategies

### Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `IGameBridge` | Abstraction over GTA V native calls for testability |
| `IZoneRepository` | Zone data access |
| `IFactionRepository` | Faction state management |
| `IResourceStorage` | Resource tracking per faction |
| `IPedPool` | Combat ped pooling |
| `IAIStrategy` | Faction-specific AI behaviors |
| `IPersistenceService` | Game state save/load |

### Testing Strategy
- **xUnit** for test framework
- **Moq** for mocking dependencies
- **2600+ tests** covering all modules
- **MockGameBridge** for simulating GTA V native calls

## Configuration

### Balance Settings
The mod supports configurable balance parameters through `IBalanceConfigurationService`:

- Resource generation rates
- Combat damage multipliers
- AI difficulty modifiers
- Takeover thresholds
- Reinforcement cooldowns

### AI Difficulty Levels
- **Easy** - AI makes more mistakes, slower reactions
- **Normal** - Balanced gameplay
- **Hard** - AI plays optimally with resource bonuses

## API Reference

### Zone Management
```csharp
// Query zones
IZoneService.GetAllZones();
IZoneService.GetZoneAtPosition(Vector3 position);
IZoneService.GetZonesByFaction(string factionId);
IZoneService.GetAdjacentZones(string zoneId);

// Zone operations
IZoneService.UpdateZoneOwner(string zoneId, string factionId);
IZoneService.SetZoneContested(string zoneId, bool contested);
```

### Faction Management
```csharp
// Query factions
IFactionService.GetFaction(string factionId);
IFactionService.GetAllFactions();
IFactionService.GetFactionState(string factionId);

// Faction operations
IFactionService.AddResources(string factionId, ResourceType type, int amount);
IFactionService.ModifyTroops(string factionId, int delta);
```

### Combat System
```csharp
// Combat operations
ICombatResultHandler.ProcessCombatResult(CombatEncounter encounter);
IReinforcementService.RequestReinforcements(ReinforcementRequest request);
IControlPercentageCalculator.Calculate(CombatEncounter encounter);
```

### AI System
```csharp
// AI decisions
IAIStrategy.MakeDecision(AIContext context);
IAIStrategy.EvaluateZone(Zone zone, AIContext context);
IAIStrategy.ShouldAttack(Zone zone, AIContext context);
IAIStrategy.ShouldDefend(Zone zone, AIContext context);
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests first (TDD)
4. Implement your changes
5. Ensure all tests pass
6. Submit a pull request

## License

MIT License - See LICENSE file for details.

## Acknowledgments

- [Script Hook V](http://www.dev-c.com/gtav/scripthookv/) by Alexander Blade
- [Script Hook V .NET](https://github.com/scripthookvdotnet/scripthookvdotnet) community
- [NativeUI](https://github.com/Guad/NativeUI) for menu system
