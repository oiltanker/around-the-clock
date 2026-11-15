using HarmonyLib;
using JetBrains.Annotations;
using AroundTheClock.Core;
using System.Linq;
using Timberborn.DwellingSystem;

namespace AroundTheClock.Patching {
  [HarmonyPatch(typeof(Dweller), nameof(Dweller.IsLookingForBetterHome))]
  [UsedImplicitly]
  internal class DwellerPatcher {

    [UsedImplicitly]
    public static bool Prefix(Dweller __instance, ref bool __result) {
      if (__instance.GetComponent<WorkerShiftHours>() is { } workerShiftHours
          && __instance.HasHome
          && __instance.Home.AdultSlots == 2
          && __instance.Home.FreeAdultSlots == 0) {
        var otherWorkingHours = __instance.Home.AdultDwellers
            .FirstOrDefault(d => d != __instance)?.GetComponent<WorkerShiftHours>();
        if (otherWorkingHours is not null
            && !workerShiftHours.GoesToSleepTogetherWith(otherWorkingHours)) {
          __result = true;
          return false;
        }
      }
      return true;
    }

  }
}