﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Artemis.Core.Services;
using Artemis.UI.Ninject.Factories;
using Stylet;

namespace Artemis.UI.Screens.Settings.Tabs.Devices
{
    public class DeviceSettingsTabViewModel : Conductor<DeviceSettingsViewModel>.Collection.AllActive
    {
        private readonly ISettingsVmFactory _settingsVmFactory;
        private readonly ISurfaceService _surfaceService;

        public DeviceSettingsTabViewModel(ISurfaceService surfaceService, ISettingsVmFactory settingsVmFactory)
        {
            DisplayName = "DEVICES";

            _surfaceService = surfaceService;
            _settingsVmFactory = settingsVmFactory;
        }
        
        protected override void OnActivate()
        {
            // Take it off the UI thread to avoid freezing on tab change
            Task.Run(async  () =>
            {
                Items.Clear();
                await Task.Delay(200);

                List<DeviceSettingsViewModel> instances = _surfaceService.ActiveSurface.Devices.Select(d => _settingsVmFactory.CreateDeviceSettingsViewModel(d)).ToList();
                foreach (DeviceSettingsViewModel deviceSettingsViewModel in instances)
                    Items.Add(deviceSettingsViewModel);
            });

            base.OnActivate();
        }
    }
}