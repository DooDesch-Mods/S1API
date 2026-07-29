using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Builds and registers an opt-in mixing profile for one logical product kind.
    /// </summary>
    public sealed class ProductMixingProfileBuilder
    {
        private readonly ProductKind _productKind;
        private ProductMixingMap _mixerMap;
        private Func<ProductMixingOutput, ProductMixingOutputDefinition>? _outputFactory;
        private string? _outputFactoryIdentity;
        private int _outputFactoryVersion;

        /// <summary>Creates a profile builder for a registered logical product kind.</summary>
        public ProductMixingProfileBuilder(ProductKind productKind)
        {
            _productKind = productKind ?? throw new ArgumentNullException(nameof(productKind));
        }

        /// <summary>
        /// Sets the native mixer-map execution strategy.
        /// </summary>
        /// <remarks>
        /// This does not alter the logical product kind or require it to have a matching vanilla
        /// drug type. The selected map is only the explicit native seam used while mixing.
        /// </remarks>
        public ProductMixingProfileBuilder WithMixerMap(ProductMixingMap mixerMap)
        {
            if (!Enum.IsDefined(typeof(ProductMixingMap), mixerMap))
                throw new ArgumentOutOfRangeException(nameof(mixerMap));
            _mixerMap = mixerMap;
            return this;
        }

        /// <summary>Sets the deterministic factory used to name, price, and optionally transform generated outputs.</summary>
        public ProductMixingProfileBuilder WithOutputFactory(Func<ProductMixingOutput, ProductMixingOutputDefinition> outputFactory)
        {
            _outputFactory = outputFactory ?? throw new ArgumentNullException(nameof(outputFactory));
            return this;
        }

        /// <summary>
        /// Sets the stable compatibility identity for the configured output factory.
        /// </summary>
        /// <remarks>
        /// Register the same identity and version on every peer. This identity is included in the
        /// multiplayer manifest so different output behavior is rejected before a mix can diverge.
        /// </remarks>
        public ProductMixingProfileBuilder WithOutputFactoryCompatibility(
            string identity,
            int version)
        {
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("Output-factory compatibility identity cannot be empty or whitespace.", nameof(identity));
            if (version < 0)
                throw new ArgumentOutOfRangeException(nameof(version));

            _outputFactoryIdentity = ProductKindId.Normalize(identity, nameof(identity));
            _outputFactoryVersion = version;
            return this;
        }

        /// <summary>Registers this immutable profile.</summary>
        public ProductMixingProfile Build()
        {
            if (_outputFactory == null)
                throw new InvalidOperationException("WithOutputFactory must be called before Build().");
            string compatibilityIdentity = _outputFactoryIdentity ??
                "s1api:factory/" + CustomProductManifestData.ComputeHash(
                    (_outputFactory.Method.DeclaringType?.AssemblyQualifiedName ?? string.Empty) +
                    "|" + _outputFactory.Method.Name);
            return ProductMixingProfileRegistry.Register(new ProductMixingProfile(
                _productKind,
                _mixerMap,
                _outputFactory,
                compatibilityIdentity,
                _outputFactoryVersion));
        }
    }
}
