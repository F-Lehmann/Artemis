﻿using System;
using Artemis.Core.Models.Profile;
using Artemis.Core.Plugins.Exceptions;
using Artemis.Core.Services.Interfaces;

namespace Artemis.Core.Plugins.LayerBrush.Abstract
{
    /// <summary>
    ///     For internal use only, please use <see cref="LayerBrush{T}" /> or <see cref="RgbNetLayerBrush{T}" /> or instead
    /// </summary>
    public abstract class PropertiesLayerBrush<T> : BaseLayerBrush where T : LayerPropertyGroup
    {
        private T _properties;

        /// <summary>
        ///     Gets whether all properties on this brush are initialized
        /// </summary>
        public bool PropertiesInitialized { get; internal set; }

        /// <inheritdoc />
        public override LayerPropertyGroup BaseProperties => Properties;

        /// <summary>
        ///     Gets the properties of this brush.
        /// </summary>
        public T Properties
        {
            get
            {
                // I imagine a null reference here can be confusing, so lets throw an exception explaining what to do
                if (_properties == null)
                    throw new ArtemisPluginException("Cannot access brush properties until OnPropertiesInitialized has been called");
                return _properties;
            }
            internal set => _properties = value;
        }

        internal void InitializeProperties(ILayerService layerService)
        {
            Properties = Activator.CreateInstance<T>();
            Properties.LayerBrush = this;
            Properties.InitializeProperties(layerService, Layer, "LayerBrush.");
            PropertiesInitialized = true;

            EnableLayerBrush();
        }
    }
}