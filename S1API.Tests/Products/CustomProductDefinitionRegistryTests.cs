#if IL2CPPMELON
using NativeProductDefinition = Il2CppScheduleOne.Product.ProductDefinition;
#elif MONOMELON
using NativeProductDefinition = ScheduleOne.Product.ProductDefinition;
#endif

using System.Runtime.CompilerServices;
using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductDefinitionRegistryTests : IDisposable
{
    private readonly FakeRuntimeAdapter _runtimeAdapter = new FakeRuntimeAdapter();

    public CustomProductDefinitionRegistryTests()
    {
        CustomProductDefinitionRegistry.ResetForTesting(_runtimeAdapter);
    }

    public void Dispose()
    {
        CustomProductDefinitionRegistry.RestoreRuntimeAdapterForTesting();
    }

    [Fact]
    public void RepeatedSameOwnerRegistrationIsCaseInsensitiveAndIdempotent()
    {
        string productId = CreateProductId();
        NativeProductDefinition firstDefinition = CreateDefinition();
        NativeProductDefinition ignoredDefinition = CreateDefinition();

        NativeProductDefinition first = CustomProductDefinitionRegistry.Register(
            "ExampleMod",
            productId,
            "First Product Name",
            125f,
            firstDefinition);
        NativeProductDefinition repeated = CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId.ToUpperInvariant(),
            "Ignored Product Name",
            900f,
            ignoredDefinition);

        Assert.Same(firstDefinition, first);
        Assert.Same(firstDefinition, repeated);
        Assert.Single(_runtimeAdapter.RegisteredDefinitions);
        Assert.Single(_runtimeAdapter.AllProducts);
        Assert.Single(_runtimeAdapter.ProductNames);
        Assert.Equal("First Product Name", _runtimeAdapter.ProductNames.Single());
        Assert.Equal(125f, _runtimeAdapter.ProductPrices[productId]);
    }

    [Fact]
    public void ConflictingOwnerFailsWithActionableIdentity()
    {
        string productId = CreateProductId();
        CustomProductDefinitionRegistry.Register(
            "first-mod",
            productId,
            "Owned Product",
            50f,
            CreateDefinition());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => CustomProductDefinitionRegistry.Register(
                "second-mod",
                productId.ToUpperInvariant(),
                "Conflicting Product",
                75f,
                CreateDefinition()));

        Assert.Contains(productId, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("first-mod", exception.Message);
        Assert.Contains("second-mod", exception.Message);
        Assert.Contains("case-insensitive", exception.Message);
        Assert.Single(_runtimeAdapter.RegisteredDefinitions);
    }

    [Fact]
    public void FailedInitialApplyDoesNotRetainOwnership()
    {
        string productId = CreateProductId();
        NativeProductDefinition rejectedDefinition = CreateDefinition();
        NativeProductDefinition correctedDefinition = CreateDefinition();
        _runtimeAdapter.NextApplyException =
            new InvalidOperationException("Native registration failed.");

        Assert.Throws<InvalidOperationException>(
            () => CustomProductDefinitionRegistry.Register(
                "examplemod",
                productId,
                "Rejected Product",
                50f,
                rejectedDefinition));

        NativeProductDefinition registered = CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Corrected Product",
            75f,
            correctedDefinition);

        Assert.Same(correctedDefinition, registered);
        Assert.Same(
            correctedDefinition,
            _runtimeAdapter.RegisteredDefinitions[productId]);
        Assert.Equal(2, _runtimeAdapter.ApplyCount);
    }

    [Fact]
    public void PreLoadRestoresDefinitionsBeforeLoadAndLoadCompletePreservesSavedPrice()
    {
        string productId = CreateProductId();
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Lifecycle Product",
            120f,
            CreateDefinition());

        _runtimeAdapter.ResetSceneState();
        CustomProductDefinitionRegistry.InvokePreLoadForTesting();

        Assert.True(_runtimeAdapter.RegisteredDefinitions.ContainsKey(productId));
        Assert.True(_runtimeAdapter.AllProducts.ContainsKey(productId));
        Assert.Contains("Lifecycle Product", _runtimeAdapter.ProductNames);
        Assert.Equal(120f, _runtimeAdapter.ProductPrices[productId]);

        _runtimeAdapter.ProductPrices[productId] = 275f;
        CustomProductDefinitionRegistry.InvokeLoadCompleteForTesting();
        CustomProductDefinitionRegistry.InvokeLoadCompleteForTesting();

        Assert.Single(_runtimeAdapter.RegisteredDefinitions);
        Assert.Single(_runtimeAdapter.AllProducts);
        Assert.Single(_runtimeAdapter.ProductNames);
        Assert.Single(_runtimeAdapter.ProductPrices);
        Assert.Equal(275f, _runtimeAdapter.ProductPrices[productId]);
        Assert.Equal(0, _runtimeAdapter.CreatedProductsCount);
    }

    [Theory]
    [InlineData(null, "product", "name", 1f)]
    [InlineData(" ", "product", "name", 1f)]
    [InlineData("owner", null, "name", 1f)]
    [InlineData("owner", " ", "name", 1f)]
    [InlineData("owner", "product", null, 1f)]
    [InlineData("owner", "product", " ", 1f)]
    [InlineData("owner", "product", "name", float.NaN)]
    [InlineData("owner", "product", "name", float.PositiveInfinity)]
    [InlineData("owner", "product", "name", float.NegativeInfinity)]
    public void InvalidRegistrationMetadataFailsBeforeRuntimeMutation(
        string? ownerId,
        string? productId,
        string? productName,
        float initialPrice)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => CustomProductDefinitionRegistry.Register(
                ownerId!,
                productId!,
                productName!,
                initialPrice,
                CreateDefinition()));

        Assert.Equal(0, _runtimeAdapter.ApplyCount);
    }

    [Fact]
    public void NullDefinitionFailsBeforeRuntimeMutation()
    {
        Assert.Throws<ArgumentNullException>(
            () => CustomProductDefinitionRegistry.Register(
                "owner",
                CreateProductId(),
                "Product",
                1f,
                null!));

        Assert.Equal(0, _runtimeAdapter.ApplyCount);
    }

    [Fact]
    public void RegistryRetainsDefinitionForProcessLifetime()
    {
        string productId = CreateProductId();
        WeakReference reference = RegisterWithoutRetainingDefinition(productId);

        _runtimeAdapter.ResetSceneState();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.True(reference.IsAlive);
        CustomProductDefinitionRegistry.InvokePreLoadForTesting();
        Assert.Same(reference.Target, _runtimeAdapter.RegisteredDefinitions[productId]);
    }

    [Theory]
    [InlineData("S1API.Internal.Products.CustomProductDefinitionRegistry")]
    [InlineData("S1API.Internal.Products.CustomProductDefinitionRuntimeAdapter")]
    public void LifecycleRegistrationTypesRemainOutsideThePublicApi(string typeName)
    {
        Type? type = typeof(CustomProductDefinitionRegistry).Assembly.GetType(typeName);

        Assert.NotNull(type);
        Assert.False(type.IsPublic);
        Assert.False(type.IsNestedPublic);
    }

    private static WeakReference RegisterWithoutRetainingDefinition(string productId)
    {
        NativeProductDefinition definition = CreateDefinition();
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Retained Product",
            10f,
            definition);
        return new WeakReference(definition);
    }

    private static NativeProductDefinition CreateDefinition()
    {
        var definition = (NativeProductDefinition)RuntimeHelpers.GetUninitializedObject(
            typeof(NativeProductDefinition));
        GC.SuppressFinalize(definition);
        return definition;
    }

    private static string CreateProductId()
    {
        return $"s1api-tests:{Guid.NewGuid():N}";
    }

    private sealed class FakeRuntimeAdapter :
        ICustomProductDefinitionRuntimeAdapter
    {
        internal Dictionary<string, NativeProductDefinition> RegisteredDefinitions { get; } =
            new Dictionary<string, NativeProductDefinition>(
                StringComparer.OrdinalIgnoreCase);

        internal Dictionary<string, NativeProductDefinition> AllProducts { get; } =
            new Dictionary<string, NativeProductDefinition>(
                StringComparer.OrdinalIgnoreCase);

        internal HashSet<string> ProductNames { get; } =
            new HashSet<string>(StringComparer.Ordinal);

        internal Dictionary<string, float> ProductPrices { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        internal int ApplyCount { get; private set; }

        internal int CreatedProductsCount =>
            0;

        internal Exception? NextApplyException { get; set; }

        public bool Apply(CustomProductDefinitionRegistration registration)
        {
            ApplyCount++;

            if (NextApplyException != null)
            {
                Exception exception = NextApplyException;
                NextApplyException = null;
                throw exception;
            }

            RegisteredDefinitions.TryAdd(
                registration.ProductId,
                registration.Definition);
            AllProducts.TryAdd(
                registration.ProductId,
                registration.Definition);
            ProductNames.Add(registration.ProductName);
            ProductPrices.TryAdd(
                registration.ProductId,
                registration.InitialPrice);
            return true;
        }

        internal void ResetSceneState()
        {
            RegisteredDefinitions.Clear();
            AllProducts.Clear();
            ProductNames.Clear();
            ProductPrices.Clear();
        }
    }
}
