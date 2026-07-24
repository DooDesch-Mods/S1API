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

        /// <summary>Creates a profile builder for a registered logical product kind.</summary>
        public ProductMixingProfileBuilder(ProductKind productKind)
        {
            _productKind = productKind ?? throw new ArgumentNullException(nameof(productKind));
        }

        /// <summary>Sets the supported native mixer-map family.</summary>
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

        /// <summary>Registers this immutable profile.</summary>
        public ProductMixingProfile Build()
        {
            if (_outputFactory == null)
                throw new InvalidOperationException("WithOutputFactory must be called before Build().");
            if (!_productKind.CompatibilityDrugType.HasValue)
                throw new InvalidOperationException("A mixing profile requires a product kind with a compatibility drug type.");
            if (_productKind.CompatibilityDrugType.Value != GetCompatibilityDrugType(_mixerMap))
            {
                throw new InvalidOperationException(
                    "The selected mixer map must match the product kind's compatibility drug type. " +
                    "This keeps custom logical kinds out of unsupported native enum switches.");
            }
            return ProductMixingProfileRegistry.Register(new ProductMixingProfile(_productKind, _mixerMap, _outputFactory));
        }

        private static DrugType GetCompatibilityDrugType(ProductMixingMap mixerMap)
        {
            switch (mixerMap)
            {
                case ProductMixingMap.Marijuana:
                    return DrugType.Marijuana;
                case ProductMixingMap.Methamphetamine:
                    return DrugType.Methamphetamine;
                case ProductMixingMap.Cocaine:
                    return DrugType.Cocaine;
                case ProductMixingMap.Shrooms:
                    return DrugType.Shrooms;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mixerMap));
            }
        }
    }
}
