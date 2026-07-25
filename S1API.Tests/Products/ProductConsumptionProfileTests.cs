using System;
using S1API.Internal.Products;
using S1API.Products;

namespace S1API.Tests.Products;

[Collection(CustomProductRegistryCollection.Name)]
public sealed class ProductConsumptionProfileTests : IDisposable
{
    public ProductConsumptionProfileTests()
    {
        ProductConsumptionProfileRegistrationRegistry.ResetForTesting();
        ProductConsumptionProfileDispatcher.ResetForTesting();
    }

    public void Dispose()
    {
        ProductConsumptionProfileRegistrationRegistry.ResetForTesting();
        ProductConsumptionProfileDispatcher.ResetForTesting();
    }

    [Fact]
    public void BuilderRequiresProviderCompatibilityAndAtLeastOneCallback()
    {
        Assert.Throws<InvalidOperationException>(
            () => new ProductConsumptionProfileBuilder().Build());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProductConsumptionProfileBuilder()
                .WithProviderCompatibility("example:provider", -1));

        Assert.Throws<InvalidOperationException>(
            () => new ProductConsumptionProfileBuilder()
                .WithProviderCompatibility("example:provider", 1)
                .Build());

        Assert.Throws<ArgumentNullException>(
            () => new ProductConsumptionProfileBuilder().OnPlayerApply(null!));
    }

    [Fact]
    public void BuilderSnapshotIsImmutableAcrossLaterBuilderMutations()
    {
        var firstCalls = 0;
        var secondCalls = 0;
        var builder = new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility("example:provider", 1)
            .OnPlayerApply(_ => firstCalls++);
        ProductConsumptionProfile first = builder.Build();
        ProductConsumptionProfile second = builder
            .WithProviderCompatibility("example:provider", 2)
            .OnPlayerApply(_ => secondCalls++)
            .Build();
        ProductConsumptionContext context = CreateContext("example:product", CreateKind());

        ProductConsumptionProfileDispatcher.DispatchForTesting(
            new object(), first, context, true, false);
        ProductConsumptionProfileDispatcher.DispatchForTesting(
            new object(), second, context, true, false);

        Assert.Equal(1, first.ProviderVersion);
        Assert.Equal(2, second.ProviderVersion);
        Assert.Equal(1, firstCalls);
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public void ProductRegistrationOverridesLogicalKindIncludingGeneratedMixes()
    {
        ProductKind kind = CreateKind();
        ProductConsumptionProfile kindProfile = CreateProfile("example:kind-provider");
        ProductConsumptionProfile productProfile = CreateProfile("example:product-provider");
        string sourceId = CreateId();
        string generatedId = CreateId();

        ProductConsumptionProfileRegistry.RegisterForProductKind(kind, kindProfile);
        ProductConsumptionProfileRegistry.RegisterForProduct(sourceId, productProfile);

        Assert.True(ProductConsumptionProfileRegistrationRegistry.TryResolve(
            sourceId,
            kind.Id,
            out ProductConsumptionProfileRegistration? sourceRegistration));
        Assert.Same(productProfile, sourceRegistration!.Profile);

        Assert.True(ProductConsumptionProfileRegistrationRegistry.TryResolve(
            generatedId,
            kind.Id,
            out ProductConsumptionProfileRegistration? generatedRegistration));
        Assert.Same(kindProfile, generatedRegistration!.Profile);
    }

    [Fact]
    public void RegistrationIsCaseInsensitiveIdempotentAndRejectsReplacement()
    {
        string productId = CreateId();
        ProductConsumptionProfile profile = CreateProfile("example:provider");

        ProductConsumptionProfile first =
            ProductConsumptionProfileRegistry.RegisterForProduct(productId, profile);
        ProductConsumptionProfile repeated =
            ProductConsumptionProfileRegistry.RegisterForProduct(
                productId.ToUpperInvariant(),
                profile);

        Assert.Same(profile, first);
        Assert.Same(first, repeated);
        Assert.Throws<InvalidOperationException>(
            () => ProductConsumptionProfileRegistry.RegisterForProduct(
                productId,
                CreateProfile("example:other-provider")));

        ProductKind kind = CreateKind();
        Assert.Same(
            profile,
            ProductConsumptionProfileRegistry.RegisterForProductKind(kind, profile));
        Assert.Same(
            profile,
            ProductConsumptionProfileRegistry.RegisterForProductKind(kind, profile));
        Assert.Throws<InvalidOperationException>(
            () => ProductConsumptionProfileRegistry.RegisterForProductKind(
                kind,
                CreateProfile("example:kind-provider")));
    }

    [Fact]
    public void UnregisteredProductsAndKindsHaveNoProfileResolution()
    {
        Assert.False(ProductConsumptionProfileRegistrationRegistry.TryResolve(
            "example:unregistered-product",
            "example:unregistered-kind",
            out ProductConsumptionProfileRegistration? registration));
        Assert.Null(registration);
    }

    [Fact]
    public void PlayerLifecycleIsOrderedIdempotentAndCleansUpFailedApply()
    {
        ProductKind kind = CreateKind();
        var calls = new System.Collections.Generic.List<string>();
        ProductConsumptionProfile first = new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility("example:first", 1)
            .OnPlayerApply(_ => calls.Add("first-apply"))
            .OnPlayerClear(_ => calls.Add("first-clear"))
            .Build();
        ProductConsumptionProfile second = new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility("example:second", 1)
            .OnPlayerApply(_ => calls.Add("second-apply"))
            .OnPlayerClear(_ => calls.Add("second-clear"))
            .Build();
        object target = new object();
        ProductConsumptionContext firstContext = CreateContext("example:first-product", kind);
        ProductConsumptionContext secondContext = CreateContext("example:second-product", kind);

        ProductConsumptionProfileDispatcher.DispatchForTesting(target, first, firstContext, true, false);
        ProductConsumptionProfileDispatcher.DispatchForTesting(target, first, firstContext, true, false);
        ProductConsumptionProfileDispatcher.DispatchForTesting(target, second, secondContext, true, false);
        ProductConsumptionProfileDispatcher.DispatchForTesting(target, second, secondContext, true, true);
        ProductConsumptionProfileDispatcher.DispatchForTesting(target, second, secondContext, true, true);

        Assert.Equal(
            new[] { "first-apply", "first-clear", "second-apply", "second-clear" },
            calls);

        var failedClearCount = 0;
        ProductConsumptionProfile failing = new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility("example:failing", 1)
            .OnPlayerApply(_ => throw new InvalidOperationException("expected"))
            .OnPlayerClear(_ => failedClearCount++)
            .Build();
        ProductConsumptionProfileDispatcher.DispatchForTesting(target, failing, firstContext, true, false);
        ProductConsumptionProfileDispatcher.DispatchForTesting(target, failing, firstContext, true, true);

        Assert.Equal(1, failedClearCount);
    }

    [Fact]
    public void NpcLifecycleUsesDedicatedCallbacksAndContextReportsTargetKind()
    {
        ProductKind kind = CreateKind();
        var applyCount = 0;
        var clearCount = 0;
        ProductConsumptionProfile profile = new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility("example:npc", 2)
            .OnNpcApply(context =>
            {
                Assert.Null(context.Player);
                applyCount++;
            })
            .OnNpcClear(_ => clearCount++)
            .Build();
        ProductConsumptionContext context = CreateContext("example:npc-product", kind);
        object target = new object();

        ProductConsumptionProfileDispatcher.DispatchForTesting(target, profile, context, false, false);
        ProductConsumptionProfileDispatcher.DispatchForTesting(target, profile, context, false, true);

        Assert.Equal(1, applyCount);
        Assert.Equal(1, clearCount);
        Assert.False(context.IsLocalPlayer);
        Assert.Equal("test-target", context.TargetId);
    }

    [Fact]
    public void PlayerCallbacksAreLocalOnlyWhileNpcCallbacksRemainObserverVisible()
    {
        ProductKind kind = CreateKind();
        var playerCalls = 0;
        var npcCalls = 0;
        ProductConsumptionProfile profile = new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility("example:visibility", 1)
            .OnPlayerApply(_ => playerCalls++)
            .OnNpcApply(_ => npcCalls++)
            .Build();
        ProductConsumptionContext context = CreateContext("example:visibility-product", kind);

        ProductConsumptionProfileDispatcher.DispatchForTesting(
            new object(), profile, context, true, false, isLocalPlayer: false);
        ProductConsumptionProfileDispatcher.DispatchForTesting(
            new object(), profile, context, true, false, isLocalPlayer: true);
        ProductConsumptionProfileDispatcher.DispatchForTesting(
            new object(), profile, context, false, false);

        Assert.Equal(1, playerCalls);
        Assert.Equal(1, npcCalls);
    }

    [Fact]
    public void ManifestProviderIdentityAndVersionAreCompatibilityRelevant()
    {
        CustomProductManifestEntryData baseline = CreateManifestEntry();
        CustomProductManifestEntryData changed = CreateManifestEntry();
        changed.ConsumptionProfileProviderVersion++;

        Assert.NotEqual(
            CustomProductManifestData.ComputeManifestHash(new[] { baseline }),
            CustomProductManifestData.ComputeManifestHash(new[] { changed }));

        var host = new CustomProductManifestData { Entries = new[] { baseline } };
        var local = new CustomProductManifestData { Entries = new[] { changed } };
        Assert.Contains(
            "consumption profile",
            CustomProductManifestData.DescribeCompatibilityMismatch(host, local),
            StringComparison.OrdinalIgnoreCase);
    }

    private static ProductConsumptionProfile CreateProfile(string providerId)
    {
        return new ProductConsumptionProfileBuilder()
            .WithProviderCompatibility(providerId, 1)
            .OnPlayerApply(_ => { })
            .Build();
    }

    private static ProductConsumptionContext CreateContext(string productId, ProductKind kind)
    {
        return new ProductConsumptionContext(productId, kind, null, null, "test-target");
    }

    private static ProductKind CreateKind()
    {
        return new ProductKindBuilder(CreateId())
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();
    }

    private static CustomProductManifestEntryData CreateManifestEntry()
    {
        return new CustomProductManifestEntryData
        {
            ProductId = "example:product",
            OwnerId = "example",
            ProductKindId = "example:kind",
            CompatibilityDrugType = (int)DrugType.MDMA,
            DescriptorFormatVersion = CustomProductSavePersistence.CurrentFormatVersion,
            ProviderId = "example:save-provider",
            ProviderVersion = 1,
            ProviderAvailable = true,
            RepresentationTemplateId = "ogkush",
            PresentationProfileId = string.Empty,
            ConsumptionProfileProviderId = "example:consumption-provider",
            ConsumptionProfileProviderVersion = 1,
            PackagingIds = Array.Empty<string>(),
            CompatibilityHash = new string('a', 64)
        };
    }

    private static string CreateId()
    {
        return $"s1api-consumption-tests:{Guid.NewGuid():N}";
    }
}
