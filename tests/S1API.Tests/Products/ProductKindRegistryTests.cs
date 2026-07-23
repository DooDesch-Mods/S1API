using System.Collections;
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
        Assert.ThrowsAny<ArgumentException>(() => new ProductKindDescriptorBuilder(id!));
    }

    [Fact]
    public void EquivalentRegistrationsAreCaseInsensitiveAndIdempotent()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string originalId = $"ExampleMod:{suffix}";
        string equivalentId = $"examplemod:{suffix.ToUpperInvariant()}";

        ProductKindDescriptor original = new ProductKindDescriptorBuilder(originalId)
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();
        ProductKindDescriptor equivalent = new ProductKindDescriptorBuilder(equivalentId)
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();

        Assert.Same(original, equivalent);
        Assert.Same(original, ProductKindRegistry.Get(equivalentId));
        Assert.True(ProductKindRegistry.TryGet(originalId.ToUpperInvariant(), out ProductKindDescriptor? found));
        Assert.Same(original, found);
        Assert.Single(
            ProductKindRegistry.All,
            descriptor => string.Equals(descriptor.Id, originalId, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConflictingRegistrationFailsWithActionableMetadata()
    {
        string id = CreateId();
        new ProductKindDescriptorBuilder(id)
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ProductKindDescriptorBuilder(id.ToUpperInvariant())
                .WithCompatibilityDrugType(DrugType.Heroin)
                .Build());

        Assert.Contains(id, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(DrugType.MDMA), exception.Message);
        Assert.Contains(nameof(DrugType.Heroin), exception.Message);
        Assert.Contains("case-insensitive", exception.Message);
    }

    [Fact]
    public void DescriptorAndRegistrySnapshotsAreImmutable()
    {
        ProductKindDescriptor descriptor = new ProductKindDescriptorBuilder(CreateId()).Build();

        Assert.True(typeof(ProductKindDescriptor).IsSealed);
        Assert.Null(typeof(ProductKindDescriptor).GetProperty(nameof(ProductKindDescriptor.Id))!.SetMethod);
        Assert.Null(
            typeof(ProductKindDescriptor)
                .GetProperty(nameof(ProductKindDescriptor.CompatibilityDrugType))!
                .SetMethod);

        ICollection snapshot = Assert.IsAssignableFrom<ICollection>(ProductKindRegistry.All);
        Assert.Throws<NotSupportedException>(
            () => ((IList)snapshot).Add(descriptor));
    }

    [Fact]
    public void CompatibilityMappingIsOptionalAndValidated()
    {
        ProductKindDescriptor withoutMapping =
            new ProductKindDescriptorBuilder(CreateId()).Build();
        ProductKindDescriptor withMapping = new ProductKindDescriptorBuilder(CreateId())
            .WithCompatibilityDrugType(DrugType.Shrooms)
            .Build();

        Assert.Null(withoutMapping.CompatibilityDrugType);
        Assert.Equal(DrugType.Shrooms, withMapping.CompatibilityDrugType);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProductKindDescriptorBuilder(CreateId())
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
    public void RegistryRetainsDescriptorsForTheProcessLifetime()
    {
        (string id, WeakReference reference) = RegisterWithoutRetainingDescriptor();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.True(reference.IsAlive);
        Assert.Same(reference.Target, ProductKindRegistry.Get(id));
    }

    private static (string Id, WeakReference Reference) RegisterWithoutRetainingDescriptor()
    {
        string id = CreateId();
        ProductKindDescriptor descriptor = new ProductKindDescriptorBuilder(id).Build();
        return (id, new WeakReference(descriptor));
    }

    private static string CreateId()
    {
        return $"s1api-tests:{Guid.NewGuid():N}";
    }
}
