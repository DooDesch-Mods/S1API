using S1API.Entities.Supplier;

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
}
