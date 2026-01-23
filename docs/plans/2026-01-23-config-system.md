# Configuration System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a runtime configuration system that reads from `config.json`, generates defaults if missing, and wires values to gameplay systems.

**Architecture:** A `GameConfig` model class with sections for AI, Combat, Economy, Spawning, and Persistence. A `ConfigLoader` service reads/writes JSON from the FactionWars scripts folder. Services receive config values via dependency injection from `ServiceContainerFactory`.

**Tech Stack:** C# .NET 4.8, Newtonsoft.Json (already in project), ScriptHookV

---

## Task 1: Delete Old Config File

**Files:**
- Delete: `E:\SteamLibrary\steamapps\common\Grand Theft Auto V\scripts\FactionWars\config.json`

**Step 1: Delete the unused config file**

```bash
rm "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/FactionWars/config.json"
```

**Step 2: Verify deletion**

```bash
ls "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/FactionWars/"
```
Expected: Only `zones.json` remains

---

## Task 2: Create GameConfig Model

**Files:**
- Create: `src/FactionWars/Configuration/GameConfig.cs`
- Test: `tests/FactionWars.Tests/Unit/Configuration/GameConfigTests.cs`

**Step 1: Write the failing test**

```csharp
using FactionWars.Configuration;
using Xunit;

namespace FactionWars.Tests.Unit.Configuration
{
    public class GameConfigTests
    {
        [Fact]
        public void Default_HasValidAISettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(60f, config.AI.DecisionIntervalSeconds);
            Assert.Equal(0.3f, config.AI.MichaelAggressiveness);
            Assert.Equal(0.85f, config.AI.TrevorAggressiveness);
            Assert.Equal(0.6f, config.AI.FranklinAggressiveness);
        }

        [Fact]
        public void Default_HasValidCombatSettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(1.5f, config.Combat.DefenderAdvantage);
            Assert.Equal(12, config.Combat.MaxSpawnedPedsPerSide);
            Assert.Equal(30, config.Combat.MaxTotalPeds);
        }

        [Fact]
        public void Default_HasValidEconomySettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(100, config.Economy.CashBaseRate);
            Assert.Equal(10, config.Economy.RecruitmentBaseRate);
            Assert.Equal(5, config.Economy.WeaponsBaseRate);
            Assert.Equal(60, config.Economy.ResourceTickIntervalSeconds);
        }

        [Fact]
        public void Default_HasValidSpawningSettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(5000, config.Initialization.StartingCash);
            Assert.Equal(5, config.Initialization.StartingTroopsPerZone);
            Assert.Equal(3, config.Initialization.StartingZonesPerFaction);
        }

        [Fact]
        public void Default_HasValidPersistenceSettings()
        {
            var config = GameConfig.Default;

            Assert.Equal(300, config.Persistence.AutoSaveIntervalSeconds);
            Assert.Equal(10, config.Persistence.MaxSaveSlots);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~GameConfigTests" -v n`
Expected: FAIL - namespace/class not found

**Step 3: Write GameConfig implementation**

```csharp
namespace FactionWars.Configuration
{
    /// <summary>
    /// Root configuration object for FactionWars mod.
    /// </summary>
    public class GameConfig
    {
        public AIConfig AI { get; set; } = new AIConfig();
        public CombatConfig Combat { get; set; } = new CombatConfig();
        public EconomyConfig Economy { get; set; } = new EconomyConfig();
        public InitializationConfig Initialization { get; set; } = new InitializationConfig();
        public PersistenceConfig Persistence { get; set; } = new PersistenceConfig();

        /// <summary>
        /// Creates a GameConfig with all default values.
        /// </summary>
        public static GameConfig Default => new GameConfig();
    }

    public class AIConfig
    {
        // Decision intervals
        public float DecisionIntervalSeconds { get; set; } = 60f;
        public float RecruitmentIntervalSeconds { get; set; } = 60f;

        // Per-faction aggressiveness (0.0 = passive, 1.0 = very aggressive)
        public float MichaelAggressiveness { get; set; } = 0.3f;
        public float TrevorAggressiveness { get; set; } = 0.85f;
        public float FranklinAggressiveness { get; set; } = 0.6f;

        // Per-faction risk tolerance (0.0 = risk-averse, 1.0 = risk-seeking)
        public float MichaelRiskTolerance { get; set; } = 0.3f;
        public float TrevorRiskTolerance { get; set; } = 0.8f;
        public float FranklinRiskTolerance { get; set; } = 0.6f;

        // AI costs
        public int RecruitCostPerTroop { get; set; } = 200;
        public int AttackCostPerTroop { get; set; } = 50;
        public int MaxRecruitPerCycle { get; set; } = 5;
    }

    public class CombatConfig
    {
        // Battle mechanics
        public float DefenderAdvantage { get; set; } = 1.5f;
        public float BaseCasualtyRate { get; set; } = 0.3f;

        // Troop strength by tier
        public float BasicTroopStrength { get; set; } = 1.0f;
        public float MediumTroopStrength { get; set; } = 1.5f;
        public float HeavyTroopStrength { get; set; } = 2.0f;

        // Troop resilience (lower = more resilient)
        public float BasicResilienceModifier { get; set; } = 1.0f;
        public float MediumResilienceModifier { get; set; } = 0.75f;
        public float HeavyResilienceModifier { get; set; } = 0.5f;

        // Battle duration
        public float MinBattleDurationSeconds { get; set; } = 60f;
        public float MaxBattleDurationSeconds { get; set; } = 300f;

        // Spawning limits
        public int MaxSpawnedPedsPerSide { get; set; } = 12;
        public int MaxTotalPeds { get; set; } = 30;
        public int TroopsPerSpawnedPed { get; set; } = 5;

        // Takeover
        public float AttackerVictoryThreshold { get; set; } = 100f;
        public float DefenderVictoryThreshold { get; set; } = 0f;
    }

    public class EconomyConfig
    {
        // Base resource generation per zone per tick
        public int CashBaseRate { get; set; } = 100;
        public int RecruitmentBaseRate { get; set; } = 10;
        public int WeaponsBaseRate { get; set; } = 5;

        // Resource caps
        public int CashCap { get; set; } = 100000;
        public int RecruitmentCap { get; set; } = 1000;
        public int WeaponsCap { get; set; } = 500;

        // Tick interval
        public int ResourceTickIntervalSeconds { get; set; } = 60;

        // Zone trait bonuses (multipliers: 0.5 = +50%)
        public float CommercialCashBonus { get; set; } = 0.50f;
        public float ResidentialRecruitmentBonus { get; set; } = 0.50f;
        public float IndustrialWeaponsBonus { get; set; } = 0.50f;
        public float PortCashBonus { get; set; } = 0.25f;
        public float PortWeaponsBonus { get; set; } = 0.25f;
        public float HighValueMultiplier { get; set; } = 2.0f;

        // Supply line efficiency when disconnected
        public float DisconnectedSupplyEfficiency { get; set; } = 0.5f;
    }

    public class InitializationConfig
    {
        public int StartingCash { get; set; } = 5000;
        public int StartingTroopsPerZone { get; set; } = 5;
        public int StartingZonesPerFaction { get; set; } = 3;
        public int StartingReserveTroops { get; set; } = 10;
    }

    public class PersistenceConfig
    {
        public int AutoSaveIntervalSeconds { get; set; } = 300;
        public int MaxSaveSlots { get; set; } = 10;
        public string SaveDirectoryName { get; set; } = "FactionWars";
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~GameConfigTests" -v n`
Expected: PASS (5 tests)

**Step 5: Commit**

```bash
git add src/FactionWars/Configuration/GameConfig.cs tests/FactionWars.Tests/Unit/Configuration/GameConfigTests.cs
git commit -m "feat: add GameConfig model with default values

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 3: Create ConfigLoader Service

**Files:**
- Create: `src/FactionWars/Configuration/IConfigLoader.cs`
- Create: `src/FactionWars/Configuration/ConfigLoader.cs`
- Test: `tests/FactionWars.Tests/Unit/Configuration/ConfigLoaderTests.cs`

**Step 1: Write the failing tests**

```csharp
using System.IO;
using FactionWars.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace FactionWars.Tests.Unit.Configuration
{
    public class ConfigLoaderTests
    {
        private readonly string _testDir;

        public ConfigLoaderTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "FactionWarsConfigTest_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        [Fact]
        public void Load_WhenFileDoesNotExist_CreatesDefaultConfig()
        {
            var configPath = Path.Combine(_testDir, "config.json");
            var loader = new ConfigLoader(configPath);

            var config = loader.Load();

            Assert.NotNull(config);
            Assert.True(File.Exists(configPath));
            Assert.Equal(GameConfig.Default.AI.DecisionIntervalSeconds, config.AI.DecisionIntervalSeconds);
        }

        [Fact]
        public void Load_WhenFileExists_ReadsExistingConfig()
        {
            var configPath = Path.Combine(_testDir, "existing.json");
            var customConfig = new GameConfig();
            customConfig.AI.DecisionIntervalSeconds = 120f;
            customConfig.AI.TrevorAggressiveness = 0.5f;
            File.WriteAllText(configPath, JsonConvert.SerializeObject(customConfig, Formatting.Indented));

            var loader = new ConfigLoader(configPath);
            var config = loader.Load();

            Assert.Equal(120f, config.AI.DecisionIntervalSeconds);
            Assert.Equal(0.5f, config.AI.TrevorAggressiveness);
        }

        [Fact]
        public void Load_WhenFileHasPartialConfig_MergesWithDefaults()
        {
            var configPath = Path.Combine(_testDir, "partial.json");
            // Only set AI section, leave others at defaults
            File.WriteAllText(configPath, @"{
                ""AI"": {
                    ""DecisionIntervalSeconds"": 90
                }
            }");

            var loader = new ConfigLoader(configPath);
            var config = loader.Load();

            Assert.Equal(90f, config.AI.DecisionIntervalSeconds);
            // Other AI values should be defaults
            Assert.Equal(0.3f, config.AI.MichaelAggressiveness);
            // Combat should be defaults
            Assert.Equal(1.5f, config.Combat.DefenderAdvantage);
        }

        [Fact]
        public void Load_CreatesDirectoryIfNotExists()
        {
            var nestedDir = Path.Combine(_testDir, "nested", "deep");
            var configPath = Path.Combine(nestedDir, "config.json");
            var loader = new ConfigLoader(configPath);

            var config = loader.Load();

            Assert.True(Directory.Exists(nestedDir));
            Assert.True(File.Exists(configPath));
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~ConfigLoaderTests" -v n`
Expected: FAIL - ConfigLoader not found

**Step 3: Write the interface**

```csharp
namespace FactionWars.Configuration
{
    /// <summary>
    /// Loads and manages game configuration from a JSON file.
    /// </summary>
    public interface IConfigLoader
    {
        /// <summary>
        /// Loads the configuration from disk.
        /// Creates a default config file if one doesn't exist.
        /// </summary>
        GameConfig Load();

        /// <summary>
        /// Gets the path to the configuration file.
        /// </summary>
        string ConfigPath { get; }
    }
}
```

**Step 4: Write the implementation**

```csharp
using System;
using System.IO;
using Newtonsoft.Json;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.Configuration
{
    /// <summary>
    /// Loads game configuration from a JSON file.
    /// Creates default config if file doesn't exist.
    /// </summary>
    public class ConfigLoader : IConfigLoader
    {
        private readonly string _configPath;
        private GameConfig? _cachedConfig;

        public ConfigLoader(string configPath)
        {
            _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
        }

        public string ConfigPath => _configPath;

        public GameConfig Load()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            try
            {
                EnsureDirectoryExists();

                if (File.Exists(_configPath))
                {
                    _cachedConfig = LoadFromFile();
                    FileLogger.Info($"Loaded config from {_configPath}");
                }
                else
                {
                    _cachedConfig = CreateDefaultConfig();
                    FileLogger.Info($"Created default config at {_configPath}");
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("Failed to load config, using defaults", ex);
                _cachedConfig = GameConfig.Default;
            }

            return _cachedConfig;
        }

        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private GameConfig LoadFromFile()
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonConvert.DeserializeObject<GameConfig>(json);
            return config ?? GameConfig.Default;
        }

        private GameConfig CreateDefaultConfig()
        {
            var config = GameConfig.Default;
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
            return config;
        }
    }
}
```

**Step 5: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~ConfigLoaderTests" -v n`
Expected: PASS (4 tests)

**Step 6: Commit**

```bash
git add src/FactionWars/Configuration/IConfigLoader.cs src/FactionWars/Configuration/ConfigLoader.cs tests/FactionWars.Tests/Unit/Configuration/ConfigLoaderTests.cs
git commit -m "feat: add ConfigLoader service for reading/writing config.json

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 4: Wire ConfigLoader into ServiceContainerFactory

**Files:**
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`
- Modify: `src/FactionWars/Core/Interfaces/IGameBridge.cs` (add GetScriptsDirectory)
- Modify: `src/FactionWars/ScriptHookV/GameBridge.cs` (implement GetScriptsDirectory)
- Modify: `src/FactionWars/Core/Utils/MockGameBridge.cs` (implement GetScriptsDirectory)

**Step 1: Add GetScriptsDirectory to IGameBridge**

In `src/FactionWars/Core/Interfaces/IGameBridge.cs`, add:

```csharp
/// <summary>
/// Gets the path to the GTA V scripts directory where the mod is installed.
/// </summary>
string GetScriptsDirectory();
```

**Step 2: Implement in GameBridge**

In `src/FactionWars/ScriptHookV/GameBridge.cs`, add:

```csharp
public string GetScriptsDirectory()
{
    // ScriptHookVDotNet scripts run from the scripts folder
    // Use the assembly location to find it
    var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
    return Path.GetDirectoryName(assemblyPath) ?? ".";
}
```

**Step 3: Implement in MockGameBridge**

In `src/FactionWars/Core/Utils/MockGameBridge.cs`, add:

```csharp
public string GetScriptsDirectory()
{
    return Path.GetTempPath();
}
```

**Step 4: Wire config into ServiceContainerFactory**

In `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`:

1. Add using: `using FactionWars.Configuration;`

2. Add at the start of `Create()` method, after container creation:

```csharp
// Load configuration first - other services depend on it
var configPath = Path.Combine(
    gameBridge.GetScriptsDirectory(),
    "FactionWars",
    "config.json");
var configLoader = new ConfigLoader(configPath);
var config = configLoader.Load();
container.Register<IConfigLoader>(configLoader);
container.Register(config);
```

3. Update `RegisterEconomyServices` to use config:

```csharp
private static void RegisterEconomyServices(ServiceContainer container)
{
    var config = container.Resolve<GameConfig>();

    // ... existing code ...

    // Resource tick service depends on faction service, zone service, resource modifier, and supply line service
    container.RegisterSingleton<IResourceTickService>(() =>
        new ResourceTickService(
            container.Resolve<IFactionService>(),
            container.Resolve<IZoneService>(),
            container.Resolve<IZoneTraitResourceModifier>(),
            container.Resolve<ISupplyLineService>(),
            config.Economy.ResourceTickIntervalSeconds));
}
```

**Step 5: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 6: Commit**

```bash
git add src/FactionWars/Core/Interfaces/IGameBridge.cs src/FactionWars/ScriptHookV/GameBridge.cs src/FactionWars/Core/Utils/MockGameBridge.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: wire ConfigLoader into ServiceContainerFactory

Config is loaded from scripts/FactionWars/config.json at startup.
ResourceTickService now uses config.Economy.ResourceTickIntervalSeconds.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 5: Wire AI Config Values

**Files:**
- Modify: `src/FactionWars/AI/Strategies/MichaelAIStrategy.cs`
- Modify: `src/FactionWars/AI/Strategies/TrevorAIStrategy.cs`
- Modify: `src/FactionWars/AI/Strategies/FranklinAIStrategy.cs`
- Modify: `src/FactionWars/AI/Controllers/AIController.cs`
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Update MichaelAIStrategy to accept config**

Change constructor to accept aggressiveness and risk tolerance:

```csharp
public class MichaelAIStrategy : IAIStrategy
{
    private readonly float _aggressiveness;
    private readonly float _riskTolerance;

    // Keep other constants that aren't worth externalizing
    private const float HighValueBonusMultiplier = 1.5f;
    private const float HighValueThreshold = 0.6f;

    public MichaelAIStrategy(float aggressiveness = 0.3f, float riskTolerance = 0.3f)
    {
        _aggressiveness = aggressiveness;
        _riskTolerance = riskTolerance;
    }

    public float Aggressiveness => _aggressiveness;
    public float RiskTolerance => _riskTolerance;
    // ... rest of implementation using _aggressiveness and _riskTolerance instead of constants
}
```

**Step 2: Update TrevorAIStrategy similarly**

```csharp
public TrevorAIStrategy(float aggressiveness = 0.85f, float riskTolerance = 0.8f)
```

**Step 3: Update FranklinAIStrategy similarly**

```csharp
public FranklinAIStrategy(float aggressiveness = 0.6f, float riskTolerance = 0.6f)
```

**Step 4: Update AIController to accept config values**

Add constructor parameters for intervals and costs:

```csharp
public AIController(
    IFactionService factionService,
    IZoneService zoneService,
    IBattleSimulationService battleSimulationService,
    IZoneDefenderAllocationService allocationService,
    IGameBridge gameBridge,
    IDictionary<string, IAIStrategy> strategies,
    IZoneBattleManager zoneBattleManager,
    float decisionIntervalSeconds = 60f,
    float recruitmentIntervalSeconds = 60f,
    int recruitCostPerTroop = 200,
    int attackCostPerTroop = 50,
    int maxRecruitPerCycle = 5)
```

**Step 5: Update ServiceContainerFactory.RegisterAIServices**

```csharp
private static void RegisterAIServices(ServiceContainer container)
{
    var config = container.Resolve<GameConfig>();

    // AI strategies dictionary - maps faction IDs to their strategies
    container.RegisterSingleton<IDictionary<string, IAIStrategy>>(() =>
        new Dictionary<string, IAIStrategy>
        {
            { "michael", new MichaelAIStrategy(
                config.AI.MichaelAggressiveness,
                config.AI.MichaelRiskTolerance) },
            { "trevor", new TrevorAIStrategy(
                config.AI.TrevorAggressiveness,
                config.AI.TrevorRiskTolerance) },
            { "franklin", new FranklinAIStrategy(
                config.AI.FranklinAggressiveness,
                config.AI.FranklinRiskTolerance) }
        });

    // ... other services ...

    // Register consolidated AI controller with config values
    container.RegisterSingleton<IAIController>(() => new AIController(
        container.Resolve<IFactionService>(),
        container.Resolve<IZoneService>(),
        container.Resolve<IBattleSimulationService>(),
        container.Resolve<IZoneDefenderAllocationService>(),
        container.Resolve<IGameBridge>(),
        container.Resolve<IDictionary<string, IAIStrategy>>(),
        container.Resolve<IZoneBattleManager>(),
        config.AI.DecisionIntervalSeconds,
        config.AI.RecruitmentIntervalSeconds,
        config.AI.RecruitCostPerTroop,
        config.AI.AttackCostPerTroop,
        config.AI.MaxRecruitPerCycle));
}
```

**Step 6: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 7: Commit**

```bash
git add src/FactionWars/AI/Strategies/*.cs src/FactionWars/AI/Controllers/AIController.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: wire AI config values to strategy and controller classes

AI aggressiveness, risk tolerance, and intervals now read from config.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 6: Wire Combat Config Values

**Files:**
- Modify: `src/FactionWars/Combat/Services/ZoneBattleManager.cs`
- Modify: `src/FactionWars/Combat/Services/BattleSimulationService.cs`
- Modify: `src/FactionWars/Combat/Services/TakeoverDetector.cs`
- Modify: `src/FactionWars/Combat/Services/DefenderScalingService.cs`
- Modify: `src/FactionWars/Combat/Models/SpawnBudget.cs`
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Update ZoneBattleManager constructor**

```csharp
public ZoneBattleManager(
    float defenderAdvantage = 1.5f,
    float minBattleDuration = 60f,
    float maxBattleDuration = 300f,
    float basicStrength = 1.0f,
    float mediumStrength = 1.5f,
    float heavyStrength = 2.0f)
```

**Step 2: Update BattleSimulationService constructor**

```csharp
public BattleSimulationService(
    float defenderAdvantage = 1.5f,
    float baseCasualtyRate = 0.3f,
    float basicResilience = 1.0f,
    float mediumResilience = 0.75f,
    float heavyResilience = 0.5f)
```

**Step 3: Update TakeoverDetector constructor**

```csharp
public TakeoverDetector(TakeoverThresholdConfig? config = null)
```

Already accepts config, just wire it from GameConfig.

**Step 4: Update DefenderScalingService constructor**

```csharp
public DefenderScalingService(int troopsPerPed = 5)
```

**Step 5: Update SpawnBudget defaults**

The SpawnBudget model has public const defaults. Update to accept constructor parameters.

**Step 6: Update ServiceContainerFactory.RegisterCombatServices**

```csharp
private static void RegisterCombatServices(ServiceContainer container)
{
    var config = container.Resolve<GameConfig>();

    // ... existing services ...

    // Takeover detector - uses config
    container.RegisterSingleton<ITakeoverDetector>(() =>
        new TakeoverDetector(new TakeoverThresholdConfig
        {
            AttackerVictoryThreshold = config.Combat.AttackerVictoryThreshold,
            DefenderVictoryThreshold = config.Combat.DefenderVictoryThreshold
        }));

    // Defender scaling service - scales zone troops to spawnable peds
    container.RegisterSingleton<IDefenderScalingService>(() =>
        new DefenderScalingService(config.Combat.TroopsPerSpawnedPed));

    // Zone battle manager - unified manager for battle lifecycle
    container.RegisterSingleton<IZoneBattleManager>(() =>
        new ZoneBattleManager(
            config.Combat.DefenderAdvantage,
            config.Combat.MinBattleDurationSeconds,
            config.Combat.MaxBattleDurationSeconds,
            config.Combat.BasicTroopStrength,
            config.Combat.MediumTroopStrength,
            config.Combat.HeavyTroopStrength));
}
```

**Step 7: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 8: Commit**

```bash
git add src/FactionWars/Combat/Services/*.cs src/FactionWars/Combat/Models/SpawnBudget.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: wire combat config values to battle services

Defender advantage, battle duration, troop strength, and spawn limits
now read from config.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 7: Wire Economy Config Values

**Files:**
- Modify: `src/FactionWars/Economy/Services/ZoneTraitResourceModifier.cs`
- Modify: `src/FactionWars/Territory/Services/SupplyLineService.cs`
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Update ZoneTraitResourceModifier constructor**

```csharp
public ZoneTraitResourceModifier(
    float commercialCashBonus = 0.50f,
    float residentialRecruitmentBonus = 0.50f,
    float industrialWeaponsBonus = 0.50f,
    float portCashBonus = 0.25f,
    float portWeaponsBonus = 0.25f,
    float highValueMultiplier = 2.0f)
```

**Step 2: Update SupplyLineService**

Add disconnected efficiency parameter:

```csharp
public SupplyLineService(IZoneService zoneService, float disconnectedEfficiency = 0.5f)
```

**Step 3: Update ServiceContainerFactory.RegisterEconomyServices**

```csharp
private static void RegisterEconomyServices(ServiceContainer container)
{
    var config = container.Resolve<GameConfig>();

    // Zone trait resource modifier - uses config
    container.RegisterSingleton<IZoneTraitResourceModifier>(() =>
        new ZoneTraitResourceModifier(
            config.Economy.CommercialCashBonus,
            config.Economy.ResidentialRecruitmentBonus,
            config.Economy.IndustrialWeaponsBonus,
            config.Economy.PortCashBonus,
            config.Economy.PortWeaponsBonus,
            config.Economy.HighValueMultiplier));

    // Supply line service depends on zone service
    container.RegisterSingleton<ISupplyLineService>(() =>
        new SupplyLineService(
            container.Resolve<IZoneService>(),
            config.Economy.DisconnectedSupplyEfficiency));

    // ... rest of services ...
}
```

**Step 4: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 5: Commit**

```bash
git add src/FactionWars/Economy/Services/*.cs src/FactionWars/Territory/Services/SupplyLineService.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: wire economy config values to resource services

Zone trait bonuses and supply line efficiency now read from config.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 8: Wire Persistence Config Values

**Files:**
- Modify: `src/FactionWars/Persistence/AutoSaveService.cs`
- Modify: `src/FactionWars/Persistence/SaveSlotManager.cs`
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs`

**Step 1: Update AutoSaveService**

Change default interval to accept seconds:

```csharp
public AutoSaveService(
    IPersistenceService persistenceService,
    IGameStateManager gameStateManager,
    string saveDirectory,
    int intervalSeconds = 300,
    string autoSaveFileName = "autosave.json")
```

**Step 2: Update SaveSlotManager**

```csharp
public SaveSlotManager(
    IPersistenceService persistenceService,
    string saveDirectory,
    int maxSlots = 10)
```

**Step 3: Update ServiceContainerFactory.RegisterPersistenceServices**

```csharp
private static void RegisterPersistenceServices(ServiceContainer container)
{
    var config = container.Resolve<GameConfig>();

    // ... existing services ...

    // Save slot manager - uses config
    container.RegisterSingleton<ISaveSlotManager>(() =>
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var saveDirectory = Path.Combine(documentsPath, config.Persistence.SaveDirectoryName, "Saves");
        return new SaveSlotManager(
            container.Resolve<IPersistenceService>(),
            saveDirectory,
            config.Persistence.MaxSaveSlots);
    });

    // Auto-save service - uses config
    container.RegisterSingleton<IAutoSaveService>(() =>
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var saveDirectory = Path.Combine(documentsPath, config.Persistence.SaveDirectoryName, "Saves");
        return new AutoSaveService(
            container.Resolve<IPersistenceService>(),
            container.Resolve<IGameStateManager>(),
            saveDirectory,
            config.Persistence.AutoSaveIntervalSeconds);
    });
}
```

**Step 4: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 5: Commit**

```bash
git add src/FactionWars/Persistence/*.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: wire persistence config values to save services

Auto-save interval and max slots now read from config.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 9: Wire Initialization Config Values

**Files:**
- Modify: `src/FactionWars/Factions/Services/FactionInitializer.cs`
- Modify: `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs` (if FactionInitializer is wired there)

**Step 1: Update FactionInitializer constructor**

```csharp
public FactionInitializer(
    IFactionRepository factionRepository,
    IZoneRepository zoneRepository,
    int startingCash = 5000,
    int startingTroopsPerZone = 5,
    int startingZonesPerFaction = 3,
    int startingReserveTroops = 10)
```

**Step 2: Wire in ServiceContainerFactory if applicable**

Check if FactionInitializer is registered in container. If so, update to use config values.

**Step 3: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v n`
Expected: All tests pass

**Step 4: Commit**

```bash
git add src/FactionWars/Factions/Services/FactionInitializer.cs src/FactionWars/ScriptHookV/ServiceContainerFactory.cs
git commit -m "feat: wire initialization config values to FactionInitializer

Starting cash, troops, and zones per faction now read from config.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 10: Build, Deploy, and Test In-Game

**Step 1: Build the project**

```bash
dotnet build src/FactionWars -c Debug
```

Expected: Build succeeded

**Step 2: Deploy to GTA V**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 3: Verify config.json is created on first run**

Launch GTA V, load the mod, then check:

```bash
ls "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/FactionWars/"
cat "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/FactionWars/config.json"
```

Expected: config.json exists with all sections and default values.

**Step 4: Test config changes**

1. Close GTA V
2. Edit `config.json` - change `TrevorAggressiveness` from 0.85 to 0.1
3. Launch GTA V again
4. Observe that Trevor faction is much less aggressive

**Step 5: Check logs for config loading**

```bash
cat "C:/Users/ryan7/Documents/FactionWars/Logs/FactionWars_*.log" | grep -i config
```

Expected: Log shows "Loaded config from ..." or "Created default config at ..."

**Step 6: Final commit**

```bash
git add -A
git commit -m "feat: complete config system implementation

- GameConfig model with AI, Combat, Economy, Initialization, Persistence sections
- ConfigLoader creates default config.json if missing
- All major gameplay constants now read from config
- Config file located at scripts/FactionWars/config.json

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Summary

This plan implements a complete configuration system:

1. **GameConfig model** - Strongly-typed config with sections for AI, Combat, Economy, Initialization, Persistence
2. **ConfigLoader service** - Reads config.json, creates defaults if missing
3. **ServiceContainerFactory integration** - All services receive config values via DI
4. **Runtime behavior** - Config is read once at startup, values flow to all game systems

Key config values externalized:
- AI aggressiveness and risk tolerance per faction
- AI decision/recruitment intervals and costs
- Combat defender advantage, battle duration, troop strength
- Spawning limits (peds per side, total peds)
- Economy resource rates and zone trait bonuses
- Persistence auto-save interval and max slots
- Initialization starting resources

---

Plan complete and saved to `docs/plans/2026-01-23-config-system.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

Which approach?
