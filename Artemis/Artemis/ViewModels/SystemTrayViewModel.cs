﻿using System.Windows;
using Artemis.Events;
using Caliburn.Micro;

namespace Artemis.ViewModels
{
    public class SystemTrayViewModel : Screen, IHandle<ToggleEnabled>
    {
        private readonly ShellViewModel _shellViewModel;

        private readonly IWindowManager _windowManager;
        private bool _enabled;
        private string _toggleText;
        /*
         * NOTE: In this sample the system tray view-model doesn't receive any notification 
         * when the other window gets closed by pressing the top right 'x'.
         * Thus no property notification is invoked, and system tray context-menu appears 
         * out of sync, still allowing 'Hide' and disabling 'Show'.
         * Given the purpose of the sample - integrating Caliburn.Micro with WPF NotifyIcon -
         * sync'ing the two view-models is not of interest here.
         * */

        public SystemTrayViewModel(IWindowManager windowManager, ShellViewModel shellViewModel)
        {
            _windowManager = windowManager;
            _shellViewModel = shellViewModel;
            _shellViewModel.MainModel.Events.Subscribe(this);

            /*
            * By now Effects are added to the MainModel so we can savely start
            * This is done from here to make sure all UI elements listening to 
            * events will receive the first ToggleEnabled event
            * */
            _shellViewModel.MainModel.StartEffects();

            // TODO: Check if show on startup is enabled, if so, show window.
        }

        public bool CanShowWindow => !_shellViewModel.IsActive;

        public bool CanHideWindow => _shellViewModel.IsActive;

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value == _enabled) return;
                _enabled = value;

                ToggleText = _enabled ? "Disable Artemis" : "Enable Artemis";
                NotifyOfPropertyChange(() => Enabled);
            }
        }

        public string ToggleText
        {
            get { return _toggleText; }
            set
            {
                if (value == _toggleText) return;
                _toggleText = value;
                NotifyOfPropertyChange(() => ToggleText);
            }
        }

        public void Handle(ToggleEnabled message)
        {
            Enabled = message.Enabled;
        }

        public void ToggleEnabled()
        {
            if (Enabled)
                _shellViewModel.MainModel.ShutdownEffects();
            else
                _shellViewModel.MainModel.StartEffects();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            NotifyOfPropertyChange(() => CanShowWindow);
            NotifyOfPropertyChange(() => CanHideWindow);
        }

        public void ShowWindow()
        {
            if (!CanShowWindow)
                return;

            // manually show the next window view-model
            _windowManager.ShowWindow(_shellViewModel);

            NotifyOfPropertyChange(() => CanShowWindow);
            NotifyOfPropertyChange(() => CanHideWindow);
        }

        public void HideWindow()
        {
            if (!CanHideWindow)
                return;

            _shellViewModel.TryClose();

            NotifyOfPropertyChange(() => CanShowWindow);
            NotifyOfPropertyChange(() => CanHideWindow);
        }

        public void ExitApplication()
        {
            _shellViewModel.MainModel.ShutdownEffects();
            Application.Current.Shutdown();
        }
    }
}