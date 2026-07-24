using S1API.Products;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace S1API.Tests.Products;

public sealed class ProductPackagingContentProfileTests
{
    [Fact]
    public void NullContentProviderIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .WithContent(null!));
    }

    [Fact]
    public void ContentProviderIsRequired()
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => new ProductPackagingContentProfileBuilder().Build());

        Assert.Contains("content provider", exception.Message);
    }

    [Fact]
    public void EmptyPlacementsPreserveOneAuthoredContentTransform()
    {
        Func<GameObject?> provider = () => null;

        ProductPackagingContentProfile profile =
            new ProductPackagingContentProfileBuilder()
                .WithContent(provider)
                .Build();

        Assert.Same(provider, profile.ContentProvider);
        Assert.Empty(profile.Placements);
    }

    [Fact]
    public void BuildSnapshotsOrderedPlacementsAcrossBuilderReuse()
    {
        ProductPresentationTransform first = CreatePlacementWithoutUnityRuntime();
        ProductPresentationTransform second = CreatePlacementWithoutUnityRuntime();
        var builder =
            new ProductPackagingContentProfileBuilder()
                .WithContent(() => null)
                .AddPlacement(first);
        ProductPackagingContentProfile profile = builder.Build();

        builder.AddPlacement(second);
        ProductPackagingContentProfile repeated = builder.Build();

        Assert.Equal(new[] { first }, profile.Placements);
        Assert.Equal(new[] { first, second }, repeated.Placements);
    }

    [Fact]
    public void NullPlacementIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .AddPlacement(null!));
    }

    [Fact]
    public void NullPlacementArrayIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .AddPlacements(null!));
    }

    [Fact]
    public void NullPlacementInsideArrayIsRejected()
    {
        ProductPresentationTransform valid = CreatePlacementWithoutUnityRuntime();

        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .AddPlacements(valid, null!));
    }

    private static ProductPresentationTransform CreatePlacementWithoutUnityRuntime()
    {
        return
            (ProductPresentationTransform)RuntimeHelpers.GetUninitializedObject(
                typeof(ProductPresentationTransform));
    }
}
