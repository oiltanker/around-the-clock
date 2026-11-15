using HarmonyLib;
using JetBrains.Annotations;
using AroundTheClock.Core;
using Timberborn.PowerGeneration;

namespace AroundTheClock.Patching {
  [HarmonyPatch(typeof(WalkerPoweredGenerator))]
  [UsedImplicitly]
  internal class WalkerPoweredGeneratorPatcher {

    [HarmonyPostfix]
    [HarmonyPatch(nameof(WalkerPoweredGenerator.GetBonusMultiplier))]
    [UsedImplicitly]
    private static void GetBonusMultiplierPostfix(WalkerPoweredGenerator __instance,
                                                  ref float __result) {
      var shiftCount = __instance.GetComponent<ShiftWorkplace>()?.ShiftCount ?? 0;
      if (shiftCount > 0) {
        __result *= shiftCount;
      }
    }

  }
}
