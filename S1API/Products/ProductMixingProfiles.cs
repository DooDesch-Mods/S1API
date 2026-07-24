using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Provides lookup for opt-in custom product mixing profiles.
    /// </summary>
    public static class ProductMixingProfiles
    {
        /// <summary>Gets a profile for a logical product kind, or null when mixing was not opted in.</summary>
        public static ProductMixingProfile? Get(ProductKind productKind)
        {
            if (productKind == null)
                throw new ArgumentNullException(nameof(productKind));
            return Get(productKind.Id);
        }

        /// <summary>Gets a profile by logical product-kind ID, or null when mixing was not opted in.</summary>
        public static ProductMixingProfile? Get(string productKindId)
        {
            ProductMixingProfileRegistry.TryGet(productKindId, out ProductMixingProfile? profile);
            return profile;
        }

        internal static bool TryGet(string productKindId, out ProductMixingProfile? profile) =>
            ProductMixingProfileRegistry.TryGet(productKindId, out profile);
    }
}
