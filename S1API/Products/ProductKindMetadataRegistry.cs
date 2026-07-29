using System;
using System.Collections.Generic;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Provides process-lifetime lookup access to registered product-kind metadata.
    /// </summary>
    public static class ProductKindMetadataRegistry
    {
        /// <summary>
        /// Gets a read-only snapshot ordered by custom-section order, display name, and identifier.
        /// </summary>
        public static IReadOnlyCollection<ProductKindMetadata> All =>
            ProductKindMetadataRegistrationRegistry.All;

        /// <summary>
        /// Gets registered metadata by product-kind identifier.
        /// </summary>
        /// <param name="productKindId">The stable, namespaced product-kind identifier.</param>
        /// <returns>The metadata, or <see langword="null"/> when none is registered.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="productKindId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="productKindId"/> is not a valid namespaced identifier.
        /// </exception>
        public static ProductKindMetadata? Get(string productKindId)
        {
            TryGet(productKindId, out ProductKindMetadata? metadata);
            return metadata;
        }

        /// <summary>
        /// Gets registered metadata for a logical product kind.
        /// </summary>
        /// <param name="productKind">The logical product kind.</param>
        /// <returns>The metadata, or <see langword="null"/> when none is registered.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="productKind"/> is <see langword="null"/>.
        /// </exception>
        public static ProductKindMetadata? Get(ProductKind productKind)
        {
            if (productKind == null)
                throw new ArgumentNullException(nameof(productKind));
            return Get(productKind.Id);
        }

        /// <summary>
        /// Attempts to get registered metadata by product-kind identifier.
        /// </summary>
        /// <param name="productKindId">The stable, namespaced product-kind identifier.</param>
        /// <param name="metadata">The metadata when found; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when metadata is registered.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="productKindId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="productKindId"/> is not a valid namespaced identifier.
        /// </exception>
        public static bool TryGet(
            string productKindId,
            out ProductKindMetadata? metadata)
        {
            return ProductKindMetadataRegistrationRegistry.TryGet(productKindId, out metadata);
        }

        /// <summary>
        /// Attempts to get registered metadata for a logical product kind.
        /// </summary>
        /// <param name="productKind">The logical product kind.</param>
        /// <param name="metadata">The metadata when found; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when metadata is registered.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="productKind"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGet(
            ProductKind productKind,
            out ProductKindMetadata? metadata)
        {
            if (productKind == null)
                throw new ArgumentNullException(nameof(productKind));
            return TryGet(productKind.Id, out metadata);
        }

        internal static ProductKindMetadata Register(ProductKindMetadata metadata)
        {
            return ProductKindMetadataRegistrationRegistry.Register(metadata);
        }
    }
}
