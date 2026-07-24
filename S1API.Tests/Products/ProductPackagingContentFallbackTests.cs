using System;
using S1API.Internal.Products;
using S1API.Products;
using Xunit;

namespace S1API.Tests.Products;

[Collection(CustomProductRegistryCollection.Name)]
public sealed class ProductPackagingContentFallbackTests : IDisposable
{
    public void Dispose() => ProductPackagingContentProfileRegistry.ResetForTesting();

    [Fact]
    public void LogicalKindFallbackResolvesForDynamicallyAllocatedGeneratedMixes()
    {
        string kindId = "moredrugs:mdma-" + Guid.NewGuid().ToString("N");
        var profile = new ProductPackagingContentProfileBuilder()
            .WithContent(() => null)
            .Build();

        ProductPackagingContentProfileRegistry.RegisterForProductKind(
            "moredrugs", kindId, "baggie", profile);

        Assert.True(ProductPackagingContentProfileRegistry.TryResolveForProductKind(
            kindId, "baggie", out ProductPackagingContentProfileRegistration? resolved));
        Assert.Same(profile, resolved!.Profile);
    }
}
