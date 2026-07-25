using S1API.Internal.Patches;
using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class ProductPackagingContentRuntimeContractTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void LoadingScreenWaitsForEitherProductIconQueue(
        bool looseIconWorkComplete,
        bool packagingIconWorkComplete,
        bool expected)
    {
        Assert.Equal(
            expected,
            LoadingScreenPatches.HasOutstandingProductIconWork(
                looseIconWorkComplete,
                packagingIconWorkComplete));
    }

    [Fact]
    public void OwnedRootIdentityIsCaseInsensitiveForTheRegisteredPair()
    {
        string first =
            ProductPackagingContentRuntime.GetGeneratedRootNameForTesting(
                "example.mod:heart-pill",
                "baggie");
        string repeated =
            ProductPackagingContentRuntime.GetGeneratedRootNameForTesting(
                "EXAMPLE.MOD:HEART-PILL",
                "BAGGIE");

        Assert.Equal(first, repeated);
        Assert.StartsWith("S1API_PackagingContent_", first);
        Assert.DoesNotContain("/", first);
    }

    [Fact]
    public void OwnedRootIdentityIncludesProductAndPackaging()
    {
        string baggie =
            ProductPackagingContentRuntime.GetGeneratedRootNameForTesting(
                "example.mod:heart-pill",
                "baggie");
        string jar =
            ProductPackagingContentRuntime.GetGeneratedRootNameForTesting(
                "example.mod:heart-pill",
                "jar");
        string otherProduct =
            ProductPackagingContentRuntime.GetGeneratedRootNameForTesting(
                "example.mod:other-pill",
                "baggie");

        Assert.NotEqual(baggie, jar);
        Assert.NotEqual(baggie, otherProduct);
    }

    [Theory]
    [InlineData(
        "example.mod:heart-pill",
        "baggie",
        "EXAMPLE.MOD:HEART-PILL",
        "BAGGIE",
        true)]
    [InlineData(
        "example.mod:other-pill",
        "baggie",
        "example.mod:heart-pill",
        "baggie",
        false)]
    [InlineData(
        "example.mod:heart-pill",
        "jar",
        "example.mod:heart-pill",
        "baggie",
        false)]
    [InlineData(
        null,
        "baggie",
        "example.mod:heart-pill",
        "baggie",
        false)]
    public void DeferredIconRefreshMatchesOnlyItsRegisteredPair(
        string? productId,
        string? packagingId,
        string registeredProductId,
        string registeredPackagingId,
        bool expected)
    {
        Assert.Equal(
            expected,
            ProductPackagingContentRuntime.MatchesRegistrationForTesting(
                productId,
                packagingId,
                registeredProductId,
                registeredPackagingId));
    }

    [Fact]
    public void SceneResetLeavesNoGeneratedIconEntries()
    {
        ProductPackagingContentRuntime.ResetForSceneChange();

        Assert.Equal(0, ProductPackagingContentRuntime.CachedIconCountForTesting);
        Assert.True(ProductPackagingContentRuntime.GeneratedIconWorkComplete);
    }
}
