using HarmonyLib;
using JetBrains.Annotations;
using SecondShift.Core;
using Timberborn.PowerGeneration;

namespace SecondShift.Patching {
  [HarmonyPatch(typeof(WalkerPoweredGenerator))]
  [UsedImplicitly]
  internal class WalkerPoweredGeneratorPatcher {

    [HarmonyPostfix]
    [HarmonyPatch(nameof(WalkerPoweredGenerator.GetBonusMultiplier))]
    [UsedImplicitly]
    private static void GetBonusMultiplierPostfix(WalkerPoweredGenerator __instance,
                                                  ref float __result) {
      var shiftCount = __instance.GetComponent<TwoShiftsWorkplace>()?.ShiftCount ?? 0;
      if (shiftCount > 0) {
        __result *= shiftCount;
      }
    }

  }
}
