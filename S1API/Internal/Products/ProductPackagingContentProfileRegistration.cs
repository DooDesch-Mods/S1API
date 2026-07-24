using System;
using S1API.Products;

namespace S1API.Internal.Products
{
    internal readonly struct ProductPackagingContentKey :
        IEquatable<ProductPackagingContentKey>
    {
        internal ProductPackagingContentKey(string productId, string packagingId)
        {
            ProductId = productId;
            PackagingId = packagingId;
        }

        internal string ProductId { get; }

        internal string PackagingId { get; }

        public bool Equals(ProductPackagingContentKey other)
        {
            return string.Equals(
                       ProductId,
                       other.ProductId,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       PackagingId,
                       other.PackagingId,
                       StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is ProductPackagingContentKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (StringComparer.OrdinalIgnoreCase.GetHashCode(ProductId) * 397) ^
                    StringComparer.OrdinalIgnoreCase.GetHashCode(PackagingId);
            }
        }
    }

    internal sealed class ProductPackagingContentProfileRegistration
    {
        internal ProductPackagingContentProfileRegistration(
            string ownerId,
            ProductPackagingContentKey key,
            ProductPackagingContentProfile profile)
        {
            OwnerId = ownerId;
            Key = key;
            Profile = profile;
        }

        internal string OwnerId { get; }

        internal ProductPackagingContentKey Key { get; }

        internal ProductPackagingContentProfile Profile { get; }
    }
}
