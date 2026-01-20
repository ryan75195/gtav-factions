# Zone Defenders and UI Improvements Design

**Goal:** Spawn friendly defenders when entering own territory, improve enemy spawn positioning, add minimap blips for friendly troops, and retain menu cursor position.

---

## Feature 1: Friendly Defender Spawning

**Behavior:**
- When player enters a zone they own, allocated defenders spawn and patrol
- Defenders are NOT followers - they wander independently within the zone
- When player exits the zone, defenders despawn

**Implementation:**

New component: `FriendlyDefenderManager`
- Listens to `TerritoryManager.ZoneEntered` and `ZoneExited` events
- On friendly zone entry:
  1. Query `ZoneDefenderAllocationService` for allocated troops by tier
  2. Calculate spawn positions (30-50m spread from zone center)
  3. Spawn peds using `PedSpawningService`
  4. Set friendly relationship group
  5. Task with `TASK_WANDER_IN_AREA` around zone center
  6. Create light blue minimap blip for each
  7. Track spawned ped handles by zone ID
- On zone exit:
  1. Get tracked peds for that zone
  2. Remove blips
  3. Despawn peds
  4. Clear tracking

**Key difference from followers:**
- No ped group membership (don't follow player)
- Wander task instead of follow task
- Zone-scoped lifecycle (despawn on exit)

---

## Feature 2: Immediate Enemy Spawning

**Behavior:**
- When entering hostile territory, ALL allocated defenders spawn immediately
- Spawn positions spread 30-50m radius around zone centroid
- No wave-based spawning for player encounters

**Implementation:**

Modify `CombatManager.StartCombat()`:
1. Query `ZoneDefenderAllocationService` for enemy allocations
2. Calculate all spawn positions upfront (spread around centroid)
3. Spawn all defenders immediately (Heavy + Medium + Basic)
4. Set hostile relationship group
5. Task with `TASK_COMBAT_HATED_TARGETS_AROUND_PED`

Remove wave spawning logic from player combat flow. Keep `WaveSpawnerService` available for potential AI-vs-AI battles.

---

## Feature 3: Minimap Blips for Friendly Troops

**Blip Colors:**
- Followers (bodyguards): Yellow
- Zone defenders (patrol): Light Blue

**Implementation:**

New/extended: `PedBlipService`
- `CreateBlipForPed(pedHandle, BlipColor)` - Creates blip attached to ped
- `RemoveBlipForPed(pedHandle)` - Removes blip
- Internal tracking: `Dictionary<int, int>` (pedHandle → blipHandle)

IGameBridge additions:
- `CreateBlipForPed(pedHandle)` - Native: `ADD_BLIP_FOR_ENTITY`
- Returns blip handle for color setting

Integration points:
- `FollowerManager.RecruitFollower()` → Create yellow blip
- `FollowerManager.DismissFollower()` / death → Remove blip
- `FriendlyDefenderManager` spawn → Create light blue blip
- `FriendlyDefenderManager` despawn → Remove blip

---

## Feature 4: Menu Cursor Retention

**Behavior:**
- After purchasing troops or allocating, cursor stays on same menu item
- Allows quick repeat purchases without navigating back

**Implementation:**

`ArmyMenuController` changes:
1. Add field: `private string? _lastSelectedItemId`
2. On purchase/recruit action: Store item ID before refresh
3. Pass to `ShowMenu()` call

`IMenuProvider` / `NativeUIMenuProvider` changes:
1. Add parameter: `ShowMenu(MenuDefinition def, string? selectedItemId = null)`
2. After building menu, find item matching ID
3. Set as selected/focused item

**Affected item IDs:**
- `purchase_basic`, `purchase_medium`, `purchase_heavy`
- `recruit_basic`, `recruit_medium`, `recruit_heavy`

---

## Files to Create/Modify

**New Files:**
- `src/FactionWars/ScriptHookV/Managers/FriendlyDefenderManager.cs`
- `src/FactionWars/UI/Services/PedBlipService.cs`
- `src/FactionWars/UI/Interfaces/IPedBlipService.cs`

**Modified Files:**
- `src/FactionWars/ScriptHookV/Managers/CombatManager.cs` - Immediate spawn
- `src/FactionWars/ScriptHookV/Managers/FollowerManager.cs` - Blip integration
- `src/FactionWars/ScriptHookV/GameLoopController.cs` - Wire new managers
- `src/FactionWars/ScriptHookV/ServiceContainerFactory.cs` - Register services
- `src/FactionWars/Core/Interfaces/IGameBridge.cs` - Add CreateBlipForPed
- `src/FactionWars/ScriptHookV/GameBridge.cs` - Implement CreateBlipForPed
- `src/FactionWars/Core/Utils/MockGameBridge.cs` - Mock implementation
- `src/FactionWars/UI/Interfaces/IMenuProvider.cs` - Add selectedItemId param
- `src/FactionWars/ScriptHookV/UI/NativeUIMenuProvider.cs` - Implement selection
- `src/FactionWars/ScriptHookV/UI/ArmyMenuController.cs` - Track selection

**Test Files:**
- `tests/FactionWars.Tests/Unit/ScriptHookV/FriendlyDefenderManagerTests.cs`
- `tests/FactionWars.Tests/Unit/UI/PedBlipServiceTests.cs`
- `tests/FactionWars.Tests/Unit/ScriptHookV/ArmyMenuControllerSelectionTests.cs`
