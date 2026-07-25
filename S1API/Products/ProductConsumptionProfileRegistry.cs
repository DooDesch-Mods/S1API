using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Registers process-lifetime intrinsic consumption profiles for custom products.
    /// </summary>
    /// <remarks>
    /// Product IDs and product-kind IDs are case-insensitive. A product-specific profile takes
    /// deterministic precedence over a product-kind profile. Register matching providers on every
    /// multiplayer peer before custom-product manifest validation begins.
    /// </remarks>
    public static class ProductConsumptionProfileRegistry
    {
        /// <summary>
        /// Registers a profile for one stable custom product ID.
        /// </summary>
        /// <param name="productId">The stable, namespaced product ID.</param>
        /// <param name="profile">The immutable consumption profile.</param>
        /// <returns>The registered profile, or the existing profile on an idempotent call.</returns>
        public static ProductConsumptionProfile RegisterForProduct(
            string productId,
            ProductConsumptionProfile profile)
        {
            return ProductConsumptionProfileRegistrationRegistry.RegisterForProduct(
                productId,
                profile);
        }

        /// <summary>
        /// Registers a profile for every registered custom product of one logical product kind.
        /// </summary>
        /// <param name="productKind">The immutable logical product kind.</param>
        /// <param name="profile">The immutable consumption profile.</param>
        /// <returns>The registered profile, or the existing profile on an idempotent call.</returns>
        public static ProductConsumptionProfile RegisterForProductKind(
            ProductKind productKind,
            ProductConsumptionProfile profile)
        {
            if (productKind == null)
                throw new ArgumentNullException(nameof(productKind));

            return ProductConsumptionProfileRegistrationRegistry.RegisterForProductKind(
                productKind,
                profile);
        }
    }
}
