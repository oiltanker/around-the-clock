# Changes vs upstream Second Shift v1.1.1.0

Mod identity (v1.0.0.0 "Around The Clock"): Id `eMka.SecondShift` -> `Oiltanker.AroundTheClock`,
Harmony id `eMka.SecondShift` -> `Oiltanker.AroundTheClock`. Full C+D rename: namespaces,
projects, assemblies and classes renamed; **serialization keys kept verbatim**
(`TwoShiftsWorkplace` component key, `ShiftCount`, legacy `TwoShiftsEnabled`), so saves from
any version load unchanged. Package pruned of upstream-only files: `version-1.0\`,
`thumbnail.png`, `workshop_data.json` removed (original author's content/metadata).

## SecondShift.Core -> AroundTheClock.Core

### TwoShiftsWorkplace.cs -> ShiftWorkplace.cs (rewritten)
- Class `TwoShiftsWorkplace` -> `ShiftWorkplace`; `ComponentKey("TwoShiftsWorkplace")` and
  PropertyKeys `ShiftCount` / legacy `TwoShiftsEnabled` kept byte-identical.
- `bool TwoShiftsEnabled` -> `int ShiftCount` (0 = regular, 2 or 3); public property
  `TwoShiftsEnabled` -> `ShiftsEnabled`; `Enable/DisableTwoShifts` -> `Enable/DisableShifts`.
- Save format: writes `ShiftCount` (int, key `ShiftCount`); for ShiftCount == 2 also writes
  the legacy `TwoShiftsEnabled` bool so the ORIGINAL mod can still read those saves.
  Load: prefers `ShiftCount`; falls back to legacy bool (true -> 2).
- DesiredWorkers transitions: 0->N multiplies by N; N->0 divides by old count and unassigns
  excess non-working beavers; 2<->3 rescales through the common divisor.
- Event `TwoShiftsEnableStateChanged` -> `ShiftCountChanged`.

### TwoShiftsWorkingHours.cs -> WorkerShiftHours.cs (rewritten)
- Parity shift (`IndexOf % 2 == 1`) -> `ShiftIndex = IndexOf % ShiftCount`.
- Time gate: `DayProgress > 0.5` -> `floor(DayProgress * ShiftCount)`.
- Sleep window model added: `(Start, Length)` per scheme — vanilla/12h-A: 22–8,
  12h-B: 0–12, 8h shift i: [i*8, i*8+8). See STATUS.md for the behavior analysis
  (need-driven sleep reality, threshold slack, pairing outcomes).
- `GoesToSleepTogetherWith`: same ShiftCount and same ShiftIndex for both workers
  (upstream parity semantics); otherwise sleep-window overlap >= 6h (hourwise circular).
- Wake-time support: `NeedsWakeAdjust()` (ShiftCount > 0 && index >= 1) and `WakeHour`
  (12h mode: 12; 3-shift: ShiftIndex * 8). `IsSecondShiftWorker` -> `IsAfternoonShiftWorker`.

### WorkerPositionFixer.cs — logic unchanged; namespace renamed.

## SecondShift.Patching -> AroundTheClock.Patching

### SecondShiftPatcher.cs -> ShiftPatcher.cs — Harmony id renamed.
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

## SecondShift.CoreUI -> AroundTheClock.CoreUI

### TwoShiftsWorkplaceFragment.cs -> WorkplaceShiftFragment.cs (rewritten)
- UXML "Two shifts" toggle hidden (legacy `ToggleTwoShifts` handler removed); game-native
  `Timberborn.DropdownSystem` `Dropdown` (DropdownListDrawer + DropdownItemsSetter +
  `ShiftModeDropdownProvider`, loc keys `AroundTheClock.Mode.*`) — the unstyled Unity
  `DropdownField` from the first iteration was replaced because it ignored game styling.
- First row: localized `AroundTheClock.ShiftMode` label left, dropdown right
  (dropdown's internal label hidden; runtime-built dropdowns never get
  `Localize()` called on them).
- Second row: code-built shift-icon bar moved out of the UXML row into its own
  row, right-aligned; wrapper freed from the dropdown row's flex squeeze.
- All worker-count math divided by ShiftCount; views/worker filtering keyed on
  `ShiftIndex == SelectedShift` (was 2-item first/second toggle).
### TwoShiftsWorkersToggle.cs -> ShiftsBar.cs (rewritten)
- N-item `SliderToggle` (factory takes params array).
- Icons: shift 0 = bundle sun `first-shift`, 2-shift mode shift 1 = bundle moon
  `second-shift`, 3-shift mode shift 1 = `version-1.1\Assets\midday-shift.png`
  loaded at runtime (ImageConversion + Sprite.Create; moon fallback), shift 2 =
  bundle moon. Tooltip closure captures the item index (was the loop variable,
  always showing "ShowShift4"). Mods are byte-loaded, so the icon path is
  discovered by walking up from the assembly plus Documents-Mods and
  Workshop-content scans.
### SecondShiftCoreUIConfigurator.cs -> ShiftCoreUIConfigurator.cs.

## Data
- `version-1.1\manifest.json`: new Id/Name/Version/Description + fork notice.
- Localizations (enUS/plPL/ukUA): keys renamed `SecondShift.*` -> `AroundTheClock.*`;
  `SecondShift.TwoShiftsToggle` and `Name_TwoShiftsWorkplaceFragment` kept (referenced by
  the asset bundle); `mod_manager_manifest.json` deleted from the fork
  (duplicate ResourceId 5522813 crashes Mod Manager).
- Icon: `assets\first-shift-ref.png` (extracted original sprite) removed from the repo —
  original art stays out of version control; `assets\make-midday-icon.py` now extracts
  the sun from the shipped asset bundle at generation time.
