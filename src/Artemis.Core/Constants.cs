﻿using System;
using System.Collections.Generic;
using System.IO;
using Artemis.Core.JsonConverters;
using Artemis.Storage.Entities.Plugins;
using Newtonsoft.Json;

namespace Artemis.Core
{
    /// <summary>
    ///     A few useful constant values
    /// </summary>
    public static class Constants
    {
        /// <summary>
        ///     The full path to the Artemis application folder
        /// </summary>
        public static readonly string ApplicationFolder = Path.GetDirectoryName(typeof(Constants).Assembly.Location)!;

        /// <summary>
        ///     The full path to the Artemis executable
        /// </summary>
        public static readonly string ExecutablePath = Utilities.GetCurrentLocation();

        /// <summary>
        ///     The full path to the Artemis data folder
        /// </summary>
        public static readonly string DataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Artemis\\";

        /// <summary>
        ///     The connection string used to connect to the database
        /// </summary>
        public static readonly string ConnectionString = $"FileName={DataFolder}\\database.db";

        /// <summary>
        ///     The plugin info used by core components of Artemis
        /// </summary>
        public static readonly PluginInfo CorePluginInfo = new PluginInfo
        {
            Guid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Name = "Artemis Core", Version = new Version(2, 0)
        };

        /// <summary>
        ///     The plugin used by core components of Artemis
        /// </summary>
        public static readonly Plugin CorePlugin = new Plugin(CorePluginInfo, new DirectoryInfo(ApplicationFolder), null);

        internal static readonly CorePluginFeature CorePluginFeature = new CorePluginFeature {Plugin = CorePlugin};
        internal static readonly EffectPlaceholderPlugin EffectPlaceholderPlugin = new EffectPlaceholderPlugin {Plugin = CorePlugin};

        internal static JsonSerializerSettings JsonConvertSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> {new SKColorConverter(), new ForgivingIntConverter()}
        };
        internal static JsonSerializerSettings JsonConvertTypedSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Converters = new List<JsonConverter> { new SKColorConverter(), new ForgivingIntConverter() }
        };

        /// <summary>
        ///     A read-only collection containing all primitive numeric types
        /// </summary>
        public static IReadOnlyCollection<Type> NumberTypes = new List<Type>
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        /// <summary>
        ///     A read-only collection containing all primitive integral numeric types
        /// </summary>
        public static IReadOnlyCollection<Type> IntegralNumberTypes = new List<Type>
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };

        /// <summary>
        ///     A read-only collection containing all primitive floating-point numeric types
        /// </summary>
        public static IReadOnlyCollection<Type> FloatNumberTypes = new List<Type>
        {
            typeof(float),
            typeof(double),
            typeof(decimal)
        };
    }
}