using S1API.Products;

namespace S1API.Tests.Products;

internal static class ProductConsumptionProfileApiCompileFixture
{
    internal static ProductConsumptionProfile CompileRepresentativeCaller(
        string productId,
        ProductKind productKind)
    {
        ProductConsumptionProfile profile = new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility("example:consumption", 1)
            .OnPlayerApply(UsePlayerContext)
            .OnPlayerClear(UsePlayerContext)
            .OnNpcApply(UseNpcContext)
            .OnNpcClear(UseNpcContext)
            .Build();

        _ = ProductConsumptionProfileRegistry.RegisterForProduct(productId, profile);
        _ = ProductConsumptionProfileRegistry.RegisterForProductKind(productKind, profile);
        return profile;
    }

    private static void UsePlayerContext(ProductConsumptionContext context)
    {
        _ = context.Product;
        _ = context.Definition;
        _ = context.ProductId;
        _ = context.ProductKind;
        _ = context.Quality;
        _ = context.Properties;
        _ = context.Player;
        _ = context.TargetId;
        _ = context.IsLocalPlayer;
    }

    private static void UseNpcContext(ProductConsumptionContext context)
    {
        _ = context.NPC;
        _ = context.TargetId;
    }
}
