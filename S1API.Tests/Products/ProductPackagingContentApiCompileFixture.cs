using S1API.Products;
using UnityEngine;

namespace S1API.Tests.Products;

internal static class ProductPackagingContentApiCompileFixture
{
    internal static ProductPackagingContentProfile CompileRepresentativeCaller(
        GameObject contentPrefab,
        ProductPresentationTransform firstPlacement,
        params ProductPresentationTransform[] additionalPlacements)
    {
        ProductPackagingContentProfile profile =
            new ProductPackagingContentProfileBuilder()
                .WithContent(provider: () => contentPrefab)
                .AddPlacement(placement: firstPlacement)
                .AddPlacements(placements: additionalPlacements)
                .Build();

        return ProductPackagingContentProfileRegistry.Register(
            ownerId: "compile-fixture",
            productId: "compile-fixture:product",
            packagingId: "baggie",
            profile: profile);
    }
}
