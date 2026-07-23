using S1API.Products;

namespace S1API.Tests.Products;

public sealed class WeedDefinitionBuilderContractTests
{
    [Fact]
    public void NormalizeId_TrimsStableNamespacedId()
    {
        var actual = WeedDefinitionBuilderContract.NormalizeId(" example.mod:calm-kush ");

        Assert.Equal("example.mod:calm-kush", actual);
    }

    [Theory]
    [InlineData("calm-kush")]
    [InlineData("example.mod:")]
    [InlineData("example:mod:calm-kush")]
    [InlineData("example.mod:calm kush")]
    public void NormalizeId_RejectsInvalidIds(string id)
    {
        Assert.Throws<ArgumentException>(() => WeedDefinitionBuilderContract.NormalizeId(id));
    }

    [Fact]
    public void NormalizeName_TrimsDisplayName()
    {
        var actual = WeedDefinitionBuilderContract.NormalizeName(" Calm Kush ");

        Assert.Equal("Calm Kush", actual);
    }

    [Fact]
    public void NormalizeName_RejectsEmptyDisplayName()
    {
        Assert.Throws<ArgumentException>(() => WeedDefinitionBuilderContract.NormalizeName(" "));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(WeedDefinitionBuilderContract.MaximumPropertyCount)]
    public void ValidatePropertyCounts_AcceptsResolvedPropertiesWithinLimit(int count)
    {
        WeedDefinitionBuilderContract.ValidatePropertyCounts(count, count);
    }

    [Fact]
    public void ValidatePropertyCounts_RejectsProductWithoutProperties()
    {
        Assert.Throws<InvalidOperationException>(
            () => WeedDefinitionBuilderContract.ValidatePropertyCounts(0, 0));
    }

    [Fact]
    public void ValidatePropertyCounts_RejectsTooManyProperties()
    {
        var count = WeedDefinitionBuilderContract.MaximumPropertyCount + 1;

        Assert.Throws<InvalidOperationException>(
            () => WeedDefinitionBuilderContract.ValidatePropertyCounts(count, count));
    }

    [Fact]
    public void ValidatePropertyCounts_RejectsUnresolvedOrDuplicateProperties()
    {
        Assert.Throws<InvalidOperationException>(
            () => WeedDefinitionBuilderContract.ValidatePropertyCounts(2, 1));
    }
}
