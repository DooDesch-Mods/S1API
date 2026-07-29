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

    internal static ProductPackagingContentProfile CompileCompleteFilledVisualCaller(
        GameObject completeVisualPrefab,
        ProductPresentationTransform transform)
    {
        return new ProductPackagingContentProfileBuilder()
            .WithCompleteFilledVisual(
                provider: () => completeVisualPrefab,
                transform: transform)
            .Build();
    }

    internal static ProductPackagingContentProfile CompileNativeScaffoldCaller(
        Material modOwnedMaterial)
    {
        return new ProductPackagingContentProfileBuilder()
            .WithNativeFilledVisualScaffold(
                template: ProductPackagingVisualTemplate.Marijuana,
                customize: clone =>
                {
                    Renderer[] renderers =
                        clone.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < renderers.Length; i++)
                        renderers[i].sharedMaterial = modOwnedMaterial;
                })
            .Build();
    }
}
