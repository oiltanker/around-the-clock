using Timberborn.BaseComponentSystem;
using Timberborn.Common;
using Timberborn.TimeSystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace AroundTheClock.Core {
  public class WorkerShiftHours : BaseComponent,
                                       IAwakableComponent {

    public const int VanillaSleepStart = 22;
    public const int VanillaSleepLength = 8;
    public const int MinSleepOverlapHours = 6;

    private readonly IDayNightCycle _dayNightCycle;
    private readonly WorkingHoursManager _workingHoursManager;
    private Worker _worker;
    private WorkerWorkingHours _workerWorkingHours;

    public WorkerShiftHours(IDayNightCycle dayNightCycle,
                                 WorkingHoursManager workingHoursManager) {
      _dayNightCycle = dayNightCycle;
      _workingHoursManager = workingHoursManager;
    }

    public void Awake() {
      _worker = GetComponent<Worker>();
      _workerWorkingHours = GetComponent<WorkerWorkingHours>();
    }

    public bool AreWorkingHours() {
      if (Access.GetField<bool>(_workerWorkingHours, "_ignoreWorkingHours")) {
        return true;
      }
      var count = WorkplaceShiftCount;
      if (count == 0) {
        return _workingHoursManager.AreWorkingHours;
      }
      return TimeShiftIndex(count) == ShiftIndex;
    }

    public bool IsAfternoonShiftWorker() {
      return ShiftIndex == 1;
    }

    public bool IsShiftWorker() {
      return ShiftIndex >= 0;
    }

    public int ShiftIndex {
      get {
        if (_worker is not { Employed: true }) {
          return -1;
        }
        var count = WorkplaceShiftCount;
        if (count == 0) {
          return -1;
        }
        return _worker.Workplace.AssignedWorkers.IndexOf(_worker) % count;
      }
    }

    public int WorkplaceShiftCount {
      get {
        return _worker.Workplace?.GetComponent<ShiftWorkplace>()?.ShiftCount ?? 0;
      }
    }

    // Sleep window (start hour, length in hours), used for housing compatibility.
    public (int Start, int Length) GetSleepWindow() {
      var count = WorkplaceShiftCount;
      var index = ShiftIndex;
      if (count == 0 || index < 0) {
        return (VanillaSleepStart, VanillaSleepLength);
      }
      if (count == 2) {
        return index == 0 ? (VanillaSleepStart, VanillaSleepLength) : (0, 12);
      }
      return (index * 8, 8);
    }

    public bool GoesToSleepTogetherWith(WorkerShiftHours other) {
      var myCount = WorkplaceShiftCount;
      var otherCount = other.WorkplaceShiftCount;
      if (myCount > 0 && otherCount > 0 && myCount == otherCount) {
        // Same scheme: same shift only (upstream semantics).
        return ShiftIndex == other.ShiftIndex;
      }
      // Mixed or vanilla: sleep-window overlap rule.
      return SleepOverlap(GetSleepWindow(), other.GetSleepWindow()) >= MinSleepOverlapHours;
    }

    public bool NeedsWakeAdjust() {
      return WorkplaceShiftCount > 0 && ShiftIndex >= 1;
    }

    public int WakeHour {
      get {
        var count = WorkplaceShiftCount;
        return count == 2 ? 12 : ShiftIndex * 8;
      }
    }

    private int TimeShiftIndex(int shiftCount) {
      return Mathf.Clamp(Mathf.FloorToInt(_dayNightCycle.DayProgress * shiftCount), 0, shiftCount - 1);
    }

    private static int SleepOverlap((int Start, int Length) w1, (int Start, int Length) w2) {
      var overlap = 0;
      for (var hour = 0; hour < 24; hour++) {
        if (HourInWindow(hour, w1) && HourInWindow(hour, w2)) {
          overlap++;
        }
      }
      return overlap;
    }

    private static bool HourInWindow(int hour, (int Start, int Length) window) {
      var relative = (hour - window.Start % 24 + 24) % 24;
      return relative < window.Length;
    }

  }
}
