using System;
using System.Collections.Generic;
using Timberborn.DropdownSystem;

namespace SecondShift.CoreUI {
  // Bridges the shift-mode Dropdown to TwoShiftsWorkplace. Items are display strings.
  internal sealed class ShiftModeDropdownProvider : IDropdownProvider {
    private readonly IReadOnlyList<string> _items;
    private readonly Func<int> _getShiftCount;
    private readonly Action<int> _setShiftCount;

    public ShiftModeDropdownProvider(IReadOnlyList<string> items,
                                      Func<int> getShiftCount,
                                      Action<int> setShiftCount) {
      _items = items;
      _getShiftCount = getShiftCount;
      _setShiftCount = setShiftCount;
    }

    public IReadOnlyList<string> Items => _items;

    public string GetValue() => _items[IndexForShiftCount(_getShiftCount())];

    public void SetValue(string value) {
      for (var index = 0; index < _items.Count; index++) {
        if (_items[index] == value) {
          _setShiftCount(ShiftCountForIndex(index));
          return;
        }
      }
    }

    private static int IndexForShiftCount(int shiftCount) => shiftCount switch {
        2 => 1,
        3 => 2,
        _ => 0
    };

    private static int ShiftCountForIndex(int index) => index switch {
        1 => 2,
        2 => 3,
        _ => 0
    };
  }
}
