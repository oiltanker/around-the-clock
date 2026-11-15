using System;
using Timberborn.BaseComponentSystem;
using Timberborn.DuplicationSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.WorkerTypesUI;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;

namespace AroundTheClock.Core {
  public class ShiftWorkplace : BaseComponent,
                                     IAwakableComponent,
                                     IPersistentEntity,
                                     IInitializableEntity,
                                     IDuplicable<ShiftWorkplace> {

    // Key string must stay "TwoShiftsWorkplace": saves are keyed by this literal
    // (original mod + this fork), not by the class name.
    private static readonly ComponentKey ShiftWorkplaceKey = new("TwoShiftsWorkplace");
    private static readonly PropertyKey<bool> LegacyEnabledKey = new("TwoShiftsEnabled"); // legacy v1.1.1 saves
    private static readonly PropertyKey<int> ShiftCountKey = new("ShiftCount");

    public event EventHandler ShiftCountChanged;
    public int ShiftCount { get; private set; }
    public bool ShiftsEnabled => ShiftCount > 0;
    public bool CanBeEnabled { get; private set; } = true;
    public bool WasLoaded { get; private set; }
    private Workplace _workplace;
    private WorkplaceWorkerType _workplaceWorkerType;
    private int? _desiredWorkersToLoad;

    public void Awake() {
      _workplace = GetComponent<Workplace>();
      _workplaceWorkerType = GetComponent<WorkplaceWorkerType>();
      _workplaceWorkerType.WorkerTypeChanged += (_, _) => UpdateCanBeEnabled();
    }

    public void InitializeEntity() {
      UpdateCanBeEnabled();
    }

    public void Save(IEntitySaver entitySaver) {
      if (ShiftCount > 0) {
        var component = entitySaver.GetComponent(ShiftWorkplaceKey);
        component.Set(ShiftCountKey, ShiftCount);
        if (ShiftCount == 2) {
          component.Set(LegacyEnabledKey, true); // legacy: original mod can read this save
        }
      }
    }

    public void Load(IEntityLoader entityLoader) {
      if (entityLoader.TryGetComponent(ShiftWorkplaceKey, out var component)) {
        if (component.Has(ShiftCountKey)) {
          SetShiftCountInternal(component.Get(ShiftCountKey), isLoading: true);
        } else if (component.Get(LegacyEnabledKey)) {
          SetShiftCountInternal(2, isLoading: true);
        }
      }
      if (_desiredWorkersToLoad.HasValue) {
        Access.Invoke(_workplace, "SetDesiredWorkers",
                      Math.Min(_desiredWorkersToLoad.Value, _workplace.MaxWorkers));
      }
      WasLoaded = true;
    }

    public void SetDesiredWorkersToLoad(int desiredWorkers) {
      _desiredWorkersToLoad = desiredWorkers;
    }

    public void DuplicateFrom(ShiftWorkplace source) {
      if (source.ShiftCount != ShiftCount) {
        SetShiftCountInternal(source.ShiftCount, isLoading: false);
      }
    }

    public void EnableShifts() {
      SetShiftCount(2);
    }

    public void DisableShifts() {
      SetShiftCount(0);
    }

    public void SetShiftCount(int count) {
      SetShiftCountInternal(count, isLoading: false);
    }

    private void UpdateCanBeEnabled() {
      CanBeEnabled = _workplaceWorkerType.WorkerType != WorkerTypeHelper.BotWorkerType;
      if (ShiftCount > 0 && !CanBeEnabled) {
        SetShiftCountInternal(0, isLoading: false);
      }
    }

    private void SetShiftCountInternal(int count, bool isLoading) {
      if (count != 0 && count != 2 && count != 3) {
        return;
      }
      var old = ShiftCount;
      if (old == count) {
        return;
      }
      if (!isLoading) {
        if (old == 0 && count > 0) {
          Access.Invoke(_workplace, "SetDesiredWorkers", _workplace.DesiredWorkers * count);
        } else if (old > 0 && count == 0) {
          var workersToUnassign = _workplace.DesiredWorkers - _workplace.DesiredWorkers / old;
          Access.Invoke(_workplace, "SetDesiredWorkers", _workplace.DesiredWorkers / old);
          UnassignOverflow(workersToUnassign);
        } else {
          var baseWorkers = _workplace.DesiredWorkers / old;
          Access.Invoke(_workplace, "SetDesiredWorkers", baseWorkers * count);
          if (count < old) {
            UnassignOverflow(baseWorkers * (old - count));
          }
        }
      }
      ShiftCount = count;
      ShiftCountChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UnassignOverflow(int limit) {
      for (var i = 0; i < limit && _workplace.Overstaffed; i++) {
        Access.Invoke(_workplace, "UnassignWorkerIfNonworking");
      }
    }

  }
}
