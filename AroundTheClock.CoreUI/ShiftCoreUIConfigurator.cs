using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace AroundTheClock.CoreUI {
  [Context("Game")]
  internal class ShiftCoreUIConfigurator : Configurator {

    protected override void Configure() {
      Bind<WorkplaceShiftFragment>().AsSingleton();
      Bind<ShiftsBar>().AsTransient();

      MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
    }

    private class EntityPanelModuleProvider : IProvider<EntityPanelModule> {

      private readonly WorkplaceShiftFragment _shiftWorkplaceFragment;

      public EntityPanelModuleProvider(WorkplaceShiftFragment shiftWorkplaceFragment) {
        _shiftWorkplaceFragment = shiftWorkplaceFragment;
      }

      public EntityPanelModule Get() {
        var builder = new EntityPanelModule.Builder();
        builder.AddTopFragment(_shiftWorkplaceFragment);
        return builder.Build();
      }

    }

  }
}