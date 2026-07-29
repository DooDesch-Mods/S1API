using S1API.Internal.Entities.Suppliers;

namespace S1API.Tests.Entities;

public sealed class SupplierShopUiPolicyTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public void CleanupRemovesOnlyListingRows(
        bool isListingUi,
        bool isAmountSelector,
        bool expected)
    {
        Assert.Equal(
            expected,
            SupplierShopRuntime.ShouldRemoveClonedUiChild(
                isListingUi,
                isAmountSelector));
    }
}
