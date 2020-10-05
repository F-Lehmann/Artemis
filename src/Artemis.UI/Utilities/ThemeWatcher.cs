﻿using System;
using System.Globalization;
using System.Management;
using System.Security.Principal;
using Artemis.UI.Events;
using Microsoft.Win32;

namespace Artemis.UI.Utilities
{
    public class ThemeWatcher
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        private const string RegistryValueName = "AppsUseLightTheme";

        public ThemeWatcher()
        {
            WatchTheme();
        }

        public void WatchTheme()
        {
            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            string query = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                currentUser.User.Value,
                RegistryKeyPath.Replace(@"\", @"\\"),
                RegistryValueName);

            try
            {
                ManagementEventWatcher watcher = new ManagementEventWatcher(query);
                watcher.EventArrived += (sender, args) =>
                {
                    WindowsTheme newWindowsTheme = GetWindowsTheme();
                    OnThemeChanged(new WindowsThemeEventArgs(newWindowsTheme));
                };

                // Start listening for events
                watcher.Start();
            }
            catch (Exception)
            {
                // This can fail on Windows 7
            }
        }

        public WindowsTheme GetWindowsTheme()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                object registryValueObject = key?.GetValue(RegistryValueName);
                if (registryValueObject == null) return WindowsTheme.Light;

                int registryValue = (int) registryValueObject;

                return registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
            }
        }

        public event EventHandler<WindowsThemeEventArgs> ThemeChanged;


        protected virtual void OnThemeChanged(WindowsThemeEventArgs e)
        {
            ThemeChanged?.Invoke(this, e);
        }

        public enum WindowsTheme
        {
            Light,
            Dark
        }
    }
}