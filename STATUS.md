# Status

**State: built, deployed, awaiting in-game testing.** (2026-11-15)

## Deployed artifacts

`..\second-shift-3shifts_5522813_1.1.1.0\version-1.1\Scripts\`:

| File | SHA256 |
|---|---|
| SecondShift.Core.dll | 513A3863621AE3193D755619F9D382B8DD56E85C63901A2D40BF219D14156C92 |
| SecondShift.CoreUI.dll | 9CE8F529B4196093DE3D6FDD7C27AED03E9B328D15E3D49381B14E34594D0018 |
| SecondShift.Patching.dll | FF85B5250A7751B0D41FC4B930A74CD2D810640DD40BA72E4E4590A42D3F7E19 |

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
