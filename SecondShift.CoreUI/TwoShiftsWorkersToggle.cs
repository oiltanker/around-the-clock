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

    // Midday icon ships as a loose PNG (Assets/midday-shift.png) in the mod
    // folder; asset bundles are the official channel but need a Unity Editor
    // build, so we load the single sprite at runtime instead. Mods are
    // byte-loaded (Assembly.Location is empty), so fall back to searching the
    // standard mod locations.
    private Sprite LoadMiddaySprite() {
      if (_middaySprite != null) {
        return _middaySprite;
      }
      try {
        var path = FindMiddayIconPath()
            ?? throw new FileNotFoundException("midday-shift.png not found in known mod locations");
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

    private static string FindMiddayIconPath() {
      const string fileName = "midday-shift.png";
      var candidates = new List<string>();
      try {
        var location = typeof(TwoShiftsWorkersToggle).Assembly.Location;
        if (!string.IsNullOrEmpty(location)) {
          var dir = Path.GetDirectoryName(location);
          candidates.Add(Path.GetFullPath(Path.Combine(dir, "..", "Assets", fileName)));
          for (var parent = Directory.GetParent(dir); parent != null; parent = parent.Parent) {
            candidates.Add(Path.Combine(parent.FullName, "Assets", fileName));
          }
        }
      } catch {
        // path probing is best-effort
      }
      var localMods = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                   "Timberborn", "Mods");
      TryAddLocalCandidates(candidates, localMods, fileName);
      TryAddLocalCandidates(candidates,
                            Path.Combine(@"C:\Program Files (x86)\Steam",
                                         "steamapps", "workshop", "content", "1062140"),
                            fileName);
      return candidates.Find(File.Exists);
    }

    private static void TryAddLocalCandidates(List<string> candidates, string root, string fileName) {
      try {
        if (!Directory.Exists(root)) {
          return;
        }
        foreach (var modDir in Directory.EnumerateDirectories(root)) {
          foreach (var versionDir in Directory.EnumerateDirectories(modDir, "version-*")) {
            candidates.Add(Path.Combine(versionDir, "Assets", fileName));
          }
        }
      } catch {
        // path probing is best-effort
      }
    }

  }
}
