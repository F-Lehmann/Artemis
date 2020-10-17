﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Artemis.Core;
using Artemis.Core.Services;
using Artemis.UI.Screens.ProfileEditor.Conditions.Abstract;
using Artemis.UI.Shared;
using Artemis.UI.Shared.Input;
using Artemis.UI.Shared.Services;
using Stylet;

namespace Artemis.UI.Screens.ProfileEditor.Conditions
{
    public class DataModelConditionPredicateViewModel : DataModelConditionViewModel, IDisposable
    {
        private readonly IConditionOperatorService _conditionOperatorService;
        private readonly IDataModelUIService _dataModelUIService;
        private readonly IProfileEditorService _profileEditorService;
        private DataModelDynamicViewModel _leftSideSelectionViewModel;
        private BindableCollection<ConditionOperator> _operators;
        private DataModelStaticViewModel _rightSideInputViewModel;
        private DataModelDynamicViewModel _rightSideSelectionViewModel;
        private ConditionOperator _selectedOperator;

        private List<Type> _supportedInputTypes;

        public DataModelConditionPredicateViewModel(
            DataModelConditionPredicate dataModelConditionPredicate,
            IProfileEditorService profileEditorService,
            IDataModelUIService dataModelUIService,
            IConditionOperatorService conditionOperatorService,
            ISettingsService settingsService) : base(dataModelConditionPredicate)
        {
            _profileEditorService = profileEditorService;
            _dataModelUIService = dataModelUIService;
            _conditionOperatorService = conditionOperatorService;
            _supportedInputTypes = new List<Type>();

            SelectOperatorCommand = new DelegateCommand(ExecuteSelectOperatorCommand);
            Operators = new BindableCollection<ConditionOperator>();

            ShowDataModelValues = settingsService.GetSetting<bool>("ProfileEditor.ShowDataModelValues");

            Initialize();
        }

        public DataModelConditionPredicate DataModelConditionPredicate => (DataModelConditionPredicate) Model;
        public PluginSetting<bool> ShowDataModelValues { get; }


        public BindableCollection<ConditionOperator> Operators
        {
            get => _operators;
            set => SetAndNotify(ref _operators, value);
        }

        public DataModelDynamicViewModel LeftSideSelectionViewModel
        {
            get => _leftSideSelectionViewModel;
            set => SetAndNotify(ref _leftSideSelectionViewModel, value);
        }

        public ConditionOperator SelectedOperator
        {
            get => _selectedOperator;
            set => SetAndNotify(ref _selectedOperator, value);
        }

        public DataModelDynamicViewModel RightSideSelectionViewModel
        {
            get => _rightSideSelectionViewModel;
            set => SetAndNotify(ref _rightSideSelectionViewModel, value);
        }

        public DataModelStaticViewModel RightSideInputViewModel
        {
            get => _rightSideInputViewModel;
            set => SetAndNotify(ref _rightSideInputViewModel, value);
        }

        public DelegateCommand SelectOperatorCommand { get; }

        public override void Delete()
        {
            base.Delete();
            _profileEditorService.UpdateSelectedProfileElement();
        }

        public void Initialize()
        {
            LeftSideSelectionViewModel = _dataModelUIService.GetDynamicSelectionViewModel(_profileEditorService.GetCurrentModule());
            LeftSideSelectionViewModel.PropertySelected += LeftSideOnPropertySelected;
            // Determine which types are currently supported
            IReadOnlyCollection<DataModelVisualizationRegistration> editors = _dataModelUIService.RegisteredDataModelEditors;
            _supportedInputTypes = editors.Select(e => e.SupportedType).ToList();
            _supportedInputTypes.AddRange(editors.Where(e => e.CompatibleConversionTypes != null).SelectMany(e => e.CompatibleConversionTypes));
            _supportedInputTypes.Add(typeof(IEnumerable<>));
            Update();
        }

        public override void Update()
        {
            LeftSideSelectionViewModel.FilterTypes = _supportedInputTypes.ToArray();
            LeftSideSelectionViewModel.ChangeDataModelPath(DataModelConditionPredicate.LeftPath);

            Type leftSideType = LeftSideSelectionViewModel.DataModelPath?.GetPropertyType();

            // Get the supported operators
            Operators.Clear();
            Operators.AddRange(_conditionOperatorService.GetConditionOperatorsForType(leftSideType ?? typeof(object)));
            if (DataModelConditionPredicate.Operator == null)
                DataModelConditionPredicate.UpdateOperator(Operators.FirstOrDefault(o => o.SupportsType(leftSideType ?? typeof(object))));
            SelectedOperator = DataModelConditionPredicate.Operator;

            if (SelectedOperator == null || !SelectedOperator.SupportsRightSide)
            {
                DisposeRightSideStaticViewModel();
                DisposeRightSideDynamicViewModel();
                return;
            }

            // Ensure the right side has the proper VM
            if (DataModelConditionPredicate.PredicateType == ProfileRightSideType.Dynamic)
            {
                DisposeRightSideStaticViewModel();
                if (RightSideSelectionViewModel == null)
                    CreateRightSideSelectionViewModel();

                RightSideSelectionViewModel.ChangeDataModelPath(DataModelConditionPredicate.RightPath);
                RightSideSelectionViewModel.FilterTypes = new[] {leftSideType};
            }
            else
            {
                DisposeRightSideDynamicViewModel();
                if (RightSideInputViewModel == null)
                    CreateRightSideInputViewModel(leftSideType);

                if (leftSideType != null && leftSideType.IsValueType && DataModelConditionPredicate.RightStaticValue == null)
                    RightSideInputViewModel.Value = leftSideType.GetDefault();
                else
                    RightSideInputViewModel.Value = DataModelConditionPredicate.RightStaticValue;
                if (RightSideInputViewModel.TargetType != leftSideType)
                    RightSideInputViewModel.UpdateTargetType(leftSideType);
            }
        }

        public void ApplyLeftSide()
        {
            if (LeftSideSelectionViewModel.DataModelPath.GetPropertyType().IsGenericEnumerable())
            {
                if (Parent is DataModelConditionGroupViewModel groupViewModel)
                    groupViewModel.ConvertToConditionList(this);
                return;
            }

            DataModelConditionPredicate.UpdateLeftSide(LeftSideSelectionViewModel.DataModelPath);
            _profileEditorService.UpdateSelectedProfileElement();

            SelectedOperator = DataModelConditionPredicate.Operator;
            Update();
        }

        public void ApplyRightSideDynamic()
        {
            DataModelConditionPredicate.UpdateRightSideDynamic(RightSideSelectionViewModel.DataModelPath);
            _profileEditorService.UpdateSelectedProfileElement();

            Update();
        }

        public void ApplyRightSideStatic(object value)
        {
            DataModelConditionPredicate.UpdateRightSideStatic(value);
            _profileEditorService.UpdateSelectedProfileElement();

            Update();
        }

        public void ApplyOperator()
        {
            DataModelConditionPredicate.UpdateOperator(SelectedOperator);
            _profileEditorService.UpdateSelectedProfileElement();

            Update();
        }

        private void ExecuteSelectOperatorCommand(object context)
        {
            if (!(context is ConditionOperator DataModelConditionOperator))
                return;

            SelectedOperator = DataModelConditionOperator;
            ApplyOperator();
        }

        #region IDisposable

        public void Dispose()
        {
            if (LeftSideSelectionViewModel != null)
            {
                LeftSideSelectionViewModel.PropertySelected -= LeftSideOnPropertySelected;
                LeftSideSelectionViewModel.Dispose();
                LeftSideSelectionViewModel = null;
            }

            DisposeRightSideStaticViewModel();
            DisposeRightSideDynamicViewModel();
        }

        #endregion

        #region View model creation

        private void CreateRightSideSelectionViewModel()
        {
            RightSideSelectionViewModel = _dataModelUIService.GetDynamicSelectionViewModel(_profileEditorService.GetCurrentModule());
            RightSideSelectionViewModel.ButtonBrush = (Brush) Application.Current.FindResource("PrimaryHueMidBrush");
            RightSideSelectionViewModel.DisplaySwitchButton = true;
            RightSideSelectionViewModel.PropertySelected += RightSideOnPropertySelected;
            RightSideSelectionViewModel.SwitchToStaticRequested += RightSideSelectionViewModelOnSwitchToStaticRequested;
        }

        private void CreateRightSideInputViewModel(Type leftSideType)
        {
            RightSideInputViewModel = _dataModelUIService.GetStaticInputViewModel(leftSideType, LeftSideSelectionViewModel.DataModelPath?.GetPropertyDescription());
            RightSideInputViewModel.ButtonBrush = (Brush) Application.Current.FindResource("PrimaryHueMidBrush");
            RightSideInputViewModel.ValueUpdated += RightSideOnValueEntered;
            RightSideInputViewModel.SwitchToDynamicRequested += RightSideInputViewModelOnSwitchToDynamicRequested;
        }

        private void DisposeRightSideStaticViewModel()
        {
            if (RightSideInputViewModel == null)
                return;
            RightSideInputViewModel.ValueUpdated -= RightSideOnValueEntered;
            RightSideInputViewModel.SwitchToDynamicRequested -= RightSideInputViewModelOnSwitchToDynamicRequested;
            RightSideInputViewModel.Dispose();
            RightSideInputViewModel = null;
        }

        private void DisposeRightSideDynamicViewModel()
        {
            if (RightSideSelectionViewModel == null)
                return;
            RightSideSelectionViewModel.PropertySelected -= RightSideOnPropertySelected;
            RightSideSelectionViewModel.SwitchToStaticRequested -= RightSideSelectionViewModelOnSwitchToStaticRequested;
            RightSideSelectionViewModel.Dispose();
            RightSideSelectionViewModel = null;
        }

        #endregion

        #region Event handlers

        private void LeftSideOnPropertySelected(object sender, DataModelInputDynamicEventArgs e)
        {
            ApplyLeftSide();
        }

        private void RightSideOnPropertySelected(object sender, DataModelInputDynamicEventArgs e)
        {
            ApplyRightSideDynamic();
        }

        private void RightSideOnValueEntered(object sender, DataModelInputStaticEventArgs e)
        {
            ApplyRightSideStatic(e.Value);
        }

        private void RightSideSelectionViewModelOnSwitchToStaticRequested(object sender, EventArgs e)
        {
            DataModelConditionPredicate.PredicateType = ProfileRightSideType.Static;
            Update();
        }


        private void RightSideInputViewModelOnSwitchToDynamicRequested(object? sender, EventArgs e)
        {
            DataModelConditionPredicate.PredicateType = ProfileRightSideType.Dynamic;
            Update();
        }

        #endregion
    }
}