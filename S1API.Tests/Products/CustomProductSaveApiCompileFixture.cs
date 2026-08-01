using S1API.Products;

namespace S1API.Tests.Products;

internal static class CustomProductSaveApiCompileFixture
{
    internal static CustomProductDefinition CompileProviderCaller(
        CustomProductDefinitionBuilder builder)
    {
        return builder
            .WithSaveProvider("example:focus-tablet", providerVersion: 1, providerData: "v1")
            .Build();
    }

    private sealed class Provider : ICustomProductSaveProvider
    {
        public string ProviderId => "example:focus-tablet";
        public int MaximumDescriptorVersion => 1;
        public CustomProductDefinitionBuilder? Restore(CustomProductSaveDescriptor descriptor) => null;
    }

    internal static void CompileRegistryCaller()
    {
        _ = CustomProductSaveProviderRegistry.Register(new Provider());
    }

    internal static void CompileMultiplayerDiagnosticsCaller()
    {
        CustomProductMultiplayerPolicy policy =
            CustomProductMultiplayer.MissingContentPolicy;
        string compatibilityHash =
            CustomProductMultiplayer.GetCompatibilityManifestHash();
        _ = policy == CustomProductMultiplayerPolicy.Reject &&
            compatibilityHash.Length == 64;
    }
}
