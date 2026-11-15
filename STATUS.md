# Status

**State: working in game (loads, unpause stable, 3-shift UI live).** (2026-11-15)

## History of crashes and fixes (all resolved)

1. **MethodAccessException on save load** — mod compiled against publicized refs called
   internal game APIs (34 accesses, found by `tools/AccessAudit`). Fix attempt 1:
   `tools/AccessInjector` injects `[assembly: IgnoresAccessChecksTo]` — **works in .NET
   but Unity Mono 1.1.2.8 ignores the attribute** (verified: crash persisted with
   attribute attached). Fix (final): all flagged sites rewritten with cached reflection
   (`SecondShift.Core/Access.cs`); audit now reports only `BaseComponent::.ctor`
   family-access false positives.
2. **ReflectionTypeLoadException (System.Private.CoreLib 8.0)** — injector leaked
   .NET 8 corelib refs; fixed by scoping injected refs to the module's core library
   (netstandard).
3. **NullReferenceException on unpause** — `DwellerHomeAssigner.AssignDweller` is
   *static*; prefix used `__instance` (null for static targets). Fixed: bind the
   `dwelling` parameter by name.

## UI (2026-11-15)

- Shift-mode selector: game-native `Timberborn.DropdownSystem` `Dropdown`
  (DropdownListDrawer + DropdownItemsSetter + `ShiftModeDropdownProvider`), label
  `SecondShift.ShiftMode`; replaces the unstyled Unity DropdownField.
- Shift viewer tooltips: fixed loop-variable closure bug (was always `ShowShift4`);
  names localized as "Show morning/afternoon/night shift" (enUS/plPL/ukUA;
  ukUA dropdown labels translated too).
- Midday icon: `version-1.1\Assets\midday-shift.png` (70% original sun top-left +
  cloud with rounded solid base, 4x supersampled; generator script uses
  `assets\first-shift-ref.png` extracted from the mod's own asset bundle).
  Loaded at runtime via ImageConversion + Sprite.Create (pivot 0.5, PPU 100);
  moon fallback if missing; path discovered by walking up from the byte-loaded
  assembly plus Documents-Mods and Workshop-content scans. Shift 3 reuses the
  bundle's `second-shift` moon.
- Layout: first row = localized "Shift mode" label left, dropdown right
  (dropdown's internal label hidden — runtime-built dropdowns never get
  `Localize()`); second row = 3-icon shift bar, right-aligned.

## Deployed artifacts

`..\second-shift-3shifts_5522813_1.1.1.0\version-1.1\Scripts\`:

| File | SHA256 (build 2026-11-15 18:52) |
|---|---|
| SecondShift.Core.dll | 27435895C6D934DACD31422BDAA3447E22A9763D9C8105D924CBF6552B133D44 |
| SecondShift.CoreUI.dll | 50F691FDF91B7A54C3DD711BD949ADAEB4C44C944B3ECBEB3F7DE61C2ABB7DD7 |
| SecondShift.Patching.dll | 77D26070158D63143ABEA923CE5ABF441A4521F8479E05248149739042D3DBBE |

Note: DLL hashes change on every rebuild (module GUID/MVID), so treat them as
per-build fingerprints, not stable identities.

`version-1.1\Assets\midday-shift.png` ships the midday icon (runtime-loaded, not hashed).

Build: all three assemblies compile with 0 errors (warnings: CS2002 duplicate AssemblyAttributes
in obj, CS8632 nullable annotation in recovered DwellerHomeAssignerPatcher).

Verified:
- Assembly references of deployed DLLs = game-shipped assemblies + 0Harmony + sibling mod DLLs
  (no publicized DLLs shipped).
- Decompiled deployed Core confirms `ShiftCount`/`ShiftCountKey` and Harmony id `eMka.SecondShift.ThreeShifts`.
- Mod folder contains no bin/obj (build tree lives outside it).

## Runtime semantics

- Modes per workplace: Regular (ShiftCount 0) / Two shifts (12h+12h, upstream behavior) /
  Three shifts (8h+8h+8h, workers split by position index % 3).
- 3-shift boundaries: 0–8 / 8–16 / 16–24; MaxWorkers = spec × ShiftCount.
- Shifts 1 and 2 workers get wake-time adjustment (wake 8:00 / 16:00); shift 0 keeps vanilla night sleep.
- Walker generator bonus × ShiftCount.
- Housing: same-scheme pairs require same shift index; cross-scheme/vanilla pairing requires
  sleep-window overlap >= 6h (vanilla window 22–8).

## Compatibility

- Conflicts: original Second Shift (both Harmony-patch `Workplace`, `WorkerWorkingHours`),
  Workplace Schedules family (patches `WorkerWorkingHours.AreWorkingHours`). Enable exactly one.
- Saves: old Second Shift saves load (legacy `TwoShiftsEnabled` -> 2 shifts). Saves made by this
  fork in 2-shift mode remain loadable by the original mod; 3-shift workplaces degrade to
  no-shift in the original mod (unknown `ShiftCount` key ignored, DesiredWorkers halved by its
  `Workplace.Load` prefix — MaxWorkers falls back to spec value).

## Open items / known risks

- Sleep-window overlap assumes vanilla beavers sleep 22–6; game sleep onset is need-driven,
  so real overlap can vary by +-1h. Threshold 6h is a compromise; see CHANGES.md.
- Verified in game: mod load, save load, unpause (housing patch), native dropdown,
  3-shift selection, shift icons/tooltips. Longer sessions may still surface issues
  (3-shift handover at 8:00/16:00, wake-time transpiler under load, housing churn).
