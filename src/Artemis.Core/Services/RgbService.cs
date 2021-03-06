﻿using System;
using System.Collections.Generic;
using System.Linq;
using RGB.NET.Core;
using RGB.NET.Groups;
using Serilog;

namespace Artemis.Core.Services
{
    /// <summary>
    ///     Provides wrapped access the RGB.NET
    /// </summary>
    internal class RgbService : IRgbService
    {
        private readonly List<IRGBDevice> _loadedDevices;
        private readonly ILogger _logger;
        private readonly PluginSetting<double> _renderScaleSetting;
        private readonly PluginSetting<int> _sampleSizeSetting;
        private readonly PluginSetting<int> _targetFrameRateSetting;
        private ListLedGroup? _surfaceLedGroup;

        public RgbService(ILogger logger, ISettingsService settingsService)
        {
            _logger = logger;
            _renderScaleSetting = settingsService.GetSetting("Core.RenderScale", 0.5);
            _targetFrameRateSetting = settingsService.GetSetting("Core.TargetFrameRate", 25);
            _sampleSizeSetting = settingsService.GetSetting("Core.SampleSize", 1);

            Surface = RGBSurface.Instance;

            // Let's throw these for now
            Surface.Exception += SurfaceOnException;
            _renderScaleSetting.SettingChanged += RenderScaleSettingOnSettingChanged;
            _targetFrameRateSetting.SettingChanged += TargetFrameRateSettingOnSettingChanged;
            _loadedDevices = new List<IRGBDevice>();
            UpdateTrigger = new TimerUpdateTrigger {UpdateFrequency = 1.0 / _targetFrameRateSetting.Value};
            Surface.RegisterUpdateTrigger(UpdateTrigger);
        }

        /// <inheritdoc />
        public RGBSurface Surface { get; set; }

        public TimerUpdateTrigger UpdateTrigger { get; }
        public BitmapBrush? BitmapBrush { get; private set; }
        public IReadOnlyCollection<IRGBDevice> LoadedDevices => _loadedDevices.AsReadOnly();
        public double RenderScale => _renderScaleSetting.Value;
        public bool IsRenderPaused { get; set; }

        public void AddDeviceProvider(IRGBDeviceProvider deviceProvider)
        {
            try
            {
                Surface.LoadDevices(deviceProvider, RGBDeviceType.All, false, true);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception during device loading for device provider {deviceProvider}", deviceProvider.GetType().Name);
                throw;
            }

            if (deviceProvider.Devices == null || !deviceProvider.Devices.Any())
            {
                _logger.Warning("Device provider {deviceProvider} has no devices", deviceProvider.GetType().Name);
                return;
            }

            foreach (IRGBDevice surfaceDevice in deviceProvider.Devices)
            {
                _logger.Debug("Device provider {deviceProvider} added {deviceName}",
                    deviceProvider.GetType().Name, surfaceDevice.DeviceInfo?.DeviceName);
                if (!_loadedDevices.Contains(surfaceDevice))
                {
                    _loadedDevices.Add(surfaceDevice);
                    OnDeviceLoaded(new DeviceEventArgs(surfaceDevice));
                }
                else
                    OnDeviceReloaded(new DeviceEventArgs(surfaceDevice));
            }
        }

        public void Dispose()
        {
            Surface.UnregisterUpdateTrigger(UpdateTrigger);

            UpdateTrigger.Dispose();
            Surface.Dispose();
        }

        private void RenderScaleSettingOnSettingChanged(object? sender, EventArgs e)
        {
            UpdateSurfaceLedGroup();
        }

        private void TargetFrameRateSettingOnSettingChanged(object? sender, EventArgs e)
        {
            UpdateTrigger.UpdateFrequency = 1.0 / _targetFrameRateSetting.Value;
        }

        private void SurfaceOnException(ExceptionEventArgs args)
        {
            _logger.Warning("Surface threw e");
            throw args.Exception;
        }

        #region Events

        public event EventHandler<DeviceEventArgs>? DeviceLoaded;
        public event EventHandler<DeviceEventArgs>? DeviceReloaded;

        public void UpdateSurfaceLedGroup()
        {
            if (_surfaceLedGroup == null || BitmapBrush == null)
            {
                // Apply the application wide brush and decorator
                BitmapBrush = new BitmapBrush(new Scale(_renderScaleSetting.Value), _sampleSizeSetting);
                _surfaceLedGroup = new ListLedGroup(Surface.Leds) {Brush = BitmapBrush};
                return;
            }

            lock (_surfaceLedGroup)
            {
                // Clean up the old background
                _surfaceLedGroup.Detach();

                // Apply the application wide brush and decorator
                BitmapBrush.Scale = new Scale(_renderScaleSetting.Value);
                _surfaceLedGroup = new ListLedGroup(Surface.Leds) {Brush = BitmapBrush};
            }
        }

        private void OnDeviceLoaded(DeviceEventArgs e)
        {
            DeviceLoaded?.Invoke(this, e);
        }

        private void OnDeviceReloaded(DeviceEventArgs e)
        {
            DeviceReloaded?.Invoke(this, e);
        }

        #endregion
    }
}