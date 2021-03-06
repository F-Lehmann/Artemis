﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Artemis.Core;
using Artemis.Core.LayerBrushes;
using Artemis.Core.Services;
using Artemis.UI.Services.Interfaces;
using Artemis.UI.Shared;
using Artemis.UI.Shared.Services;
using Serilog.Events;
using Stylet;

namespace Artemis.UI.Screens.Settings.Tabs.General
{
    public class GeneralSettingsTabViewModel : Screen
    {
        private readonly IDebugService _debugService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;
        private List<Tuple<string, double>> _renderScales;
        private List<int> _sampleSizes;
        private List<Tuple<string, int>> _targetFrameRates;
        private readonly PluginSetting<LayerBrushReference> _defaultLayerBrushDescriptor;

        public GeneralSettingsTabViewModel(IDialogService dialogService, IDebugService debugService, ISettingsService settingsService, IPluginManagementService pluginManagementService)
        {
            DisplayName = "GENERAL";

            _dialogService = dialogService;
            _debugService = debugService;
            _settingsService = settingsService;

            LogLevels = new BindableCollection<ValueDescription>(EnumUtilities.GetAllValuesAndDescriptions(typeof(LogEventLevel)));
            ColorSchemes = new BindableCollection<ValueDescription>(EnumUtilities.GetAllValuesAndDescriptions(typeof(ApplicationColorScheme)));
            RenderScales = new List<Tuple<string, double>> {new Tuple<string, double>("10%", 0.1)};
            for (int i = 25; i <= 100; i += 25)
                RenderScales.Add(new Tuple<string, double>(i + "%", i / 100.0));

            TargetFrameRates = new List<Tuple<string, int>>();
            for (int i = 10; i <= 30; i += 5)
                TargetFrameRates.Add(new Tuple<string, int>(i + " FPS", i));

            // Anything else is kinda broken right now
            SampleSizes = new List<int> {1, 9};

            List<LayerBrushProvider> layerBrushProviders = pluginManagementService.GetFeaturesOfType<LayerBrushProvider>();

            LayerBrushDescriptors = new BindableCollection<LayerBrushDescriptor>(layerBrushProviders.SelectMany(l => l.LayerBrushDescriptors));
            _defaultLayerBrushDescriptor = _settingsService.GetSetting("ProfileEditor.DefaultLayerBrushDescriptor", new LayerBrushReference
            {
                LayerBrushProviderId = "Artemis.Plugins.LayerBrushes.Color.ColorBrushProvider-92a9d6ba",
                BrushType = "ColorBrush"
            });
        }

        public BindableCollection<LayerBrushDescriptor> LayerBrushDescriptors { get; }

        public LayerBrushDescriptor SelectedLayerBrushDescriptor
        {
            get => LayerBrushDescriptors.FirstOrDefault(d => d.MatchesLayerBrushReference(_defaultLayerBrushDescriptor.Value));
            set
            {
                _defaultLayerBrushDescriptor.Value = new LayerBrushReference(value);
                _defaultLayerBrushDescriptor.Save();
            }
        }

        public BindableCollection<ValueDescription> LogLevels { get; }
        public BindableCollection<ValueDescription> ColorSchemes { get; }

        public List<Tuple<string, int>> TargetFrameRates
        {
            get => _targetFrameRates;
            set => SetAndNotify(ref _targetFrameRates, value);
        }

        public List<Tuple<string, double>> RenderScales
        {
            get => _renderScales;
            set => SetAndNotify(ref _renderScales, value);
        }

        public List<int> SampleSizes
        {
            get => _sampleSizes;
            set => SetAndNotify(ref _sampleSizes, value);
        }

        public bool StartWithWindows
        {
            get => _settingsService.GetSetting("UI.AutoRun", false).Value;
            set
            {
                _settingsService.GetSetting("UI.AutoRun", false).Value = value;
                _settingsService.GetSetting("UI.AutoRun", false).Save();
                Task.Run(ApplyAutorun);
            }
        }

        public bool StartMinimized
        {
            get => !_settingsService.GetSetting("UI.ShowOnStartup", true).Value;
            set
            {
                _settingsService.GetSetting("UI.ShowOnStartup", true).Value = !value;
                _settingsService.GetSetting("UI.ShowOnStartup", true).Save();
            }
        }

        public bool ShowDataModelValues
        {
            get => _settingsService.GetSetting("ProfileEditor.ShowDataModelValues", false).Value;
            set
            {
                _settingsService.GetSetting("ProfileEditor.ShowDataModelValues", false).Value = value;
                _settingsService.GetSetting("ProfileEditor.ShowDataModelValues", false).Save();
            }
        }

        public Tuple<string, double> SelectedRenderScale
        {
            get => RenderScales.FirstOrDefault(s => Math.Abs(s.Item2 - RenderScale) < 0.01);
            set => RenderScale = value.Item2;
        }

        public Tuple<string, int> SelectedTargetFrameRate
        {
            get => TargetFrameRates.FirstOrDefault(t => Math.Abs(t.Item2 - TargetFrameRate) < 0.01);
            set => TargetFrameRate = value.Item2;
        }

        public LogEventLevel SelectedLogLevel
        {
            get => _settingsService.GetSetting("Core.LoggingLevel", LogEventLevel.Information).Value;
            set
            {
                _settingsService.GetSetting("Core.LoggingLevel", LogEventLevel.Information).Value = value;
                _settingsService.GetSetting("Core.LoggingLevel", LogEventLevel.Information).Save();
            }
        }

        public ApplicationColorScheme SelectedColorScheme
        {
            get => _settingsService.GetSetting("UI.ColorScheme", ApplicationColorScheme.Automatic).Value;
            set
            {
                _settingsService.GetSetting("UI.ColorScheme", ApplicationColorScheme.Automatic).Value = value;
                _settingsService.GetSetting("UI.ColorScheme", ApplicationColorScheme.Automatic).Save();
            }
        }

        public double RenderScale
        {
            get => _settingsService.GetSetting("Core.RenderScale", 0.5).Value;
            set
            {
                _settingsService.GetSetting("Core.RenderScale", 0.5).Value = value;
                _settingsService.GetSetting("Core.RenderScale", 0.5).Save();
            }
        }

        public int TargetFrameRate
        {
            get => _settingsService.GetSetting("Core.TargetFrameRate", 25).Value;
            set
            {
                _settingsService.GetSetting("Core.TargetFrameRate", 25).Value = value;
                _settingsService.GetSetting("Core.TargetFrameRate", 25).Save();
            }
        }

        public int SampleSize
        {
            get => _settingsService.GetSetting("Core.SampleSize", 1).Value;
            set
            {
                _settingsService.GetSetting("Core.SampleSize", 1).Value = value;
                _settingsService.GetSetting("Core.SampleSize", 1).Save();
            }
        }

        public void ShowDebugger()
        {
            _debugService.ShowDebugger();
        }

        public void ShowLogsFolder()
        {
            try
            {
                Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
            }
            catch (Exception e)
            {
                _dialogService.ShowExceptionDialog("Welp, we couldn\'t open the logs folder for you", e);
            }
        }

        public void ShowDataFolder()
        {
            try
            {
                Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", Constants.DataFolder);
            }
            catch (Exception e)
            {
                _dialogService.ShowExceptionDialog("Welp, we couldn\'t open the data folder for you", e);
            }
        }

        protected override void OnInitialActivate()
        {
            Task.Run(ApplyAutorun);
            base.OnInitialActivate();
        }

        private void ApplyAutorun()
        {
            try
            {
                string autoRunFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Artemis.lnk");
                string executableFile = Constants.ExecutablePath;

                if (File.Exists(autoRunFile))
                    File.Delete(autoRunFile);
                if (StartWithWindows)
                    ShortcutUtilities.Create(autoRunFile, executableFile, "--autorun", new FileInfo(executableFile).DirectoryName, "Artemis", "", "");
            }
            catch (Exception e)
            {
                _dialogService.ShowExceptionDialog("An exception occured while trying to apply the auto run setting", e);
                throw;
            }
        }
    }

    public enum ApplicationColorScheme
    {
        Light,
        Dark,
        Automatic
    }
}