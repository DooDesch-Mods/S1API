using S1API.Entities.Supplier;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;

namespace S1API.Tests.Entities;

public sealed class SupplierDataBuilderDeferredItemTests
{
    [Fact]
    public void WithDeliveryItem_AllowsStableIdBeforeItemRegistration()
    {
        var builder = new SupplierDataBuilder();

        SupplierDataBuilder result =
            builder.WithDeliveryItem("example.mod:ingredients/late-item");

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithDeliveryItem_DeduplicatesDeferredIdsIgnoringCase()
    {
        var builder = new SupplierDataBuilder()
            .WithDeliveryItem("example.mod:ingredients/late-item")
            .WithDeliveryItem("EXAMPLE.MOD:INGREDIENTS/LATE-ITEM");

        var data = builder.BuildInternal();

        Assert.Single(data.DeliveryItemIds);
    }

    [Fact]
    public void WithStashDeadDrop_StoresTypedGuidWithoutSceneResolution()
    {
        var builder = new SupplierDataBuilder()
            .WithStashDeadDrop<BehindBank>();

        var data = builder.BuildInternal();

        Assert.Equal(
            "e5399b56-22a5-4a2c-a254-cc81e3ab2f75",
            data.StashDeadDropGuid);
    }

    [Fact]
    public void GetGuid_RejectsUnannotatedIdentifiers()
    {
        Assert.Throws<InvalidOperationException>(
            DeadDropManager.GetGuid<MissingGuid>);
    }

    private sealed class MissingGuid : IDeadDropIdentifier
    {
    }
}
