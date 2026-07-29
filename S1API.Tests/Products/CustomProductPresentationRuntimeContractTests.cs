using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductPresentationRuntimeContractTests
{
    [Fact]
    public void EmptyNativeCaptureRetriesButNormalizationFailureIsTerminal()
    {
        Assert.Equal(
            CustomProductPresentationRuntime.GeneratedIconAttemptResult.Retry,
            CustomProductPresentationRuntime
                .GetGeneratedTextureFailureResult(
                    rendererProducedTexture: false));
        Assert.Equal(
            CustomProductPresentationRuntime.GeneratedIconAttemptResult.Failure,
            CustomProductPresentationRuntime
                .GetGeneratedTextureFailureResult(
                    rendererProducedTexture: true));
    }
}
