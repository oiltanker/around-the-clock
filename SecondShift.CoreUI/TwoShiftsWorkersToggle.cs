using SecondShift.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Timberborn.AssetSystem;
using Timberborn.Common;
using Timberborn.CoreUI;
using Timberborn.Localization;
using Timberborn.SliderToggleSystem;
using Timberborn.TimeSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondShift.CoreUI {
  internal class TwoShiftsWorkersToggle {

    private readonly SliderToggleFactory _sliderToggleFactory;
    private readonly ILoc _loc;
    private readonly IAssetLoader _assetLoader;
    private readonly IDayNightCycle _dayNightCycle;
    private SliderToggle _sliderToggle;
    private Sprite _middaySprite;

    public int SelectedShift { get; private set; }
    public int ShiftCount { get; private set; }

    public TwoShiftsWorkersToggle(SliderToggleFactory sliderToggleFactory,
                                  ILoc loc,
                                  IAssetLoader assetLoader,
                                  IDayNightCycle dayNightCycle) {
      _sliderToggleFactory = sliderToggleFactory;
      _loc = loc;
      _assetLoader = assetLoader;
      _dayNightCycle = dayNightCycle;
    }

    public void Initialize(VisualElement parent) {
      parent.Clear();
      var shiftCount = ShiftCount;
      var items = new List<SliderToggleItem>();
      for (var shift = 0; shift < shiftCount; shift++) {
        var index = shift;
        items.Add(SliderToggleItem.Create(new Func<string>(() => GetTooltip(index)), GetIcon(shift),
                                           new Action(() => SelectShift(index)),
                                           new Func<bool>(() => SelectedShift == index)));
      }
      _sliderToggle = _sliderToggleFactory.Create(parent, items.ToArray());
    }

    public void Open(TwoShiftsWorkplace twoShiftsWorkplace) {
      ShiftCount = twoShiftsWorkplace.ShiftCount;
      if (twoShiftsWorkplace.ShiftCount > 0) {
        Show();
      } else {
        Hide();
      }
    }

    public void SetShiftCount(int shiftCount) {
      ShiftCount = shiftCount;
    }

    public void Update() {
      _sliderToggle?.Update();
    }

    public void Show() {
      SelectedShift = CurrentShift();
      _sliderToggle.Root.ToggleDisplayStyle(true);
    }

    public void Hide() {
      _sliderToggle?.Root.ToggleDisplayStyle(false);
    }

    private int CurrentShift() {
      var shiftCount = ShiftCount;
      if (shiftCount <= 0) {
        return 0;
      }
      return Mathf.Clamp(Mathf.FloorToInt(_dayNightCycle.DayProgress * shiftCount), 0, shiftCount - 1);
    }

    private void SelectShift(int shift) {
      SelectedShift = shift;
    }

    private string GetTooltip(int shift) {
      var shiftCount = ShiftCount;
      if (shiftCount == 2) {
        return shift == 0
            ? _loc.T("SecondShift.ShowFirstShift")
            : _loc.T("SecondShift.ShowSecondShift");
      }
      return _loc.T("SecondShift.ShowShift" + (shift + 1));
    }

    private Sprite GetIcon(int shift) {
      return shift switch {
          0 => _assetLoader.Load<Sprite>("UI/Images/Game/first-shift"),
          1 when ShiftCount == 2 => _assetLoader.Load<Sprite>("UI/Images/Game/second-shift"),
          1 => LoadMiddaySprite(),
          _ => _assetLoader.Load<Sprite>("UI/Images/Game/second-shift")
      };
    }

    // Midday icon ships as a loose PNG (Assets/midday-shift.png) next to the
    // mod's version folder; asset bundles are the official channel but need
    // a Unity Editor build, so we load the single sprite at runtime instead.
    private Sprite LoadMiddaySprite() {
      if (_middaySprite != null) {
        return _middaySprite;
      }
      try {
        var dllDir = Path.GetDirectoryName(typeof(TwoShiftsWorkersToggle).Assembly.Location);
        var path = Path.Combine(dllDir, "..", "Assets", "midday-shift.png");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path))) {
          throw new IOException("LoadImage failed");
        }
        _middaySprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                                      new Vector2(0.5f, 0.5f), 100f);
      } catch (Exception e) {
        Debug.LogWarning($"[SecondShift] midday-shift.png load failed, using moon fallback: {e.Message}");
        _middaySprite = _assetLoader.Load<Sprite>("UI/Images/Game/second-shift");
      }
      return _middaySprite;
    }

  }
}
