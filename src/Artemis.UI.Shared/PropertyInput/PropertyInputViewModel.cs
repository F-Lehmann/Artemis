﻿using System;
using Artemis.Core.Models.Profile.LayerProperties;
using Artemis.UI.Shared.Services.Interfaces;
using Stylet;

namespace Artemis.UI.Shared.PropertyInput
{
    public abstract class PropertyInputViewModel<T> : ValidatingModelBase, IDisposable
    {
        private T _inputValue;

        protected PropertyInputViewModel(LayerProperty<T> layerProperty, IProfileEditorService profileEditorService)
        {
            LayerProperty = layerProperty;
            ProfileEditorService = profileEditorService;
            LayerProperty.Updated += LayerPropertyOnUpdated;
            UpdateInputValue();
        }

        protected PropertyInputViewModel(LayerProperty<T> layerProperty, IProfileEditorService profileEditorService, IModelValidator validator) : base(validator)
        {
            LayerProperty = layerProperty;
            ProfileEditorService = profileEditorService;
            LayerProperty.Updated += LayerPropertyOnUpdated;
            UpdateInputValue();
        }

        public LayerProperty<T> LayerProperty { get; }
        public IProfileEditorService ProfileEditorService { get; }

        public bool InputDragging { get; private set; }

        public T InputValue
        {
            get => _inputValue;
            set
            {
                _inputValue = value;
                ApplyInputValue();
            }
        }

        public virtual void Dispose()
        {
            LayerProperty.Updated -= LayerPropertyOnUpdated;
        }

        protected virtual void OnInputValueApplied()
        {
        }

        protected virtual void OnInputValueChanged()
        {
        }

        protected void ApplyInputValue()
        {
            // Force the validator to run
            if (Validator != null)
                Validate();
            // Only apply the input value to the layer property if the validator found no errors
            if (!HasErrors)
                SetCurrentValue(_inputValue, !InputDragging);

            OnInputValueChanged();
            OnInputValueApplied();
        }

        private void LayerPropertyOnUpdated(object sender, EventArgs e)
        {
            UpdateInputValue();
        }

        private void UpdateInputValue()
        {
            // Avoid unnecessary UI updates and validator cycles
            if (_inputValue != null && _inputValue.Equals(LayerProperty.CurrentValue) || _inputValue == null && LayerProperty.CurrentValue == null)
                return;

            // Override the input value
            _inputValue = LayerProperty.CurrentValue;

            // Notify a change in the input value
            OnInputValueChanged();
            NotifyOfPropertyChange(nameof(InputValue));

            // Force the validator to run with the overridden value
            if (Validator != null)
                Validate();
        }


        #region Event handlers

        public void InputDragStarted(object sender, EventArgs e)
        {
            InputDragging = true;
        }

        public void InputDragEnded(object sender, EventArgs e)
        {
            InputDragging = false;
            ProfileEditorService.UpdateSelectedProfileElement();
        }

        private void SetCurrentValue(T value, bool saveChanges)
        {
            LayerProperty.SetCurrentValue(value, ProfileEditorService.CurrentTime);
            if (saveChanges)
                ProfileEditorService.UpdateSelectedProfileElement();
            else
                ProfileEditorService.UpdateProfilePreview();
        }

        #endregion
    }
}