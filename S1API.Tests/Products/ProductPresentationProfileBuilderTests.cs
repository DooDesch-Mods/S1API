using S1API.Products;
using UnityEngine;

namespace S1API.Tests.Products;

public sealed class ProductPresentationProfileBuilderTests
{
    [Fact]
    public void LooseVisualIsTheDeterministicRenderedContextFallback()
    {
        Func<GameObject?> loose = () => null;
        ProductPresentationProfile profile =
            new ProductPresentationProfileBuilder()
                .WithLooseVisual(loose)
                .Build();

        Assert.True(
            profile.TryGetVisualProvider(
                ProductPresentationContext.Loose,
                out Func<GameObject?>? looseProvider));
        Assert.True(
            profile.TryGetVisualProvider(
                ProductPresentationContext.Stored,
                out Func<GameObject?>? storedProvider));
        Assert.True(
            profile.TryGetVisualProvider(
                ProductPresentationContext.Held,
                out Func<GameObject?>? heldProvider));
        Assert.True(
            profile.TryGetVisualProvider(
                ProductPresentationContext.Station,
                out Func<GameObject?>? stationProvider));
        Assert.True(
            profile.TryGetVisualProvider(
                ProductPresentationContext.FunctionalProduct,
                out Func<GameObject?>? functionalProvider));
        Assert.Same(loose, looseProvider);
        Assert.Same(loose, storedProvider);
        Assert.Same(loose, heldProvider);
        Assert.Same(loose, stationProvider);
        Assert.Same(loose, functionalProvider);
    }

    [Fact]
    public void ContextSpecificVisualOverridesLooseFallback()
    {
        Func<GameObject?> loose = () => null;
        Func<GameObject?> held = () => null;
        ProductPresentationProfile profile =
            new ProductPresentationProfileBuilder()
                .WithLooseVisual(loose)
                .WithHeldVisual(held)
                .Build();

        Assert.True(
            profile.TryGetVisualProvider(
                ProductPresentationContext.Held,
                out Func<GameObject?>? resolved));
        Assert.Same(held, resolved);
    }

#if MONOMELON
    [Fact]
    public void LooseTransformFallsBackUntilAContextDefinesItsOwnProvider()
    {
        var looseTransform =
            new ProductPresentationTransform(
                new Vector3(1f, 2f, 3f),
                new Vector3(10f, 20f, 30f),
                new Vector3(0.1f, 0.2f, 0.3f));
        var heldTransform =
            new ProductPresentationTransform(
                Vector3.zero,
                new Vector3(0f, 90f, 0f),
                Vector3.one);
        ProductPresentationProfile profile =
            new ProductPresentationProfileBuilder()
                .WithLooseVisual(() => null, looseTransform)
                .WithHeldVisual(() => null, heldTransform)
                .Build();

        Assert.True(
            profile.TryGetVisualTransform(
                ProductPresentationContext.Stored,
                out ProductPresentationTransform? stored));
        Assert.Same(looseTransform, stored);
        Assert.True(
            profile.TryGetVisualTransform(
                ProductPresentationContext.Held,
                out ProductPresentationTransform? held));
        Assert.Same(heldTransform, held);
    }
#endif

    [Fact]
    public void RequiredContextMustHaveAConfiguredProvider()
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    new ProductPresentationProfileBuilder()
                        .Require(ProductPresentationContext.Consumption)
                        .Build());

        Assert.Contains("Consumption", exception.Message);
        Assert.Contains("does not have a provider", exception.Message);
    }

    [Fact]
    public void RequiredRenderedContextsMayUseLooseFallback()
    {
        ProductPresentationProfile profile =
            new ProductPresentationProfileBuilder()
                .WithLooseVisual(() => null)
                .Require(
                    ProductPresentationContext.Stored,
                    ProductPresentationContext.Held,
                    ProductPresentationContext.Station,
                    ProductPresentationContext.FunctionalProduct)
                .Build();

        Assert.True(profile.IsRequired(ProductPresentationContext.Stored));
        Assert.True(profile.IsRequired(ProductPresentationContext.Held));
        Assert.True(profile.IsRequired(ProductPresentationContext.Station));
        Assert.True(
            profile.IsRequired(ProductPresentationContext.FunctionalProduct));
    }

    [Theory]
    [InlineData(31)]
    [InlineData(2049)]
    public void GeneratedIconSizeIsBounded(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new ProductPresentationProfileBuilder()
                    .WithGeneratedIconFromLooseVisual(size));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.1f)]
    [InlineData(2.01f)]
    public void GeneratedIconCameraFillIsBounded(float cameraFill)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new ProductPresentationProfileBuilder()
                    .WithGeneratedIconFromLooseVisual(
                        512,
                        fitToCamera: true,
                        cameraFill: cameraFill));
    }

    [Fact]
    public void GeneratedIconFramingControlsAreSnapshotted()
    {
        ProductPresentationProfile profile =
            new ProductPresentationProfileBuilder()
                .WithLooseVisual(() => null)
                .WithGeneratedIconFromLooseVisual(
                    256,
                    fitToCamera: false,
                    cameraFill: 1.25f)
                .Build();

        Assert.Equal(256, profile.GeneratedIconSize);
        Assert.False(profile.FitGeneratedIconToCamera);
        Assert.Equal(1.25f, profile.GeneratedIconCameraFill);
    }

    [Fact]
    public void BuildSnapshotsProvidersAndRequiredContexts()
    {
        Func<GameObject?> loose = () => null;
        var builder =
            new ProductPresentationProfileBuilder()
                .WithLooseVisual(loose)
                .Require(ProductPresentationContext.Stored);
        ProductPresentationProfile first = builder.Build();

        Func<GameObject?> replacement = () => null;
        builder
            .WithLooseVisual(replacement)
            .Require(ProductPresentationContext.Held);

        Assert.True(
            first.TryGetVisualProvider(
                ProductPresentationContext.Stored,
                out Func<GameObject?>? retained));
        Assert.Same(loose, retained);
        Assert.True(first.IsRequired(ProductPresentationContext.Stored));
        Assert.False(first.IsRequired(ProductPresentationContext.Held));
    }

    [Theory]
    [InlineData((ProductPresentationContext)(-1))]
    [InlineData((ProductPresentationContext)99)]
    public void UndefinedRequiredContextIsRejected(
        ProductPresentationContext context)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProductPresentationProfileBuilder().Require(context));
    }
}
