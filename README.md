# Second Shift (Three Shifts) — source tree

Fork of eMka's **Second Shift** Timberborn mod (mod.io resource 5522813), extended with a
three-shift mode (3 × 8h, 3× workers). This directory holds only the build; the installable
mod lives at `..\second-shift-3shifts_5522813_1.1.1.0\` (sibling folder).

## Layout

```
build.ps1              build orchestration (publicize -> compile -> optional deploy)
SecondShift.Core/      game logic components (TwoShiftsWorkplace, TwoShiftsWorkingHours, WorkerPositionFixer)
SecondShift.Patching/  Harmony patches
SecondShift.CoreUI/    entity-panel UI (dropdown + shift viewer)
tools/Publicizer/      Cecil tool that makes game assemblies fully public for compile-time use
lib_public/            publicized copies of game assemblies (generated, not committed)
```

## Provenance of sources

- Base: https://github.com/eMkaQQ/timberborn-modding , commit `3a74964` ("Riverborne 1.1.2.4"),
  `Assets/Mods/SecondShift/Scripts/` (12 files, taken verbatim or modified — see CHANGES.md).
- `SecondShift.Patching/DwellerHomeAssignerPatcher.cs` is **not in the repo snapshot**; it was
  recovered from the shipped v1.1.1.0 `SecondShift.Patching.dll` (decompiled with ILSpy 9.1).
  Everything else in the shipped DLLs matched the repo sources semantically.

## Build

Requires .NET SDK 8 (per-user install at `%USERPROFILE%\dotnet-sdk` suffices; build.ps1 sets
`DOTNET_ROOT`). Game must be at the default Steam path (`build.ps1` `$gameDir`).

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1            # compile only
powershell -ExecutionPolicy Bypass -File build.ps1 -Deploy    # compile + install DLLs into the mod folder
```

What `build.ps1` does:
1. Builds `tools/Publicizer` and runs it on 10 game assemblies into `lib_public\`
   (compile-time only; private-member access matches how the upstream author used
   Unity-publicized assemblies — the mod itself ships no game DLLs).
2. Builds `SecondShift.Core`, `SecondShift.Patching`, `SecondShift.CoreUI` (netstandard2.1).
3. With `-Deploy`, copies the three DLLs to
   `..\second-shift-3shifts_5522813_1.1.1.0\version-1.1\Scripts\`.

## Constraints

- This build tree must stay OUTSIDE the mod folder: the game loads every `*.dll` recursively
  under a mod directory, and `bin/`, `obj/`, `lib_public/` are full of DLLs that would crash it.
- Enable either the original Second Shift or this fork, never both (duplicate Harmony patches
  on `Workplace` / `WorkerWorkingHours`).
- Asset bundles (UXML/USS/sprites) are reused from the shipped mod unchanged — Unity-built
  binaries are not rebuilt; all UI additions are constructed in C# code.

See CHANGES.md for the code delta and STATUS.md for the current deployment state.
