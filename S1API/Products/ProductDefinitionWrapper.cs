using System;
using S1API.Internal.Utils;
#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

namespace S1API.Products
{
    /// <summary>
    /// Provides functionality to wrap and convert generic product definitions into their specific type-derived definitions.
    /// </summary>
    public static class ProductDefinitionWrapper
    {
        /// <summary>
        /// Converts a generic <see cref="ProductDefinition"/> into its corresponding typed wrapper.
        /// </summary>
        /// <param name="def">The raw product definition to be processed and converted.</param>
        /// <returns>A wrapped instance of <see cref="ProductDefinition"/> with type-specific methods and properties, or the input definition if no specific wrapper applies.</returns>
        public static ProductDefinition Wrap(ProductDefinition def)
        {
            return Wrap(def.S1ProductDefinition, def);
        }

        /// <summary>
        /// INTERNAL: Creates the most specific API wrapper for a native product definition.
        /// </summary>
        /// <param name="definition">The native product definition to wrap.</param>
        /// <returns>The most specific available product definition wrapper.</returns>
        internal static ProductDefinition Wrap(S1Product.ProductDefinition definition)
        {
            return Wrap(definition, null);
        }

        private static ProductDefinition Wrap(
            S1Product.ProductDefinition definition,
            ProductDefinition? fallback)
        {
            if (CrossType.Is<S1Product.WeedDefinition>(definition, out var weed))
                return new WeedDefinition(weed);

            if (CrossType.Is<S1Product.MethDefinition>(definition, out var meth))
                return new MethDefinition(meth);

            if (CrossType.Is<S1Product.CocaineDefinition>(definition, out var coke))
                return new CocaineDefinition(coke);

            if (CrossType.Is<S1Product.ShroomDefinition>(definition, out var shroom))
                return new ShroomDefinition(shroom);

            return fallback ?? new ProductDefinition(definition);
        }
    }
}
