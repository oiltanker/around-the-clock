using SecondShift.Core;
using System;
using System.Collections.Generic;
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
        items.Add(SliderToggleItem.Create(new Func<string>(() => GetTooltip(shift)), GetIcon(shift),
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
          1 => _assetLoader.Load<Sprite>("UI/Images/Game/second-shift"),
          _ => CreateFallbackIcon(shift)
      };
    }

    // Third shift has no shipped sprite; generate a simple colored icon.
    private static Sprite CreateFallbackIcon(int shift) {
      const int size = 64;
      var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
      var colors = new Color32[size * size];
      var color = new Color32(0x9B, 0x22, 0xD9, 0xFF); // purple
      for (var i = 0; i < colors.Length; i++) {
        colors[i] = color;
      }
      texture.SetPixels32(colors);
      texture.Apply();
      return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

  }
}
