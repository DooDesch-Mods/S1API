using System;

namespace S1API.Products
{
    /// <summary>
    /// Defines intrinsic consumption behavior for registered custom products.
    /// </summary>
    /// <remarks>
    /// Profiles are immutable. Their callbacks run in addition to ordinary product-property effects
    /// and only for product definitions registered through S1API.
    /// </remarks>
    public sealed class ProductConsumptionProfile
    {
        internal ProductConsumptionProfile(
            string providerId,
            int providerVersion,
            Action<ProductConsumptionContext>? onPlayerApply,
            Action<ProductConsumptionContext>? onPlayerClear,
            Action<ProductConsumptionContext>? onNpcApply,
            Action<ProductConsumptionContext>? onNpcClear)
        {
            ProviderId = providerId;
            ProviderVersion = providerVersion;
            OnPlayerApply = onPlayerApply;
            OnPlayerClear = onPlayerClear;
            OnNpcApply = onNpcApply;
            OnNpcClear = onNpcClear;
        }

        /// <summary>
        /// Gets the stable, namespaced identity of the provider that owns this behavior.
        /// </summary>
        public string ProviderId { get; }

        /// <summary>
        /// Gets the provider-defined compatibility version for this behavior.
        /// </summary>
        public int ProviderVersion { get; }

        internal Action<ProductConsumptionContext>? OnPlayerApply { get; }

        internal Action<ProductConsumptionContext>? OnPlayerClear { get; }

        internal Action<ProductConsumptionContext>? OnNpcApply { get; }

        internal Action<ProductConsumptionContext>? OnNpcClear { get; }
    }
}
