#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
using NativeStringList = Il2CppSystem.Collections.Generic.List<string>;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
using NativeStringList = System.Collections.Generic.List<string>;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Properties;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Properties.Interfaces;

namespace S1API.Products
{
    /// <summary>
    /// Builds marijuana-family weed variants through the game's native
    /// <c>CreateWeed_Server</c> lifecycle.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Build"/> after the save has loaded, such as from
    /// <c>GameLifecycle.OnLoadComplete</c>. Native creation owns product registration,
    /// pricing, discovery, icon generation, persistence, and network replication.
    /// </remarks>
    public sealed class WeedDefinitionBuilder
    {
        private static readonly Log Logger = new Log("WeedDefinitionBuilder");

        private readonly string _id;
        private readonly List<PropertyBase> _properties = new List<PropertyBase>();
        private string? _name;
        private WeedAppearanceSettings? _appearance;

        /// <summary>
        /// Creates a builder for a marijuana-family weed variant.
        /// </summary>
        /// <param name="id">A stable namespaced ID in the form <c>mod-id:product-id</c>.</param>
        public WeedDefinitionBuilder(string id)
        {
            _id = WeedDefinitionBuilderContract.NormalizeId(id);
        }

        /// <summary>
        /// Sets the display name used by the native product definition and save representation.
        /// </summary>
        /// <param name="name">The non-empty display name.</param>
        /// <returns>This builder.</returns>
        public WeedDefinitionBuilder WithName(string name)
        {
            _name = WeedDefinitionBuilderContract.NormalizeName(name);
            return this;
        }

        /// <summary>
        /// Adds one property to the weed variant.
        /// </summary>
        /// <param name="property">A vanilla property token or registered custom property.</param>
        /// <returns>This builder.</returns>
        public WeedDefinitionBuilder WithProperty(PropertyBase property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _properties.Add(property);
            return this;
        }

        /// <summary>
        /// Replaces the weed variant's properties.
        /// </summary>
        /// <param name="properties">Vanilla property tokens or registered custom properties.</param>
        /// <returns>This builder.</returns>
        public WeedDefinitionBuilder WithProperties(params PropertyBase[] properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            _properties.Clear();
            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i] == null)
                    throw new ArgumentException("Weed properties cannot contain null values.", nameof(properties));

                _properties.Add(properties[i]);
            }

            return this;
        }

        /// <summary>
        /// Sets explicit native weed colors. When omitted, the game generates an appearance
        /// from the configured properties.
        /// </summary>
        /// <param name="appearance">The four native weed color channels.</param>
        /// <returns>This builder.</returns>
        public WeedDefinitionBuilder WithAppearance(WeedAppearanceSettings appearance)
        {
            _appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
            return this;
        }

        /// <summary>
        /// Creates or resolves the weed variant through the native product creator.
        /// </summary>
        /// <returns>The existing S1API typed wrapper around the native weed definition.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called before the product runtime is available, when properties do not
        /// resolve, when the ID collides with another item family, or when native creation fails.
        /// </exception>
        public WeedDefinition Build()
        {
            var name = WeedDefinitionBuilderContract.NormalizeName(_name!);
            var productManager = S1Product.ProductManager.Instance;
            if (productManager == null)
            {
                throw new InvalidOperationException(
                    "Cannot build a weed product before ProductManager is available. " +
                    "Build from GameLifecycle.OnLoadComplete or later.");
            }

            if (S1Registry.Instance == null)
            {
                throw new InvalidOperationException(
                    "Cannot build a weed product before the item registry is available. " +
                    "Build from GameLifecycle.OnLoadComplete or later.");
            }

            var existingItem = S1Registry.GetItem(_id);
            if (existingItem != null)
                return WrapExisting(existingItem, true);

            var resolvedProperties = PropertyResolver.ResolveToGameProperties(_properties);
            WeedDefinitionBuilderContract.ValidatePropertyCounts(
                _properties.Count,
                resolvedProperties.Count);

            var propertyIds = new NativeStringList();
            for (var i = 0; i < resolvedProperties.Count; i++)
                propertyIds.Add(resolvedProperties[i].ID);

            productManager.CreateWeed_Server(
                name,
                _id,
                S1Product.EDrugType.Marijuana,
                propertyIds,
                _appearance?.ToNative());

            var createdItem = S1Registry.GetItem(_id);
            if (createdItem == null)
            {
                throw new InvalidOperationException(
                    $"The native product creator did not register weed product '{_id}'. " +
                    "Ensure Build() runs on a connected host/server after the save has loaded.");
            }

            return WrapExisting(createdItem, false);
        }

        private WeedDefinition WrapExisting(object item, bool isDuplicate)
        {
            if (CrossType.Is(item, out S1Product.WeedDefinition weedDefinition))
            {
                if (isDuplicate)
                {
                    Logger.Warning(
                        $"Weed product '{_id}' is already registered; returning the existing native definition.");
                }

                return new WeedDefinition(weedDefinition);
            }

            throw new InvalidOperationException(
                $"Item ID '{_id}' is already registered by a non-weed definition. " +
                "Choose a different namespaced ID.");
        }
    }
}
