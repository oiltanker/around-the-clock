# Status

**State: fixed (access-visibility), rebuilt, redeployed, awaiting in-game testing.** (2026-11-15)

## 2026-11-15 crash and fix

First in-game test crashed on save load: `MethodAccessException` — the mod called
internal game APIs that are public only in our publicized compile-time references
(`WorkplacePriorityToggleGroupFactory.Create` plus 33 more accesses found by the
new `tools/AccessAudit` Cecil checker).

Fix: `tools/AccessInjector` post-build step injects
`[assembly: IgnoresAccessChecksTo("Timberborn.*")]` (attribute type defined in-module)
into all three DLLs — the same mechanism used by ChooChoo, SmartPower, XRay, TimberUi,
ModdableTimberborn, etc. Their DLLs all carry the attribute; none of eMka's do
(upstream Second Shift is presumably broken on game 1.1.2.8 for the same reason).

## Deployed artifacts

`..\second-shift-3shifts_5522813_1.1.1.0\version-1.1\Scripts\`:

| File | SHA256 |
|---|---|
| SecondShift.Core.dll | 76771AE3B2E093FBAD8596DD31EFD96CE0630E670CA513A0693BF5CFB4AAB901 |
| SecondShift.CoreUI.dll | ECC619BD48C9BF96E1084A93A9CED057432C57114CF92066EDB4D5C7A387CE9A |
| SecondShift.Patching.dll | C6919F912C2DF14547E69973EAEEBBEA491022EDB703B709FE546DAE7612B170 |

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

- DropdownField is not a stock control in the entity panel USS — may look unstyled but functional.
- Sleep-window overlap assumes vanilla beavers sleep 22–6; game sleep onset is need-driven,
  so real overlap can vary by +-1h. Threshold 6h is a compromise; see CHANGES.md.
- Not yet tested in game: dropdown rendering, 3-shift handover at 8:00/16:00/24:00,
  wake-time transpiler in 3-shift mode, housing pairing churn.
