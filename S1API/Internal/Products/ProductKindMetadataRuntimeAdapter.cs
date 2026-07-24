using System.Collections.Generic;
using S1API.Products;

namespace S1API.Internal.Products
{
    internal sealed class ProductKindMetadataRuntimeAdapter :
        IProductKindMetadataRuntimeAdapter
    {
        internal static readonly ProductKindMetadataRuntimeAdapter Instance =
            new ProductKindMetadataRuntimeAdapter();

        private ProductKindMetadataRuntimeAdapter()
        {
        }

        public void Apply(IReadOnlyList<ProductKindMetadata> metadata)
        {
            ProductKindNativeMetadataRuntime.Apply(metadata);
            ProductManagerUiRuntime.ApplyCurrent(metadata);
        }
    }
}
