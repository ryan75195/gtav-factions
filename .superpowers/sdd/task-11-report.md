# Task 11 Report: AI recruits snipers as a capped minority (#39)

## Status: COMPLETE

## Files Changed

| File | Change |
|------|--------|
| `src/FactionWars/AI/Services/AIRecruitmentService.Purchasing.cs` | Added `SniperPerNDefenders = 6` const; added `BuySnipers` method; changed `BuyEliteTroops` signature to split wealth signal from spend budget |
| `src/FactionWars/AI/Services/AIRecruitmentService.cs` | Added `{ DefenderRole.Sniper, 0 }` to recruited dict; inserted `BuySnipers` at front of chain and threaded outputs into `BuyEliteTroops` |
| `tests/FactionWars.Tests/Unit/AI/Services/AIRecruitmentServiceTests.cs` | Added `Recruit_WealthyFaction_BuysCappedSnipers` and `Recruit_PoorFaction_BuysNoSnipers`; updated 2 existing tests (documented below) |

---

## How Budget/Slots Were Threaded and Wealth Signal Preserved

### New orchestration in `TryAutoRecruitMultiTier`

```csharp
int remainingBudget = BuySnipers(cash, maxTroops, recruited, out int remainingSlots);
remainingBudget = BuyEliteTroops(cash, remainingBudget, remainingSlots, recruited, out remainingSlots);
BuyStandardTroops(remainingBudget, remainingSlots, recruited);
```

`BuySnipers` runs first: when `cash >= MidWealthThreshold` it buys up to `ceil(maxTroops / 6)` snipers at $1500 each, reducing both `remainingBudget` and `remainingSlots`.

`BuyEliteTroops` received a new signature separating the wealth signal from the spend budget:
```csharp
private int BuyEliteTroops(int wealthSignal, int startBudget, int maxTroops, ...)
```
The original `cash` is passed as `wealthSignal` so `GetEliteCountForWealth` always keys off the faction's real wealth (not the post-sniper budget). `startBudget` carries the reduced budget from snipers so spending is accurate.

### Poor-faction invariant

When `cash < MidWealthThreshold`, `BuySnipers` returns immediately with `remainingBudget = cash` and `remainingSlots = maxTroops` unchanged. The `BuyEliteTroops` and `BuyStandardTroops` calls then receive the same inputs they received before this task — byte-identical path.

---

## Test Evidence

### Failing-first (before implementation)

Command: `dotnet test tests/FactionWars.Tests/FactionWars.Tests.csproj --filter "FullyQualifiedName~AIRecruitmentServiceTests" --nologo`

```
Failed: 1, Passed: 29, Total: 30
[FAIL] Recruit_WealthyFaction_BuysCappedSnipers
  Expected invocation on mock once, but was 0 times:
  AddReserveTroops("test", DefenderRole.Sniper, c => c >= 1 && c <= 2)
```

`Recruit_PoorFaction_BuysNoSnipers` passed trivially (no snipers are bought by any path in unmodified code — correct negative case).

### After implementation

```
dotnet test --filter "FullyQualifiedName~AIRecruitmentServiceTests"
Passed: 30, Total: 30
```

### Full unit suite

```
dotnet test --filter "FullyQualifiedName~FactionWars.Tests.Unit"
Passed: 3571, Total: 3571, Duration: 14s
```

0 failures, 0 skipped.

---

## Existing Test Expectation Changes (with justification)

Two tests had their expected values updated because sniper-first ordering is the intended new behavior. Snipers consume slots before elites and standard troops, so fewer standard troops fill the remainder. The elite count is preserved in both cases (wealth signal passes original cash).

### `TryAutoRecruit_15kTo30k_Buys1Elite_AndUses40_30_20Distribution` ($20,000, 10 troops)

| Role | Old | New |
|------|-----|-----|
| Sniper | (not asserted) | **2** (new assertion) |
| Rocketeer | 1 | 1 (unchanged) |
| Grunt | 4 | 4 (unchanged) |
| Gunner | **3** | **2** |
| Rifleman | **2** | **1** |
| SpendCash | **$6,300** | **$7,800** |

Derivation: 2 snipers ($3,000) → 8 remaining slots. 1 Rocketeer → 7 standard slots. 40/30/20 of 7 → round(2.8)=3, round(2.1)=2, round(1.4)=1 → total 6 < 7, +1 basic → 4/2/1.

### `TryAutoRecruit_Above30k_Buys2Elite_AndUses20_30_40Distribution` ($40,000, 10 troops)

| Role | Old | New |
|------|-----|-----|
| Sniper | (not asserted) | **2** (new assertion) |
| Rocketeer | 2 | 2 (unchanged) |
| Grunt | **3** | **2** |
| Gunner | 2 | 2 (unchanged) |
| Rifleman | **3** | **2** |
| SpendCash | **$8,600** | **$10,400** |

Derivation: 2 snipers ($3,000) → 8 remaining slots. 2 Rocketeers ($4,000) → 6 standard slots. 20/30/40 of 6 → round(1.2)=1, round(1.8)=2, round(2.4)=2 → total 5 < 6, +1 basic → 2/2/2.

These are not regressions — they are the correct behavior of the sniper-first purchase order.

---

## Deviations from Brief

- The brief writes `RoleService` as the field name; the actual field is `TierService`. Used `TierService` throughout.
- `BuyEliteTroops` required a signature change (added `wealthSignal` / `startBudget` split as permitted by the brief) to satisfy the wealth-signal invariant. The method remains private and within 40 effective lines.
