# Zones JSON Loader Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make zones.json actually load zone definitions at runtime, allowing map customization without recompiling.

**Architecture:** Update `ZoneDataLoader` to check for `zones.json` in the scripts/FactionWars folder. If it exists, load zones from JSON; otherwise use hardcoded defaults. The JSON format supports traits as arrays and initial faction ownership. Adjacencies are computed based on zone proximity or defined in JSON.

**Tech Stack:** C# .NET 4.8, Newtonsoft.Json, ScriptHookV

---

## Task 1: Update ZoneDto to Match JSON Format

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs:252-262`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Data/ZoneDataLoaderTests.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void LoadFromJson_WithArrayTraits_ShouldParseCorrectly()
{
    // Arrange
    var zoneRepository = new InMemoryZoneRepository();
    var loader = new ZoneDataLoader(zoneRepository);
    var json = @"[
        {
            ""id"": ""test_zone"",
            ""name"": ""Test Zone"",
            ""centerX"": 100.0,
            ""centerY"": 200.0,
            ""centerZ"": 30.0,
            ""radius"": 150.0,
            ""strategicValue"": 5,
            ""traits"": [""Commercial"", ""HighValue""],
            ""initialOwner"": ""michael""
        }
    ]";

    // Act
    loader.LoadFromJson(json);

    // Assert
    var zone = zoneRepository.GetById("test_zone");
    Assert.NotNull(zone);
    Assert.True(zone.Traits.HasFlag(ZoneTrait.Commercial));
    Assert.True(zone.Traits.HasFlag(ZoneTrait.HighValue));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~LoadFromJson_WithArrayTraits" -v n`
Expected: FAIL (traits not parsed correctly from array)

**Step 3: Update ZoneDto to support both formats**

In `ZoneDataLoader.cs`, update the ZoneDto class:

```csharp
private class ZoneDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float CenterZ { get; set; }
    public float Radius { get; set; } = 150f;
    public int StrategicValue { get; set; } = 1;

    // Support both string (comma-separated) and array formats
    [JsonProperty("traits")]
    public object? TraitsRaw { get; set; }

    public string? InitialOwner { get; set; }

    // Adjacent zone IDs (optional - if not provided, computed by proximity)
    public List<string>? AdjacentZones { get; set; }
}
```

**Step 4: Update ParseTraits to handle array format**

```csharp
private static ZoneTrait ParseTraits(object? traitsRaw)
{
    if (traitsRaw == null)
        return ZoneTrait.None;

    ZoneTrait result = ZoneTrait.None;

    // Handle JArray (from JSON array)
    if (traitsRaw is Newtonsoft.Json.Linq.JArray jArray)
    {
        foreach (var item in jArray)
        {
            var traitStr = item.ToString().Trim();
            if (Enum.TryParse<ZoneTrait>(traitStr, true, out var trait))
            {
                result |= trait;
            }
        }
        return result;
    }

    // Handle string (comma-separated)
    var traitsString = traitsRaw.ToString();
    if (string.IsNullOrWhiteSpace(traitsString))
        return ZoneTrait.None;

    var parts = traitsString.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var part in parts)
    {
        if (Enum.TryParse<ZoneTrait>(part.Trim(), true, out var trait))
        {
            result |= trait;
        }
    }
    return result;
}
```

**Step 5: Update CreateZoneFromDto**

```csharp
private static Zone CreateZoneFromDto(ZoneDto dto)
{
    var center = new Vector3(dto.CenterX, dto.CenterY, dto.CenterZ);
    var zone = new Zone(dto.Id, dto.Name, center, dto.Radius, dto.StrategicValue);
    zone.Traits = ParseTraits(dto.TraitsRaw);
    return zone;
}
```

**Step 6: Run test to verify it passes**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~LoadFromJson_WithArrayTraits" -v n`
Expected: PASS

**Step 7: Commit**

```bash
git add src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs tests/FactionWars.Tests/Unit/ScriptHookV/Data/ZoneDataLoaderTests.cs
git commit -m "feat: support array format for zone traits in JSON

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 2: Add LoadFromFile Method

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Data/ZoneDataLoaderTests.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void LoadFromFile_WhenFileExists_ShouldLoadZones()
{
    // Arrange
    var zoneRepository = new InMemoryZoneRepository();
    var loader = new ZoneDataLoader(zoneRepository);
    var tempDir = Path.Combine(Path.GetTempPath(), "ZoneDataLoaderTest_" + Guid.NewGuid());
    Directory.CreateDirectory(tempDir);
    var zonesFile = Path.Combine(tempDir, "zones.json");

    var json = @"{
        ""zones"": [
            {
                ""id"": ""test1"",
                ""name"": ""Test Zone 1"",
                ""centerX"": 100.0,
                ""centerY"": 200.0,
                ""centerZ"": 30.0,
                ""radius"": 150.0,
                ""strategicValue"": 5,
                ""traits"": [""Commercial""]
            }
        ]
    }";
    File.WriteAllText(zonesFile, json);

    try
    {
        // Act
        bool loaded = loader.LoadFromFile(zonesFile);

        // Assert
        Assert.True(loaded);
        Assert.Equal(1, zoneRepository.Count);
        Assert.NotNull(zoneRepository.GetById("test1"));
    }
    finally
    {
        Directory.Delete(tempDir, true);
    }
}

[Fact]
public void LoadFromFile_WhenFileDoesNotExist_ShouldReturnFalse()
{
    // Arrange
    var zoneRepository = new InMemoryZoneRepository();
    var loader = new ZoneDataLoader(zoneRepository);

    // Act
    bool loaded = loader.LoadFromFile("/nonexistent/path/zones.json");

    // Assert
    Assert.False(loaded);
    Assert.Equal(0, zoneRepository.Count);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~LoadFromFile" -v n`
Expected: FAIL (method not found)

**Step 3: Implement LoadFromFile**

Add to `ZoneDataLoader.cs`:

```csharp
/// <summary>
/// Loads zones from a JSON file.
/// </summary>
/// <param name="filePath">Path to the zones.json file.</param>
/// <returns>True if file was loaded, false if file doesn't exist.</returns>
public bool LoadFromFile(string filePath)
{
    if (!File.Exists(filePath))
    {
        FileLogger.Info($"Zones file not found: {filePath}");
        return false;
    }

    try
    {
        var json = File.ReadAllText(filePath);
        var wrapper = JsonConvert.DeserializeObject<ZonesFileWrapper>(json);

        if (wrapper?.Zones == null || wrapper.Zones.Count == 0)
        {
            FileLogger.Warn($"Zones file is empty or invalid: {filePath}");
            return false;
        }

        foreach (var dto in wrapper.Zones)
        {
            var zone = CreateZoneFromDto(dto);
            _zoneRepository.Add(zone);
        }

        FileLogger.Info($"Loaded {wrapper.Zones.Count} zones from {filePath}");
        return true;
    }
    catch (Exception ex)
    {
        FileLogger.Error($"Failed to load zones from {filePath}", ex);
        return false;
    }
}

/// <summary>
/// Wrapper for the zones.json file format.
/// </summary>
private class ZonesFileWrapper
{
    public List<ZoneDto> Zones { get; set; } = new List<ZoneDto>();
}
```

Add using at top:
```csharp
using System.IO;
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~LoadFromFile" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs tests/FactionWars.Tests/Unit/ScriptHookV/Data/ZoneDataLoaderTests.cs
git commit -m "feat: add LoadFromFile method to ZoneDataLoader

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 3: Add LoadZonesWithFallback Method

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs`
- Test: `tests/FactionWars.Tests/Unit/ScriptHookV/Data/ZoneDataLoaderTests.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void LoadZonesWithFallback_WhenFileExists_ShouldLoadFromFile()
{
    // Arrange
    var zoneRepository = new InMemoryZoneRepository();
    var loader = new ZoneDataLoader(zoneRepository);
    var tempDir = Path.Combine(Path.GetTempPath(), "ZoneDataLoaderTest_" + Guid.NewGuid());
    Directory.CreateDirectory(tempDir);
    var zonesFile = Path.Combine(tempDir, "zones.json");

    var json = @"{""zones"": [{""id"": ""custom"", ""name"": ""Custom Zone"", ""centerX"": 0, ""centerY"": 0, ""centerZ"": 0, ""radius"": 100, ""strategicValue"": 1, ""traits"": []}]}";
    File.WriteAllText(zonesFile, json);

    try
    {
        // Act
        loader.LoadZonesWithFallback(zonesFile);

        // Assert
        Assert.Equal(1, zoneRepository.Count);
        Assert.NotNull(zoneRepository.GetById("custom"));
        Assert.Null(zoneRepository.GetById("downtown")); // Default zone should NOT exist
    }
    finally
    {
        Directory.Delete(tempDir, true);
    }
}

[Fact]
public void LoadZonesWithFallback_WhenFileDoesNotExist_ShouldLoadDefaults()
{
    // Arrange
    var zoneRepository = new InMemoryZoneRepository();
    var loader = new ZoneDataLoader(zoneRepository);

    // Act
    loader.LoadZonesWithFallback("/nonexistent/zones.json");

    // Assert
    Assert.True(zoneRepository.Count > 0);
    Assert.NotNull(zoneRepository.GetById("downtown")); // Default zone should exist
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~LoadZonesWithFallback" -v n`
Expected: FAIL (method not found)

**Step 3: Implement LoadZonesWithFallback**

Add to `ZoneDataLoader.cs`:

```csharp
/// <summary>
/// Loads zones from file if it exists, otherwise loads default hardcoded zones.
/// Also sets up zone adjacencies after loading.
/// </summary>
/// <param name="zonesFilePath">Path to the zones.json file.</param>
public void LoadZonesWithFallback(string zonesFilePath)
{
    if (_zoneRepository.Count > 0)
    {
        throw new InvalidOperationException("Zones have already been loaded.");
    }

    bool loadedFromFile = LoadFromFile(zonesFilePath);

    if (!loadedFromFile)
    {
        FileLogger.Info("Loading default zones (no zones.json found)");
        var zones = CreateDefaultZones().ToList();
        foreach (var zone in zones)
        {
            _zoneRepository.Add(zone);
        }
    }

    // Set up zone adjacencies after all zones are loaded
    FileLogger.AI("Setting up zone adjacencies...");
    SetupZoneAdjacencies(_zoneRepository);

    // Log summary
    int totalAdjacencies = 0;
    foreach (var zone in _zoneRepository.GetAll())
    {
        totalAdjacencies += zone.AdjacentZoneIds.Count;
    }
    FileLogger.AI($"Zone loading complete: {_zoneRepository.Count} zones, {totalAdjacencies} total adjacency links");
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/FactionWars.Tests --filter "FullyQualifiedName~LoadZonesWithFallback" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs tests/FactionWars.Tests/Unit/ScriptHookV/Data/ZoneDataLoaderTests.cs
git commit -m "feat: add LoadZonesWithFallback for file-or-defaults loading

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 4: Wire Up Zone Loading in GameLoopController

**Files:**
- Modify: `src/FactionWars/ScriptHookV/GameLoopController.cs:511`

**Step 1: Read current implementation**

Current code at line 511:
```csharp
_zoneDataLoader.LoadDefaultZones();
```

**Step 2: Update to use LoadZonesWithFallback**

Change to:
```csharp
// Load zones from file if exists, otherwise use defaults
var scriptsDir = _gameBridge.GetScriptsDirectory();
var zonesFilePath = Path.Combine(scriptsDir, "FactionWars", "zones.json");
_zoneDataLoader.LoadZonesWithFallback(zonesFilePath);
```

Add using at top if not present:
```csharp
using System.IO;
```

**Step 3: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v q`
Expected: All tests pass

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/GameLoopController.cs
git commit -m "feat: wire zone loading to use zones.json file

Loads from scripts/FactionWars/zones.json if exists, otherwise uses defaults.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 5: Update Adjacency Setup for Custom Zones

**Files:**
- Modify: `src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs:177-223`

The current `SetupZoneAdjacencies` method has hardcoded adjacency relationships. For custom zones from JSON, we need a fallback that computes adjacencies based on proximity.

**Step 1: Add proximity-based adjacency computation**

Add to `ZoneDataLoader.cs`:

```csharp
/// <summary>
/// Computes zone adjacencies based on proximity (overlapping or close zones).
/// Used when loading custom zones from JSON that don't have explicit adjacencies.
/// </summary>
/// <param name="zoneRepository">The zone repository containing loaded zones.</param>
/// <param name="proximityMultiplier">Zones are adjacent if distance < (radius1 + radius2) * multiplier. Default 1.2.</param>
public static void ComputeAdjacenciesByProximity(IZoneRepository zoneRepository, float proximityMultiplier = 1.2f)
{
    var zones = zoneRepository.GetAll().ToList();

    for (int i = 0; i < zones.Count; i++)
    {
        for (int j = i + 1; j < zones.Count; j++)
        {
            var zone1 = zones[i];
            var zone2 = zones[j];

            float distance = CalculateDistance(zone1.Center, zone2.Center);
            float threshold = (zone1.Radius + zone2.Radius) * proximityMultiplier;

            if (distance < threshold)
            {
                if (!zone1.AdjacentZoneIds.Contains(zone2.Id))
                    zone1.AdjacentZoneIds.Add(zone2.Id);
                if (!zone2.AdjacentZoneIds.Contains(zone1.Id))
                    zone2.AdjacentZoneIds.Add(zone1.Id);
            }
        }
    }
}

private static float CalculateDistance(Vector3 a, Vector3 b)
{
    float dx = a.X - b.X;
    float dy = a.Y - b.Y;
    // Ignore Z for 2D map distance
    return (float)Math.Sqrt(dx * dx + dy * dy);
}
```

**Step 2: Update SetupZoneAdjacencies to handle custom zones**

```csharp
/// <summary>
/// Sets up adjacency relationships between zones.
/// Uses hardcoded adjacencies for default zones, proximity-based for custom zones.
/// </summary>
public static void SetupZoneAdjacencies(IZoneRepository zoneRepository)
{
    // Check if we have the default zones by looking for a known zone
    var downtown = zoneRepository.GetById("downtown");
    var vinewood = zoneRepository.GetById("vinewood");

    // If we have the default zone IDs, use hardcoded adjacencies
    if (downtown != null && vinewood != null && zoneRepository.Count >= 30)
    {
        SetupDefaultAdjacencies(zoneRepository);
    }
    else
    {
        // Custom zones - compute adjacencies by proximity
        FileLogger.Info("Using proximity-based adjacency computation for custom zones");
        ComputeAdjacenciesByProximity(zoneRepository);
    }
}

/// <summary>
/// Sets up hardcoded adjacencies for the default 31 zones.
/// </summary>
private static void SetupDefaultAdjacencies(IZoneRepository zoneRepository)
{
    // [Keep existing hardcoded adjacency code here - lines 180-222]
}
```

**Step 3: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v q`
Expected: All tests pass

**Step 4: Commit**

```bash
git add src/FactionWars/ScriptHookV/Data/ZoneDataLoader.cs
git commit -m "feat: add proximity-based adjacency computation for custom zones

Falls back to hardcoded adjacencies for default zone set.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 6: Build, Deploy, and Test

**Step 1: Run all tests**

Run: `dotnet test tests/FactionWars.Tests -v q`
Expected: All tests pass

**Step 2: Build the project**

Run: `dotnet build src/FactionWars -c Debug`
Expected: Build succeeded

**Step 3: Deploy to GTA V**

```bash
cp "C:/Users/ryan7/programming/gtav-factions/src/FactionWars/bin/Debug/net48/FactionWars.dll" "E:/SteamLibrary/steamapps/common/Grand Theft Auto V/scripts/"
```

**Step 4: Test in-game**

1. Launch GTA V with the mod
2. Check logs at `C:\Users\ryan7\Documents\FactionWars\Logs\` for:
   - "Loaded X zones from .../zones.json"
   - "Using proximity-based adjacency computation"
3. Verify zones load correctly from the JSON file
4. Test modifying zones.json:
   - Change a zone's radius or position
   - Restart game
   - Verify changes took effect

**Step 5: Commit final state**

```bash
git add -A
git commit -m "feat: complete zones.json loading implementation

- Zones load from scripts/FactionWars/zones.json if exists
- Falls back to hardcoded defaults if file missing
- Supports traits as arrays and initialOwner field
- Proximity-based adjacencies for custom zone sets

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Summary

This plan implements zone loading from `zones.json`:

| Task | Description |
|------|-------------|
| 1 | Update ZoneDto to support array traits format |
| 2 | Add LoadFromFile method |
| 3 | Add LoadZonesWithFallback (file or defaults) |
| 4 | Wire up in GameLoopController |
| 5 | Add proximity-based adjacency computation |
| 6 | Build, deploy, and test |

After implementation, users can:
- Modify zone positions, sizes, and properties in `zones.json`
- Add/remove zones without recompiling
- Create entirely custom map layouts
