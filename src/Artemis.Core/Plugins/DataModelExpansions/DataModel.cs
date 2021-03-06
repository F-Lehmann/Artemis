﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Artemis.Core.Modules;
using Humanizer;

namespace Artemis.Core.DataModelExpansions
{
    /// <summary>
    ///     Represents a data model that contains information on a game/application etc.
    /// </summary>
    public abstract class DataModel
    {
        private readonly Dictionary<string, DataModel> _dynamicDataModels = new Dictionary<string, DataModel>();

        /// <summary>
        ///     Creates a new instance of the <see cref="DataModel" /> class
        /// </summary>
        protected DataModel()
        {
            // These are both set right after construction to keep the constructor of inherited classes clean
            Feature = null!;
            DataModelDescription = null!;
        }

        /// <summary>
        ///     Gets the plugin feature this data model belongs to
        /// </summary>
        [DataModelIgnore]
        public DataModelPluginFeature Feature { get; internal set; }

        /// <summary>
        ///     Gets the <see cref="DataModelPropertyAttribute" /> describing this data model
        /// </summary>
        [DataModelIgnore]
        public DataModelPropertyAttribute DataModelDescription { get; internal set; }

        /// <summary>
        ///     Gets the is expansion status indicating whether this data model expands the main data model
        /// </summary>
        [DataModelIgnore]
        public bool IsExpansion { get; internal set; }

        /// <summary>
        ///     Gets an read-only dictionary of all dynamic data models
        /// </summary>
        [DataModelIgnore]
        public ReadOnlyDictionary<string, DataModel> DynamicDataModels => new ReadOnlyDictionary<string, DataModel>(_dynamicDataModels);

        /// <summary>
        ///     Returns a read-only collection of all properties in this datamodel that are to be ignored
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<PropertyInfo> GetHiddenProperties()
        {
            if (Feature is ProfileModule profileModule)
                return profileModule.HiddenProperties;
            if (Feature is BaseDataModelExpansion dataModelExpansion)
                return dataModelExpansion.HiddenProperties;

            return new List<PropertyInfo>().AsReadOnly();
        }

        /// <summary>
        ///     Adds a dynamic data model to this data model
        /// </summary>
        /// <param name="dynamicDataModel">The dynamic data model to add</param>
        /// <param name="key">The key of the child, must be unique to this data model</param>
        /// <param name="name">An optional name, if not provided the key will be used in a humanized form</param>
        /// <param name="description">An optional description</param>
        public T AddDynamicChild<T>(T dynamicDataModel, string key, string? name = null, string? description = null) where T : DataModel
        {
            if (dynamicDataModel == null)
                throw new ArgumentNullException(nameof(dynamicDataModel));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (key.Contains('.'))
                throw new ArtemisCoreException("The provided key contains an illegal character (.)");
            if (_dynamicDataModels.ContainsKey(key))
                throw new ArtemisCoreException($"Cannot add a dynamic data model with key '{key}' " +
                                               "because the key is already in use on by another dynamic property this data model.");

            if (_dynamicDataModels.ContainsValue(dynamicDataModel))
            {
                string existingKey = _dynamicDataModels.First(kvp => kvp.Value == dynamicDataModel).Key;
                throw new ArtemisCoreException($"Cannot add a dynamic data model with key '{key}' " +
                                               $"because the dynamic data model is already added with key '{existingKey}.");
            }

            if (GetType().GetProperty(key) != null)
                throw new ArtemisCoreException($"Cannot add a dynamic data model with key '{key}' " +
                                               "because the key is already in use by a static property on this data model.");

            dynamicDataModel.Feature = Feature;
            dynamicDataModel.DataModelDescription = new DataModelPropertyAttribute
            {
                Name = string.IsNullOrWhiteSpace(name) ? key.Humanize() : name,
                Description = description
            };
            _dynamicDataModels.Add(key, dynamicDataModel);

            OnDynamicDataModelAdded(new DynamicDataModelEventArgs(dynamicDataModel, key));
            return dynamicDataModel;
        }

        /// <summary>
        ///     Removes a dynamic data model from the data model by its key
        /// </summary>
        /// <param name="key">The key of the dynamic data model to remove</param>
        public void RemoveDynamicChildByKey(string key)
        {
            _dynamicDataModels.TryGetValue(key, out DataModel? childDataModel);
            if (childDataModel == null)
                return;

            _dynamicDataModels.Remove(key);
            OnDynamicDataModelRemoved(new DynamicDataModelEventArgs(childDataModel, key));
        }

        /// <summary>
        ///     Removes a dynamic data model from this data model
        /// </summary>
        /// <param name="dynamicDataModel">The dynamic data model to remove</param>
        public void RemoveDynamicChild(DataModel dynamicDataModel)
        {
            List<string> keys = _dynamicDataModels.Where(kvp => kvp.Value == dynamicDataModel).Select(kvp => kvp.Key).ToList();
            foreach (string key in keys)
            {
                _dynamicDataModels.Remove(key);
                OnDynamicDataModelRemoved(new DynamicDataModelEventArgs(dynamicDataModel, key));
            }
        }

        /// <summary>
        ///     Removes all dynamic data models from this data model
        /// </summary>
        public void ClearDynamicChildren()
        {
            while (_dynamicDataModels.Any())
                RemoveDynamicChildByKey(_dynamicDataModels.First().Key);
        }

        /// <summary>
        ///     Gets a dynamic data model of type <typeparamref name="T" /> by its key
        /// </summary>
        /// <typeparam name="T">The type of data model you expect</typeparam>
        /// <param name="key">The unique key of the dynamic data model</param>
        /// <returns>If found, the dynamic data model otherwise <c>null</c></returns>
        public T? DynamicChild<T>(string key) where T : DataModel
        {
            _dynamicDataModels.TryGetValue(key, out DataModel? value);
            return value as T;
        }

        #region Events

        /// <summary>
        ///     Occurs when a dynamic data model has been added to this data model
        /// </summary>
        public event EventHandler<DynamicDataModelEventArgs>? DynamicDataModelAdded;

        /// <summary>
        ///     Occurs when a dynamic data model has been removed from this data model
        /// </summary>
        public event EventHandler<DynamicDataModelEventArgs>? DynamicDataModelRemoved;

        /// <summary>
        ///     Invokes the <see cref="DynamicDataModelAdded" /> event
        /// </summary>
        protected virtual void OnDynamicDataModelAdded(DynamicDataModelEventArgs e)
        {
            DynamicDataModelAdded?.Invoke(this, e);
        }

        /// <summary>
        ///     Invokes the <see cref="DynamicDataModelRemoved" /> event
        /// </summary>
        protected virtual void OnDynamicDataModelRemoved(DynamicDataModelEventArgs e)
        {
            DynamicDataModelRemoved?.Invoke(this, e);
        }

        #endregion
    }
}