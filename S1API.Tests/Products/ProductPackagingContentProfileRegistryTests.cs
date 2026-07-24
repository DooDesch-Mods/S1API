using S1API.Internal.Products;
using S1API.Products;

namespace S1API.Tests.Products;

public sealed class ProductPackagingContentProfileRegistryTests : IDisposable
{
    public ProductPackagingContentProfileRegistryTests()
    {
        ProductPackagingContentProfileRegistry.ResetForTesting();
    }

    public void Dispose()
    {
        ProductPackagingContentProfileRegistry.ResetForTesting();
    }

    [Fact]
    public void ProductAndPackagingPairIsCaseInsensitiveAndIdempotent()
    {
        string productId = CreateProductId();
        ProductPackagingContentProfile profile = CreateProfile();

        ProductPackagingContentProfile first =
            ProductPackagingContentProfileRegistry.Register(
                "ExampleMod",
                productId,
                "Baggie",
                profile);
        ProductPackagingContentProfile repeated =
            ProductPackagingContentProfileRegistry.Register(
                "examplemod",
                productId.ToUpperInvariant(),
                "baggie",
                profile);

        Assert.Same(profile, first);
        Assert.Same(first, repeated);
        Assert.True(
            ProductPackagingContentProfileRegistry.TryResolve(
                productId,
                "BAGGIE",
                out ProductPackagingContentProfileRegistration? registration));
        Assert.NotNull(registration);
        Assert.Same(profile, registration.Profile);
    }

    [Fact]
    public void SameProductCanRegisterIndependentPackagingProfiles()
    {
        string productId = CreateProductId();
        ProductPackagingContentProfile baggie = CreateProfile();
        ProductPackagingContentProfile jar = CreateProfile();

        ProductPackagingContentProfileRegistry.Register(
            "examplemod",
            productId,
            "baggie",
            baggie);
        ProductPackagingContentProfileRegistry.Register(
            "examplemod",
            productId,
            "jar",
            jar);

        Assert.True(
            ProductPackagingContentProfileRegistry.TryResolve(
                productId,
                "baggie",
                out ProductPackagingContentProfileRegistration? baggieRegistration));
        Assert.True(
            ProductPackagingContentProfileRegistry.TryResolve(
                productId,
                "jar",
                out ProductPackagingContentProfileRegistration? jarRegistration));
        Assert.Same(baggie, baggieRegistration!.Profile);
        Assert.Same(jar, jarRegistration!.Profile);
    }

    [Fact]
    public void RegisteredProductWithUnregisteredPackagingPreservesNativeFallback()
    {
        string productId = CreateProductId();
        ProductPackagingContentProfileRegistry.Register(
            "examplemod",
            productId,
            "baggie",
            CreateProfile());

        Assert.False(
            ProductPackagingContentProfileRegistry.TryResolve(
                productId,
                "jar",
                out _));
    }

    [Fact]
    public void ConflictingOwnerReportsThePair()
    {
        string productId = CreateProductId();
        ProductPackagingContentProfileRegistry.Register(
            "first-mod",
            productId,
            "jar",
            CreateProfile());

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    ProductPackagingContentProfileRegistry.Register(
                        "second-mod",
                        productId.ToUpperInvariant(),
                        "JAR",
                        CreateProfile()));

        Assert.Contains(productId, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("jar", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("first-mod", exception.Message);
        Assert.Contains("second-mod", exception.Message);
    }

    [Fact]
    public void SameOwnerCannotSilentlyReplaceAProfile()
    {
        string productId = CreateProductId();
        ProductPackagingContentProfileRegistry.Register(
            "examplemod",
            productId,
            "jar",
            CreateProfile());

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    ProductPackagingContentProfileRegistry.Register(
                        "examplemod",
                        productId,
                        "jar",
                        CreateProfile()));

        Assert.Contains("another profile", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PackagingIdMustNotBeEmpty(string packagingId)
    {
        Assert.Throws<ArgumentException>(
            () =>
                ProductPackagingContentProfileRegistry.Register(
                    "examplemod",
                    CreateProductId(),
                    packagingId,
                    CreateProfile()));
    }

    [Fact]
    public void RegistrationTrimsOwnerProductAndPackagingIds()
    {
        string productId = CreateProductId();
        ProductPackagingContentProfile profile = CreateProfile();

        ProductPackagingContentProfileRegistry.Register(
            " examplemod ",
            $" {productId} ",
            " jar ",
            profile);

        Assert.True(
            ProductPackagingContentProfileRegistry.TryResolve(
                productId,
                "jar",
                out ProductPackagingContentProfileRegistration? registration));
        Assert.Equal("examplemod", registration!.OwnerId);
        Assert.Equal(productId, registration.Key.ProductId);
        Assert.Equal("jar", registration.Key.PackagingId);
    }

    [Theory]
    [InlineData(null, "s1api-tests:product", "baggie", "ownerId")]
    [InlineData("", "s1api-tests:product", "baggie", "ownerId")]
    [InlineData("owner", null, "baggie", "productId")]
    [InlineData("owner", "", "baggie", "productId")]
    [InlineData("owner", "not-namespaced", "baggie", "productId")]
    [InlineData("owner", "s1api-tests:product", null, "packagingId")]
    public void InvalidRegistrationInputsAreRejected(
        string? ownerId,
        string? productId,
        string? packagingId,
        string parameterName)
    {
        ArgumentException exception =
            Assert.ThrowsAny<ArgumentException>(
                () =>
                    ProductPackagingContentProfileRegistry.Register(
                        ownerId!,
                        productId!,
                        packagingId!,
                        CreateProfile()));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void NullProfileIsRejected()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(
                () =>
                    ProductPackagingContentProfileRegistry.Register(
                        "examplemod",
                        CreateProductId(),
                        "baggie",
                        null!));

        Assert.Equal("profile", exception.ParamName);
    }

    private static ProductPackagingContentProfile CreateProfile()
    {
        return new ProductPackagingContentProfileBuilder()
            .WithContent(() => null)
            .Build();
    }

    private static string CreateProductId()
    {
        return $"s1api-tests:{Guid.NewGuid():N}";
    }
}
