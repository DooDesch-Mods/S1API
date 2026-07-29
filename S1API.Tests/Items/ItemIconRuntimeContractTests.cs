using S1API.Internal.Items;
using S1API.Items;
using UnityEngine;

namespace S1API.Tests.Items;

public sealed class ItemIconRuntimeContractTests
{
    [Theory]
    [InlineData("ifbars.example:item", "ifbars.example:item", true)]
    [InlineData("IFBARS.EXAMPLE:ITEM", "ifbars.example:item", true)]
    [InlineData("ifbars.example:other", "ifbars.example:item", false)]
    [InlineData("", "ifbars.example:item", false)]
    [InlineData("ifbars.example:item", "", false)]
    [InlineData(null, "ifbars.example:item", false)]
    [InlineData("ifbars.example:item", null, false)]
    public void BoundSlotMatchingUsesStableCaseInsensitiveItemIds(
        string? candidateItemId,
        string? targetItemId,
        bool expected)
    {
        Assert.Equal(
            expected,
            ItemIconRuntime.MatchesItemId(
                candidateItemId,
                targetItemId));
    }

    [Fact]
    public void ExistingIconPropertyShapeRemainsCompatible()
    {
        var property = typeof(ItemDefinition).GetProperty(
            nameof(ItemDefinition.Icon));

        Assert.NotNull(property);
        Assert.Equal(typeof(Sprite), property!.PropertyType);
        Assert.True(property.CanRead);
        Assert.True(property.CanWrite);
        Assert.NotNull(property.SetMethod);
        Assert.True(property.SetMethod!.IsPublic);
    }
}
