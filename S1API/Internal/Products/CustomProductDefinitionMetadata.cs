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
        {
            ProductKind = productKind;
            DefaultQuality = defaultQuality;
        }

        internal ProductKind ProductKind { get; }

        internal Quality DefaultQuality { get; }
    }
}
