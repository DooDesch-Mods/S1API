using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductManifestRuntimeContractTests
{
    [Fact]
    public void EmptyManifestDoesNotRequireValidation()
    {
        Assert.False(CustomProductManifestRuntime.RequiresValidation(
            new CustomProductManifestData()));
    }

    [Fact]
    public void DescriptorBackedProductRequiresValidation()
    {
        var manifest = new CustomProductManifestData
        {
            Entries = [new CustomProductManifestEntryData()]
        };

        Assert.True(CustomProductManifestRuntime.RequiresValidation(manifest));
    }

    [Fact]
    public void MixingOnlyManifestRequiresValidation()
    {
        var manifest = new CustomProductManifestData
        {
            MixingProfiles =
                [new CustomProductMixingProfileManifestEntryData()]
        };

        Assert.True(CustomProductManifestRuntime.RequiresValidation(manifest));
    }
}
