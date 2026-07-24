using S1API.Internal.Products;
using S1API.Products;
using Xunit;

namespace S1API.Tests.Products;

public sealed class CustomProductMixingIdentityTests
{
    [Fact]
    public void PostSanitizationNativeIdGetsStableNamespacedGeneratedIdentity()
    {
        // ProductManager.FinishAndNameMix lowercases, calls MakeIDFileSafe, then strips ':'
        // before the private FinishAndNameMix/RPC seam. This is the actual post-sanitization
        // value, not an assumed MakeIDFileSafe result.
        const string nativePostSanitizationId = "moremdmablue";

        string generated = CustomProductMixingIdentity.CreateGeneratedProductId(
            "moredrugs:mdma",
            nativePostSanitizationId);

        Assert.StartsWith("moredrugs:mix/mdma/", generated);
        Assert.Equal("moredrugs",
            CustomProductDefinitionBuilderContract.GetOwnerId(generated));
        Assert.Equal(generated, CustomProductMixingIdentity.CreateGeneratedProductId(
            "moredrugs:mdma", nativePostSanitizationId));
        Assert.True(CustomProductMixingIdentity.IsGeneratedIdForSource(
            "moredrugs:mdma", generated));
    }
}
