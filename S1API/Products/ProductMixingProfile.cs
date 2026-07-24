using System;

namespace S1API.Products
{
    /// <summary>
    /// Opts a logical product kind into native mixing with a supported map and deterministic output factory.
    /// </summary>
    public sealed class ProductMixingProfile
    {
        internal ProductMixingProfile(ProductKind productKind, ProductMixingMap mixerMap, Func<ProductMixingOutput, ProductMixingOutputDefinition> outputFactory)
        {
            ProductKind = productKind;
            MixerMap = mixerMap;
            OutputFactory = outputFactory;
        }

        /// <summary>Gets the opted-in logical product kind.</summary>
        public ProductKind ProductKind { get; }
        /// <summary>Gets the supported native mixer-map family selected for this kind.</summary>
        public ProductMixingMap MixerMap { get; }
        internal Func<ProductMixingOutput, ProductMixingOutputDefinition> OutputFactory { get; }
    }
}
