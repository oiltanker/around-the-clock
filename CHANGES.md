# Changes vs upstream Second Shift v1.1.1.0

Mod identity: Id `eMka.SecondShift` -> `eMka.SecondShift.ThreeShifts`, Version 1.2.0.0,
Harmony id `eMka.SecondShift` -> `eMka.SecondShift.ThreeShifts`.

## SecondShift.Core

### TwoShiftsWorkplace.cs (rewritten)
- `bool TwoShiftsEnabled` -> `int ShiftCount` (0 = regular, 2 or 3).
- Save format: writes `ShiftCount` (int, key `ShiftCount`); for ShiftCount == 2 also writes
  the legacy `TwoShiftsEnabled` bool so the ORIGINAL mod can still read those saves.
  Load: prefers `ShiftCount`; falls back to legacy bool (true -> 2).
- DesiredWorkers transitions: 0->N multiplies by N; N->0 divides by old count and unassigns
  excess non-working beavers; 2<->3 rescales through the common divisor.
- Event `TwoShiftsEnableStateChanged` -> `ShiftCountChanged`; class and component key
  `TwoShiftsWorkplace` unchanged for save compatibility.

### TwoShiftsWorkingHours.cs (rewritten)
- Parity shift (`IndexOf % 2 == 1`) -> `ShiftIndex = IndexOf % ShiftCount`.
- Time gate: `DayProgress > 0.5` -> `floor(DayProgress * ShiftCount)`.
- Sleep window model added: `(Start, Length)` per scheme — vanilla/12h-A: 22–8,
  12h-B: 0–12, 8h shift i: [i*8, i*8+8).
- `GoesToSleepTogetherWith`: same ShiftCount and same ShiftIndex for both workers
  (upstream parity semantics); otherwise sleep-window overlap >= 6h (hourwise circular).
- Wake-time support: `NeedsWakeAdjust()` (ShiftCount > 0 && index >= 1) and `WakeHour`
  (12h mode: 12; 3-shift: ShiftIndex * 8).

### WorkerPositionFixer.cs, SecondShiftCoreConfigurator.cs — unchanged from repo.

## SecondShift.Patching

### SecondShiftPatcher.cs — Harmony id renamed.
### WorkplacePatcher.cs
- `MaxWorkersPrefix`: multiplier = ShiftCount (was hardcoded 2).
- `Increase/DecreaseDesiredWorkersPrefix`: step size = ShiftCount (was 2), minimum
  DesiredWorkers = ShiftCount when enabled.
### WalkerPoweredGeneratorPatcher.cs — bonus multiplier = ShiftCount (was *2).
### HoursToDawnPatcher.cs — transpiler unchanged; `Adjust` delegates to
  `NeedsWakeAdjust()/WakeHour` (was hardcoded wake at 12:00 for odd-index workers).
### WorkerWorkingHoursPatcher.cs, DwellerPatcher.cs, WorkplaceFragmentPatcher.cs — unchanged.
### DwellerHomeAssignerPatcher.cs — recovered from the shipped DLL (absent in repo);
  logic unchanged (calls `GoesToSleepTogetherWith`, which now implements the overlap
  rule). One fix on top: `AssignDweller` is static, so the prefix binds the
  `dwelling` parameter by name instead of the null `__instance`.

## SecondShift.CoreUI

### TwoShiftsWorkplaceFragment.cs (rewritten)
- UXML "Two shifts" toggle hidden; game-native `Timberborn.DropdownSystem`
  `Dropdown` (DropdownListDrawer + DropdownItemsSetter + `ShiftModeDropdownProvider`,
  loc keys `SecondShift.Mode.*`) — the unstyled Unity `DropdownField` from the
  first iteration was replaced because it ignored game styling.
- First row: localized `SecondShift.ShiftMode` label left, dropdown right
  (dropdown's internal label hidden; runtime-built dropdowns never get
  `Localize()` called on them).
- Second row: code-built shift-icon bar moved out of the UXML row into its own
  row, right-aligned; wrapper freed from the dropdown row's flex squeeze.
- All worker-count math divided by ShiftCount; views/worker filtering keyed on
  `ShiftIndex == SelectedShift` (was 2-item first/second toggle).
### TwoShiftsWorkersToggle.cs (rewritten)
- N-item `SliderToggle` (factory takes params array).
- Icons: shift 0 = bundle sun `first-shift`, 2-shift mode shift 1 = bundle moon
  `second-shift`, 3-shift mode shift 1 = `version-1.1\Assets\midday-shift.png`
  loaded at runtime (ImageConversion + Sprite.Create; moon fallback), shift 2 =
  bundle moon. Tooltip closure captures the item index (was the loop variable,
  always showing "ShowShift4").
### SecondShiftCoreUIConfigurator.cs — unchanged.

## Data
- `version-1.1\manifest.json`: new Id/Name/Version/Description.
- Localizations (enUS/plPL/ukUA): keys `SecondShift.ShiftMode`, `SecondShift.Mode.*`,
  `SecondShift.ShowShift{1,2,3}` appended; `mod_manager_manifest.json` deleted from the fork
  (duplicate ResourceId 5522813 crashes Mod Manager).
