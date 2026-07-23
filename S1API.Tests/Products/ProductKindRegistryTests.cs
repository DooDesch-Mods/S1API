using S1API.Products;

namespace S1API.Tests.Products;

public sealed class ProductKindRegistryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("missing-namespace")]
    [InlineData(":missing-namespace")]
    [InlineData("missing-name:")]
    [InlineData("too:many:parts")]
    [InlineData("example mod:mdma")]
    [InlineData("examplemod:mdma?")]
    [InlineData(".:/")]
    public void ConstructorRejectsInvalidIds(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ProductKindBuilder(id!));
    }

    [Fact]
    public void EquivalentRegistrationsAreCaseInsensitiveAndIdempotent()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string originalId = $"ExampleMod:{suffix}";
        string equivalentId = $"examplemod:{suffix.ToUpperInvariant()}";

        ProductKind original = new ProductKindBuilder(originalId)
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();
        ProductKind equivalent = new ProductKindBuilder(equivalentId)
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();

        Assert.Same(original, equivalent);
        Assert.Same(original, ProductKindRegistry.Get(equivalentId));
        Assert.True(ProductKindRegistry.TryGet(originalId.ToUpperInvariant(), out ProductKind? found));
        Assert.Same(original, found);
        Assert.Single(
            ProductKindRegistry.All,
            productKind => string.Equals(productKind.Id, originalId, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConflictingRegistrationFailsWithActionableMetadata()
    {
        string id = CreateId();
        new ProductKindBuilder(id)
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ProductKindBuilder(id.ToUpperInvariant())
                .WithCompatibilityDrugType(DrugType.Heroin)
                .Build());

        Assert.Contains(id, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(DrugType.MDMA), exception.Message);
        Assert.Contains(nameof(DrugType.Heroin), exception.Message);
        Assert.Contains("case-insensitive", exception.Message);
    }

    [Fact]
    public void ProductKindsAndRegistrySnapshotsAreImmutable()
    {
        ProductKind productKind = new ProductKindBuilder(CreateId()).Build();

        Assert.True(typeof(ProductKind).IsSealed);
        Assert.Null(typeof(ProductKind).GetProperty(nameof(ProductKind.Id))!.SetMethod);
        Assert.Null(
            typeof(ProductKind)
                .GetProperty(nameof(ProductKind.CompatibilityDrugType))!
                .SetMethod);

        ICollection<ProductKind> snapshot =
            Assert.IsAssignableFrom<ICollection<ProductKind>>(ProductKindRegistry.All);
        Assert.True(snapshot.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => snapshot.Add(productKind));
    }

    [Fact]
    public void ProductKindStringIncludesStableDiagnosticIdentity()
    {
        ProductKind withoutMapping = new ProductKindBuilder(CreateId()).Build();
        ProductKind withMapping = new ProductKindBuilder(CreateId())
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();

        Assert.Equal(withoutMapping.Id, withoutMapping.ToString());
        Assert.Equal(withMapping.Id, withMapping.ToString());
    }

    [Fact]
    public void BuilderReusePreservesIdempotencyAndConflictBehavior()
    {
        var builder = new ProductKindBuilder(CreateId())
            .WithCompatibilityDrugType(DrugType.MDMA);

        ProductKind first = builder.Build();
        ProductKind repeated = builder.Build();

        Assert.Same(first, repeated);
        Assert.Throws<InvalidOperationException>(
            () => builder
                .WithCompatibilityDrugType(DrugType.Heroin)
                .Build());
    }

    [Fact]
    public void RegistryAllReturnsAnIsolatedSnapshot()
    {
        IReadOnlyCollection<ProductKind> before = ProductKindRegistry.All;
        ProductKind added = new ProductKindBuilder(CreateId()).Build();

        Assert.DoesNotContain(before, productKind => productKind.Id == added.Id);
        Assert.Contains(ProductKindRegistry.All, productKind => productKind.Id == added.Id);
    }

    [Fact]
    public void CompatibilityMappingIsOptionalAndValidated()
    {
        ProductKind withoutMapping =
            new ProductKindBuilder(CreateId()).Build();
        ProductKind withMapping = new ProductKindBuilder(CreateId())
            .WithCompatibilityDrugType(DrugType.Shrooms)
            .Build();

        Assert.Null(withoutMapping.CompatibilityDrugType);
        Assert.Equal(DrugType.Shrooms, withMapping.CompatibilityDrugType);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProductKindBuilder(CreateId())
                .WithCompatibilityDrugType((DrugType)int.MaxValue));
    }

    [Fact]
    public void CompatibilityMappingRetainsExistingDrugTypeValues()
    {
        Assert.Equal(0, (int)DrugType.Marijuana);
        Assert.Equal(1, (int)DrugType.Methamphetamine);
        Assert.Equal(2, (int)DrugType.Cocaine);
        Assert.Equal(3, (int)DrugType.MDMA);
        Assert.Equal(4, (int)DrugType.Shrooms);
        Assert.Equal(5, (int)DrugType.Heroin);
    }

    [Fact]
    public void RegistryRetainsProductKindsForTheProcessLifetime()
    {
        (string id, WeakReference reference) = RegisterWithoutRetainingProductKind();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.True(reference.IsAlive);
        Assert.Same(reference.Target, ProductKindRegistry.Get(id));
    }

    private static (string Id, WeakReference Reference) RegisterWithoutRetainingProductKind()
    {
        string id = CreateId();
        ProductKind productKind = new ProductKindBuilder(id).Build();
        return (id, new WeakReference(productKind));
    }

    private static string CreateId()
    {
        return $"s1api-tests:{Guid.NewGuid():N}";
    }
}
