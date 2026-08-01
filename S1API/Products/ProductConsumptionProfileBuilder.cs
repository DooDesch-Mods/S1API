using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Builds an immutable intrinsic consumption profile for custom products.
    /// </summary>
    public sealed class ProductConsumptionProfileBuilder
    {
        private string? _providerId;
        private int _providerVersion;
        private Action<ProductConsumptionContext>? _onPlayerApply;
        private Action<ProductConsumptionContext>? _onPlayerClear;
        private Action<ProductConsumptionContext>? _onNpcApply;
        private Action<ProductConsumptionContext>? _onNpcClear;

        /// <summary>
        /// Sets the stable identity and compatibility version for this profile provider.
        /// </summary>
        /// <param name="providerId">A stable, namespaced provider identifier.</param>
        /// <param name="providerVersion">A positive compatibility version.</param>
        /// <returns>This builder for method chaining.</returns>
        public ProductConsumptionProfileBuilder WithProviderCompatibility(
            string providerId,
            int providerVersion)
        {
            if (providerVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(providerVersion));

            _providerId = ProductKindId.Normalize(providerId, nameof(providerId));
            _providerVersion = providerVersion;
            return this;
        }

        /// <summary>
        /// Sets behavior to run after ordinary effects apply to the local consuming player.
        /// </summary>
        /// <param name="callback">The callback to run for the local player.</param>
        /// <returns>This builder for method chaining.</returns>
        public ProductConsumptionProfileBuilder OnPlayerApply(
            Action<ProductConsumptionContext> callback)
        {
            _onPlayerApply = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }

        /// <summary>
        /// Sets behavior to run after ordinary effects clear from the local consuming player.
        /// </summary>
        /// <param name="callback">The callback to run for the local player.</param>
        /// <returns>This builder for method chaining.</returns>
        public ProductConsumptionProfileBuilder OnPlayerClear(
            Action<ProductConsumptionContext> callback)
        {
            _onPlayerClear = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }

        /// <summary>
        /// Sets behavior to run after ordinary effects apply to an NPC consumer.
        /// </summary>
        /// <param name="callback">The callback to run for the NPC consumer.</param>
        /// <returns>This builder for method chaining.</returns>
        public ProductConsumptionProfileBuilder OnNpcApply(
            Action<ProductConsumptionContext> callback)
        {
            _onNpcApply = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }

        /// <summary>
        /// Sets behavior to run after ordinary effects clear from an NPC consumer.
        /// </summary>
        /// <param name="callback">The callback to run for the NPC consumer.</param>
        /// <returns>This builder for method chaining.</returns>
        public ProductConsumptionProfileBuilder OnNpcClear(
            Action<ProductConsumptionContext> callback)
        {
            _onNpcClear = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }

        /// <summary>
        /// Builds the configured immutable consumption profile.
        /// </summary>
        /// <returns>The immutable consumption profile.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when provider compatibility or at least one lifecycle callback was not configured.
        /// </exception>
        public ProductConsumptionProfile Build()
        {
            if (_providerId == null)
            {
                throw new InvalidOperationException(
                    "WithProviderCompatibility must be called before Build().");
            }

            if (_onPlayerApply == null && _onPlayerClear == null &&
                _onNpcApply == null && _onNpcClear == null)
            {
                throw new InvalidOperationException(
                    "Configure at least one consumption lifecycle callback before Build().");
            }

            return new ProductConsumptionProfile(
                _providerId,
                _providerVersion,
                _onPlayerApply,
                _onPlayerClear,
                _onNpcApply,
                _onNpcClear);
        }
    }
}
