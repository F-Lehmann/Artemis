﻿using System.Windows;
using MaterialDesignThemes.Wpf;
using Stylet;

namespace Artemis.UI.Shared.Services
{
    internal class DialogViewModelHost : PropertyChangedBase
    {
        private readonly IViewManager _viewManager;

        public DialogViewModelHost(IViewManager viewManager)
        {
            _viewManager = viewManager;
        }

        public DialogViewModelBase? ActiveDialogViewModel { get; set; }
        public bool IsOpen { get; set; }

        public void OpenDialog(DialogViewModelBase viewModel, string dialogIdentifier)
        {
            UIElement view = _viewManager.CreateViewForModel(viewModel);
            DialogHost.Show(view, dialogIdentifier, viewModel.OnDialogClosed);
            viewModel.DialogViewModelHost = this;

            ActiveDialogViewModel = viewModel;
            IsOpen = true;
        }
    }
}