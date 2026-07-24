using System;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Allocates stable IDs after the native mix-name sanitizer has run.</summary>
    internal static class CustomProductMixingIdentity
    {
        internal static string CreateGeneratedProductId(
            string sourceProductId,
            string nativeMixId)
        {
            string sourceId = ProductKindId.Normalize(sourceProductId, nameof(sourceProductId));
            if (string.IsNullOrWhiteSpace(nativeMixId))
                throw new InvalidOperationException("The native mix-name sanitizer produced an empty ID.");

            int separator = sourceId.IndexOf(':');
            string ownerId = sourceId.Substring(0, separator);
            string sourceName = sourceId.Substring(separator + 1);
            string nativeHash = CustomProductManifestData.ComputeHash(nativeMixId);
            return ProductKindId.Normalize(
                ownerId + ":mix/" + sourceName + "/" + nativeHash,
                nameof(nativeMixId));
        }

        internal static bool IsGeneratedIdForSource(string sourceProductId, string productId)
        {
            string sourceId = ProductKindId.Normalize(sourceProductId, nameof(sourceProductId));
            int separator = sourceId.IndexOf(':');
            string prefix = sourceId.Substring(0, separator) + ":mix/" +
                sourceId.Substring(separator + 1) + "/";
            return productId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
