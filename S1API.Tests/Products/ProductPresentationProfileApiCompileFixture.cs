using S1API.Products;
using UnityEngine;

namespace S1API.Tests.Products;

internal static class ProductPresentationProfileApiCompileFixture
{
    internal static ProductPresentationProfile CompileRepresentativeCaller(
        string ownerId,
        string productId,
        ProductKind productKind,
        Func<GameObject?> looseVisual,
        Func<GameObject?> contextVisual,
        Func<Sprite?> icon,
        Func<GameObject?> consumption)
    {
        ProductPresentationProfile profile =
            new ProductPresentationProfileBuilder()
                .WithLooseVisual(looseVisual)
                .WithStoredVisual(contextVisual)
                .WithHeldVisual(contextVisual)
                .WithStationVisual(contextVisual)
                .WithFunctionalProductVisual(contextVisual)
                .WithFunctionalProductConvexMeshColliders()
                .WithIcon(icon)
                .WithConsumptionPrefab(consumption)
                .Require(
                    ProductPresentationContext.Loose,
                    ProductPresentationContext.Stored,
                    ProductPresentationContext.Held,
                    ProductPresentationContext.Station,
                    ProductPresentationContext.FunctionalProduct,
                    ProductPresentationContext.Icon,
                    ProductPresentationContext.Consumption)
                .Build();

        _ = ProductPresentationProfileRegistry.RegisterForProduct(
            ownerId,
            productId,
            profile);
        _ = ProductPresentationProfileRegistry.RegisterForProductKind(
            ownerId,
            productKind,
            profile);
        return profile;
    }

    internal static ProductPresentationProfile CompileGeneratedIconCaller(
        Func<GameObject?> looseVisual)
    {
        var presentationTransform =
            new ProductPresentationTransform(
                Vector3.zero,
                new Vector3(0f, 90f, 0f),
                Vector3.one * 0.1f);
        return new ProductPresentationProfileBuilder()
            .WithLooseVisual(looseVisual, presentationTransform)
            .WithGeneratedIconFromLooseVisual(
                512,
                fitToCamera: true,
                cameraFill: 0.8f)
            .WithGeneratedIconTransform(
                new ProductPresentationTransform(
                    Vector3.zero,
                    new Vector3(45f, 0f, 0f),
                    Vector3.one))
            .Build();
    }
}
