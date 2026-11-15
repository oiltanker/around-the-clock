using SecondShift.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using Timberborn.BaseComponentSystem;
using Timberborn.Bots;
using Timberborn.Characters;
using Timberborn.CoreUI;
using Timberborn.DropdownSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.GameDistricts;
using Timberborn.InputSystemUI;
using Timberborn.Localization;
using Timberborn.PrioritySystemUI;
using Timberborn.SelectionSystem;
using Timberborn.TooltipSystem;
using Timberborn.WorkSystem;
using Timberborn.WorkSystemUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondShift.CoreUI {
  internal class TwoShiftsWorkplaceFragment : IEntityPanelFragment {

    private const int RegularMode = 0;
    private const int TwoShiftsMode = 1;
    private const int ThreeShiftsMode = 2;

    private static readonly string PriorityLabelLocKey = "Work.Workplace.DisplayName";
    private static readonly string PriorityTooltipLocKey = "Work.PriorityTitle";
    private static readonly string CurrentWorkersLocKey = "Work.CurrentWorkers";
    private static readonly string SecondShiftDisabledLocKey = "SecondShift.Disabled";
    private static readonly string ShiftModeLocKey = "SecondShift.ShiftMode";
    private static readonly string ModeRegularLocKey = "SecondShift.Mode.Regular";
    private static readonly string ModeTwoShiftsLocKey = "SecondShift.Mode.TwoShifts";
    private static readonly string ModeThreeShiftsLocKey = "SecondShift.Mode.ThreeShifts";
    private static readonly string IncreaseWorkersKey = "IncreaseWorkers";
    private static readonly string DecreaseWorkersKey = "DecreaseWorkers";
    private static readonly string ToggleWorkerTypeKey = "ToggleWorkerType";
    private readonly VisualElementLoader _visualElementLoader;
    private readonly object _workerViewFactory;
    private readonly EntitySelectionService _entitySelectionService;
    private readonly ILoc _loc;
    private readonly ITooltipRegistrar _tooltipRegistrar;
    private readonly object _workplacePriorityToggleGroupFactory;
    private readonly BotPopulation _botPopulation;
    private readonly WorkerTypeToggleFactory _workerTypeToggleFactory;
    private readonly BindableButtonFactory _bindableButtonFactory;
    private readonly DropdownListDrawer _dropdownListDrawer;
    private readonly DropdownItemsSetter _dropdownItemsSetter;
    private readonly TwoShiftsWorkersToggle _twoShiftsWorkersToggle;
    private VisualElement _root;
    private VisualElement _workplaceUsers;
    private Label _text;
    private BindableButton _increase;
    private BindableButton _decrease;
    private Toggle _twoShiftsEnabledToggle;
    private Dropdown _shiftModeDropdown;
    private WorkerTypeToggle _workerTypeToggle;
    private Workplace _workplace;
    private TwoShiftsWorkplace _twoShiftsWorkplace;
    private WorkplaceDescriber _workplaceDescriber;
    private readonly List<WorkerViewRef> _views = new();
    private PriorityToggleGroup _priorityToggleGroup;

    public TwoShiftsWorkplaceFragment(
        VisualElementLoader visualElementLoader,
        WorkerViewFactory workerViewFactory,
        EntitySelectionService entitySelectionService,
        ILoc loc,
        ITooltipRegistrar tooltipRegistrar,
        WorkplacePriorityToggleGroupFactory workplacePriorityToggleGroupFactory,
        BotPopulation botPopulation,
        WorkerTypeToggleFactory workerTypeToggleFactory,
        BindableButtonFactory bindableButtonFactory,
        DropdownListDrawer dropdownListDrawer,
        DropdownItemsSetter dropdownItemsSetter,
        TwoShiftsWorkersToggle twoShiftsWorkersToggle) {
      _visualElementLoader = visualElementLoader;
      _workerViewFactory = workerViewFactory;
      _entitySelectionService = entitySelectionService;
      _loc = loc;
      _tooltipRegistrar = tooltipRegistrar;
      _workplacePriorityToggleGroupFactory = workplacePriorityToggleGroupFactory;
      _botPopulation = botPopulation;
      _workerTypeToggleFactory = workerTypeToggleFactory;
      _bindableButtonFactory = bindableButtonFactory;
      _dropdownListDrawer = dropdownListDrawer;
      _dropdownItemsSetter = dropdownItemsSetter;
      _twoShiftsWorkersToggle = twoShiftsWorkersToggle;
    }

    public VisualElement InitializeFragment() {
      _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/TwoShiftsWorkplaceFragment");
      _workplaceUsers = _root.Q<VisualElement>("WorkplaceUsers");
      _text = _root.Q<Label>("Text");
      _tooltipRegistrar.Register(_text, () => _workplaceDescriber.GetWorkersTooltip());
      _increase = _bindableButtonFactory.Create(_root.Q<Button>("Increase"),
                                                 IncreaseWorkersKey,
                                                 () => _workplace.IncreaseDesiredWorkers());
      _decrease = _bindableButtonFactory.Create(_root.Q<Button>("Decrease"),
                                                 DecreaseWorkersKey,
                                                 () => _workplace.DecreaseDesiredWorkers());
      var visualElement = _root.Q<VisualElement>("HeaderWrapper");
      _priorityToggleGroup = (PriorityToggleGroup)Reflection.Invoke(
          _workplacePriorityToggleGroupFactory, "Create", visualElement, PriorityLabelLocKey);
      _tooltipRegistrar.RegisterLocalizable(visualElement.Q<VisualElement>("TogglesWrapper"),
                                            PriorityTooltipLocKey);
      _workerTypeToggle = _workerTypeToggleFactory.CreateBindable(
          _root.Q<VisualElement>("WorkerTypeToggleWrapper"), ToggleWorkerTypeKey);
      _twoShiftsEnabledToggle = _root.Q<Toggle>("TwoShiftsToggle");
      _twoShiftsEnabledToggle.RegisterValueChangedCallback(ToggleTwoShifts);
      _tooltipRegistrar.Register(_twoShiftsEnabledToggle, GetTwoShiftsTooltip);

      // Replace the fixed UXML toggle with a 3-way dropdown: Regular / 2 / 3 shifts.
      var toggleParent = _twoShiftsEnabledToggle.parent;
      _twoShiftsEnabledToggle.style.display = DisplayStyle.None;
      _shiftModeDropdown = new Dropdown();
      _shiftModeDropdown.Initialize(_dropdownListDrawer);
      _shiftModeDropdown.OverrideLabelLocKey(ShiftModeLocKey);
      // Runtime-created dropdowns never get Localize() called, so the internal
      // label stays empty; hide it and render our own row label instead.
      _shiftModeDropdown.Q<Label>("Label").style.display = DisplayStyle.None;
      _dropdownItemsSetter.SetItems(_shiftModeDropdown, new ShiftModeDropdownProvider(
          new[] { _loc.T(ModeRegularLocKey), _loc.T(ModeTwoShiftsLocKey), _loc.T(ModeThreeShiftsLocKey) },
          () => _twoShiftsWorkplace?.ShiftCount ?? 0,
          count => {
            if (_twoShiftsWorkplace != null) {
              _twoShiftsWorkplace.SetShiftCount(count);
            }
          }));
      // Label on the left, dropdown pushed to the right.
      var modeRow = new VisualElement();
      modeRow.style.flexDirection = FlexDirection.Row;
      modeRow.style.alignItems = Align.Center;
      modeRow.style.flexGrow = 1;
      var modeLabel = new Label(_loc.T(ShiftModeLocKey));
      modeLabel.AddToClassList("entity-panel__single-line-text");
      modeLabel.style.flexGrow = 1;
      modeRow.Add(modeLabel);
      modeRow.Add(_shiftModeDropdown);
      toggleParent.Add(modeRow);

      // Give the shift-viewer bar its own full-width row: sharing the row with
      // the flexed dropdown squeezes three buttons.
      var barWrapper = _root.Q<VisualElement>("TwoShiftsWorkersToggleWrapper");
      var oldRow = barWrapper.parent;
      var barRow = new VisualElement();
      barRow.style.flexDirection = FlexDirection.Row;
      barRow.style.justifyContent = Justify.FlexEnd;
      barRow.style.marginTop = 2;
      oldRow.parent.Insert(oldRow.parent.IndexOf(oldRow) + 1, barRow);
      oldRow.Remove(barWrapper);
      barRow.Add(barWrapper);
      // Cancel any stylesheet-fixed width so the bar grows with its buttons.
      barWrapper.style.width = new StyleLength(StyleKeyword.Initial);
      barWrapper.style.maxWidth = new StyleLength(StyleKeyword.Initial);
      _twoShiftsWorkersToggle.Initialize(barWrapper);
      _root.ToggleDisplayStyle(false);
      return _root;
    }

    public void ShowFragment(BaseComponent entity) {
      _workplace = entity.GetComponent<Workplace>();
      if (_workplace != null) {
        _workplaceDescriber = entity.GetComponent<WorkplaceDescriber>();
        _twoShiftsWorkplace = entity.GetComponent<TwoShiftsWorkplace>();
        var component = entity.GetComponent<WorkplaceWorkerType>();
        _workerTypeToggle.Show(component);
        _priorityToggleGroup.Enable(entity.GetComponent<WorkplacePriority>());
        AddEmptyViews(component);
        _increase.Bind();
        _decrease.Bind();
        _shiftModeDropdown.UpdateSelectedValue();
        _twoShiftsWorkplace.ShiftCountChanged += OnShiftCountChanged;
        _twoShiftsWorkersToggle.SetShiftCount(_twoShiftsWorkplace.ShiftCount);
        _twoShiftsWorkersToggle.Open(_twoShiftsWorkplace);
      }
    }

    public void ClearFragment() {
      _workplace = null;
      _workplaceDescriber = null;
      if (_twoShiftsWorkplace != null) {
        _twoShiftsWorkplace.ShiftCountChanged -= OnShiftCountChanged;
      }
      _twoShiftsWorkplace = null;
      _workerTypeToggle.Clear();
      _priorityToggleGroup.Disable();
      _root.ToggleDisplayStyle(false);
      _increase.Unbind();
      _decrease.Unbind();
    }

    public void UpdateFragment() {
      if (_workplace != null) {
        _priorityToggleGroup.UpdateGroup();
        UpdateButtons();
        if (_workplace.Enabled) {
          UpdateViews();
        }
        _workplaceUsers.ToggleDisplayStyle(_workplace.Enabled);
        if (_botPopulation.BotCreated) {
          _workerTypeToggle.Update();
          _workerTypeToggle.Root.ToggleDisplayStyle(true);
        } else {
          _workerTypeToggle.Root.ToggleDisplayStyle(false);
        }
        _root.ToggleDisplayStyle(true);
        _shiftModeDropdown.SetEnabled(_twoShiftsWorkplace.CanBeEnabled);
        if (_twoShiftsWorkplace.TwoShiftsEnabled) {
          _twoShiftsWorkersToggle.Update();
        }
      } else {
        _root.ToggleDisplayStyle(false);
      }
    }

    private void AddEmptyViews(WorkplaceWorkerType workplaceWorkerType) {
      var shiftCount = _twoShiftsWorkplace.ShiftCount;
      var workersToShow = shiftCount > 0
          ? _workplace.MaxWorkers / shiftCount
          : _workplace.MaxWorkers;
      RemoveViews();
      for (var index = 0; index < workersToShow; ++index) {
        AddEmptyView(workplaceWorkerType);
      }
    }

    private void RemoveViews() {
      _workplaceUsers.Clear();
      _views.Clear();
    }

    private void AddEmptyView(WorkplaceWorkerType workplaceWorkerType) {
      var workerView = WorkerViewRef.Create(_workerViewFactory, workplaceWorkerType);
      workerView.ShowEmpty();
      _views.Add(workerView);
      _workplaceUsers.Add(workerView.Root);
    }

    private void UpdateViews() {
      var workers = _workplace.AssignedWorkers;
      var index = 0;
      foreach (var worker in workers) {
        if (ShouldBeShown(worker) && index < _views.Count) {
          var character = worker.GetComponent<Character>();
          var view = _views[index];
          var onClick = (Action) (() => _entitySelectionService.SelectAndFollow(character));
          view.Fill(character, onClick, character.FirstName);
          ++index;
        }
      }
      var component = _workplace.GetComponent<DistrictBuilding>();
      var shiftCount = _twoShiftsWorkplace.ShiftCount;
      var desiredWorkers = shiftCount > 0
          ? _workplace.DesiredWorkers / shiftCount
          : _workplace.DesiredWorkers;
      if (_workplace.Understaffed && (bool) (BaseComponent) component.InstantDistrict) {
        if (desiredWorkers > _views.Count) {
          Debug.LogError($"{nameof(TwoShiftsWorkplaceFragment)}: Not enough views to show all "
                         + $"workers. Desired: {desiredWorkers}, Current: {_views.Count}, "
                         + $"Workplace: {component.Name}, MaxWorkers: {_workplace.MaxWorkers}, "
                         + $"ShiftCount: {shiftCount}");
        }
        for (; index < desiredWorkers; ++index) {
          _views[index].ShowVacant();
        }
      }
      for (; index < _views.Count; ++index) {
        _views[index].ShowEmpty();
      }
    }

    private void UpdateButtons() {
      var shiftCount = _twoShiftsWorkplace.ShiftCount;
      var desiredWorkers = _workplace.DesiredWorkers;
      var maxWorkers = _workplace.MaxWorkers;
      var displayedDesiredWorkers = shiftCount > 0
          ? desiredWorkers / shiftCount
          : desiredWorkers;
      _text.text = _loc.T(CurrentWorkersLocKey, GetNumberOfWorkersToShow(),
                          displayedDesiredWorkers);
      if (displayedDesiredWorkers > 1) {
        _decrease.Enable();
      } else {
        _decrease.Disable();
      }
      if (desiredWorkers < maxWorkers) {
        _increase.Enable();
      } else {
        _increase.Disable();
      }
    }

    private int GetNumberOfWorkersToShow() {
      var shiftCount = _twoShiftsWorkplace.ShiftCount;
      var assigned = _workplace.NumberOfAssignedWorkers;
      if (shiftCount == 0) {
        return assigned;
      }
      var count = 0;
      foreach (var worker in _workplace.AssignedWorkers) {
        if (worker.GetComponent<TwoShiftsWorkingHours>().ShiftIndex
            == _twoShiftsWorkersToggle.SelectedShift) {
          count++;
        }
      }
      return count;
    }

    private string GetTwoShiftsTooltip() {
      return _twoShiftsWorkplace.CanBeEnabled ? null : _loc.T(SecondShiftDisabledLocKey);
    }

    private bool ShouldBeShown(Worker worker) {
      var shiftCount = _twoShiftsWorkplace.ShiftCount;
      if (shiftCount == 0) {
        return true;
      }
      return worker.GetComponent<TwoShiftsWorkingHours>().ShiftIndex
             == _twoShiftsWorkersToggle.SelectedShift;
    }

    private void OnShiftCountChanged(object sender, EventArgs e) {
      var shiftCount = _twoShiftsWorkplace.ShiftCount;
      _shiftModeDropdown.UpdateSelectedValue();
      AddEmptyViews(_workplace.GetComponent<WorkplaceWorkerType>());
      if (shiftCount > 0) {
        _twoShiftsWorkersToggle.SetShiftCount(shiftCount);
        _twoShiftsWorkersToggle.Initialize(
            _root.Q<VisualElement>("TwoShiftsWorkersToggleWrapper"));
        _twoShiftsWorkersToggle.Show();
      } else {
        _twoShiftsWorkersToggle.Hide();
      }
    }

    private void ToggleTwoShifts(ChangeEvent<bool> evt) {
      // Legacy UXML toggle is hidden; dropdown drives the state.
    }

    // WorkerView is internal: late-bound wrapper around the reflected instance.
    internal readonly struct WorkerViewRef {
      private static MethodInfo _create;
      private static MethodInfo _showEmpty;
      private static MethodInfo _showVacant;
      private static MethodInfo _fill;
      private static PropertyInfo _root;
      private readonly object _view;

      private WorkerViewRef(object view) {
        _view = view;
      }

      public static WorkerViewRef Create(object factory, WorkplaceWorkerType workerType) {
        if (_create == null) {
          _create = factory.GetType().GetMethod("Create");
        }
        return new WorkerViewRef(_create.Invoke(factory, new object[] { workerType }));
      }

      public VisualElement Root {
        get {
          if (_root == null) {
            _root = _view.GetType().GetProperty("Root");
          }
          return (VisualElement)_root.GetValue(_view, null);
        }
      }

      public void ShowEmpty() {
        if (_showEmpty == null) {
          _showEmpty = _view.GetType().GetMethod("ShowEmpty", Type.EmptyTypes);
        }
        _showEmpty.Invoke(_view, null);
      }

      public void ShowVacant() {
        if (_showVacant == null) {
          _showVacant = _view.GetType().GetMethod("ShowVacant", Type.EmptyTypes);
        }
        _showVacant.Invoke(_view, null);
      }

      public void Fill(object user, Action onClick, string description) {
        if (_fill == null) {
          _fill = _view.GetType().GetMethod("Fill");
        }
        _fill.Invoke(_view, new object[] { user, onClick, description });
      }
    }
  }

  // Cached late-bound invocation for internal game methods.
  internal static class Reflection {
    private static readonly Dictionary<string, MethodInfo> Methods = new();

    public static object Invoke(object obj, string name, params object[] args) {
      var key = obj.GetType().FullName + "::" + name;
      if (!Methods.TryGetValue(key, out var method)) {
        method = obj.GetType().GetMethod(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Methods[key] = method;
      }
      return method.Invoke(obj, args);
    }
  }
}
