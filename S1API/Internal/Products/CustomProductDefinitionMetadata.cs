using System;
using System.Collections.Generic;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Retains S1API-specific metadata for a generic custom product.
    /// Native save and network payloads continue to use the product definition's stable ID.
    /// </summary>
    internal sealed class CustomProductDefinitionMetadata
    {
        internal CustomProductDefinitionMetadata(
            ProductKind productKind,
            Quality defaultQuality)
            : this(
                productKind,
                defaultQuality,
                Array.Empty<PackagingDefinition>())
        {
        }

        internal CustomProductDefinitionMetadata(
            ProductKind productKind,
            Quality defaultQuality,
            IReadOnlyList<PackagingDefinition> validPackaging)
        {
            ProductKind = productKind;
            DefaultQuality = defaultQuality;
            if (validPackaging == null)
                throw new ArgumentNullException(nameof(validPackaging));

            ValidPackaging =
                new List<PackagingDefinition>(validPackaging).AsReadOnly();
        }

        internal ProductKind ProductKind { get; }

        internal Quality DefaultQuality { get; }

        internal IReadOnlyList<PackagingDefinition> ValidPackaging { get; }
    }
}
