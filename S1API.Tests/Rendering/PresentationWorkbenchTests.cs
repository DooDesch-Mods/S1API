using System.Globalization;
using S1API.Internal.Rendering;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Tests.Rendering;

public sealed class PresentationWorkbenchTests : IDisposable
{
    public PresentationWorkbenchTests()
    {
        PresentationWorkbenchRegistry.ResetForTesting();
    }

    public void Dispose()
    {
        PresentationWorkbenchRegistry.ResetForTesting();
    }

    [Fact]
    public void BuilderRequiresNamespacedIdAndAtLeastOneContext()
    {
        Assert.Throws<ArgumentException>(
            () => new PresentationWorkbenchDefinitionBuilder(
                "not-namespaced",
                "Preview"));
        Assert.Throws<InvalidOperationException>(
            () => new PresentationWorkbenchDefinitionBuilder(
                    "example.mod:item",
                    "Preview")
                .Build());
    }

#if MONOMELON
    [Fact]
    public void BuilderSnapshotsSupportedContexts()
    {
        PresentationWorkbenchDefinition definition =
            new PresentationWorkbenchDefinitionBuilder(
                    "example.mod:item",
                    "Example item")
                .WithFirstPersonPreview(() => null)
                .WithAvatarPreview(() => null)
                .WithIconPreview(
                    () => null,
                    new Vector3(18f, -32f, 0f),
                    fitToCamera: false,
                    cameraFill: 0.8f,
                    size: 256,
                    initialScale: Vector3.one * 0.45f)
                .Build();

        Assert.Equal("example.mod:item", definition.Id);
        Assert.Equal("Example item", definition.DisplayName);
        Assert.True(definition.SupportsFirstPerson);
        Assert.True(definition.SupportsAvatar);
        Assert.True(definition.SupportsIcon);
        Assert.Equal(256, definition.Icon!.Size);
        Assert.False(definition.Icon.FitToCamera);
        Assert.Equal(0.8f, definition.Icon.CameraFill);
    }
#endif

    [Theory]
    [InlineData(31, 0.72f)]
    [InlineData(2049, 0.72f)]
    [InlineData(512, 0f)]
    [InlineData(512, 2.01f)]
    public void IconSettingsAreBounded(int size, float cameraFill)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PresentationWorkbenchDefinitionBuilder(
                    "example.mod:item",
                    "Preview")
                .WithIconPreview(
                    () => null,
                    default,
                    cameraFill: cameraFill,
                    size: size));
    }

    [Fact]
    public void RegistryIsCaseInsensitiveIdempotentAndOwnerSafe()
    {
        PresentationWorkbenchDefinition definition =
            new PresentationWorkbenchDefinitionBuilder(
                    "Example.Mod:Item",
                    "Example item")
                .WithFirstPersonPreview(() => null)
                .Build();

        Assert.Same(
            definition,
            PresentationWorkbenchRegistry.Register(
                "Example.Mod",
                definition));
        Assert.Same(
            definition,
            PresentationWorkbenchRegistry.Register(
                "example.mod",
                definition));
        Assert.True(
            PresentationWorkbenchRegistry.TryGet(
                "EXAMPLE.MOD:ITEM",
                out PresentationWorkbenchDefinition? resolved));
        Assert.Same(definition, resolved);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => PresentationWorkbenchRegistry.Register(
                    "another.mod",
                    definition));
        Assert.Contains("already owned", exception.Message);
    }

#if MONOMELON
    [Fact]
    public void ExportUsesInvariantCopyReadyCSharp()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            string exported = PresentationWorkbenchExporter.FormatIcon(
                new Vector3(18.5f, -32f, 0f),
                Vector3.one * 0.45f,
                fitToCamera: false,
                cameraFill: 0.8f,
                size: 512);

            Assert.Contains("new Vector3(18.5f, -32f, 0f)", exported);
            Assert.Contains("cameraFill: 0.8f", exported);
            Assert.Contains("fitToCamera: false", exported);
            Assert.DoesNotContain("18,5", exported);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
#endif

    [Fact]
    public void PublicApiRemainsAdditiveAndRuntimeAgnostic()
    {
        Assert.True(typeof(PresentationWorkbenchDefinition).IsSealed);
        Assert.True(typeof(PresentationWorkbenchDefinitionBuilder).IsSealed);
        Assert.True(typeof(PresentationWorkbenchTransform).IsSealed);
        Assert.True(typeof(PresentationWorkbench).IsAbstract);
        Assert.True(typeof(PresentationWorkbench).IsSealed);
        Assert.True(typeof(PresentationWorkbenchRegistry).IsAbstract);
        Assert.True(typeof(PresentationWorkbenchRegistry).IsSealed);
        Assert.Equal(
            new[] { 0, 1, 2 },
            Enum.GetValues<PresentationWorkbenchMode>()
                .Select(value => (int)value));
        Assert.NotNull(
            typeof(PresentationWorkbench).GetMethod(
                nameof(PresentationWorkbench.Open),
                new[] { typeof(string) }));
        Assert.NotNull(
            typeof(PresentationWorkbenchRegistry).GetMethod(
                nameof(PresentationWorkbenchRegistry.Register),
                new[]
                {
                    typeof(string),
                    typeof(PresentationWorkbenchDefinition),
                }));
    }
}
