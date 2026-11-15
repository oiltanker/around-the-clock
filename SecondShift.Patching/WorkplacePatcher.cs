using HarmonyLib;
using JetBrains.Annotations;
using SecondShift.Core;
using System;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;

namespace SecondShift.Patching {
  [HarmonyPatch(typeof(Workplace))]
  [UsedImplicitly]
  internal class WorkplacePatcher {

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Workplace.MaxWorkers), MethodType.Getter)]
    [UsedImplicitly]
    public static bool MaxWorkersPrefix(Workplace __instance, ref int __result) {
      var shiftCount = __instance.GetComponent<TwoShiftsWorkplace>().ShiftCount;
      __result = shiftCount > 0
          ? __instance._workplaceSpec.MaxWorkers * shiftCount
          : __instance._workplaceSpec.MaxWorkers;
      return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Workplace.Load))]
    [UsedImplicitly]
    public static bool LoadPrefix(Workplace __instance, IEntityLoader entityLoader) {
      if (entityLoader.TryGetComponent(Workplace.WorkplaceKey, out var workplace)) {
        var twoShiftsWorkplace = __instance.GetComponent<TwoShiftsWorkplace>();
        var desiredWorkers = workplace.Get(Workplace.DesiredWorkersKey);
        if (twoShiftsWorkplace.WasLoaded) {
          __instance.SetDesiredWorkers(Math.Min(desiredWorkers, __instance.MaxWorkers));
        } else {
          twoShiftsWorkplace.SetDesiredWorkersToLoad(desiredWorkers);
        }
      }
      return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Workplace.IncreaseDesiredWorkers))]
    [UsedImplicitly]
    public static bool IncreaseDesiredWorkersPrefix(Workplace __instance) {
      var steps = __instance.GetComponent<TwoShiftsWorkplace>().ShiftCount;
      if (steps < 1) {
        steps = 1;
      }
      for (var i = 0; i < steps; i++) {
        IncreaseDesiredWorkers(__instance);
      }
      return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Workplace.DecreaseDesiredWorkers))]
    [UsedImplicitly]
    public static bool DecreaseDesiredWorkersPrefix(Workplace __instance) {
      var shiftCount = __instance.GetComponent<TwoShiftsWorkplace>().ShiftCount;
      var minDesired = shiftCount > 0 ? shiftCount : 1;
      if (__instance.DesiredWorkers > minDesired) {
        var steps = Math.Min(shiftCount, __instance.DesiredWorkers - minDesired);
        for (var i = 0; i < steps; i++) {
          DecreaseDesiredWorkers(__instance);
        }
      }
      return false;
    }

    private static void IncreaseDesiredWorkers(Workplace workplace) {
      if (workplace.DesiredWorkers < workplace.MaxWorkers) {
        workplace.SetDesiredWorkers(workplace.DesiredWorkers + 1);
      }
    }

    private static void DecreaseDesiredWorkers(Workplace workplace) {
      if (workplace.DesiredWorkers > 1) {
        workplace.SetDesiredWorkers(workplace.DesiredWorkers - 1);
      }
    }

  }
}
