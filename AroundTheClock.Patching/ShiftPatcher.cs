using HarmonyLib;
using JetBrains.Annotations;
using Timberborn.ModManagerScene;

namespace AroundTheClock.Patching {
  [UsedImplicitly]
  internal class ShiftPatcher : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
      new Harmony("Oiltanker.AroundTheClock").PatchAll();
    }

  }
}
