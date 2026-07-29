using System.Collections.Generic;
using S1API.Products;

namespace S1API.Internal.Products
{
    internal interface IProductKindMetadataRuntimeAdapter
    {
        void Apply(IReadOnlyList<ProductKindMetadata> metadata);
    }
}
