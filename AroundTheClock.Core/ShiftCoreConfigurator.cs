using Bindito.Core;
using Timberborn.TemplateInstantiation;
using Timberborn.WorkSystem;

namespace AroundTheClock.Core {
  [Context("Game")]
  internal class ShiftCoreConfigurator : Configurator {

    protected override void Configure() {
      Bind<WorkerShiftHours>().AsTransient();
      Bind<ShiftWorkplace>().AsTransient();
      Bind<WorkerPositionFixer>().AsTransient();

      MultiBind<TemplateModule>()
          .ToProvider(ProvideTemplateModule)
          .AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule() {
      var builder = new TemplateModule.Builder();
      builder.AddDecorator<WorkerWorkingHours, WorkerShiftHours>();
      builder.AddDecorator<Workplace, ShiftWorkplace>();
      builder.AddDecorator<Worker, WorkerPositionFixer>();
      return builder.Build();
    }

  }
}