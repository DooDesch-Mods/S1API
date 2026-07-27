using System.Globalization;
using S1API.Internal.Rendering;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Tests.Rendering;

public sealed class PresentationWorkbenchTests
{
    [Theory]
    [InlineData("product")]
    [InlineData("PRODUCT")]
    [InlineData("item")]
    [InlineData("avatar")]
    public void ResolverRecognizesInternalTargetKinds(string value)
    {
        Assert.True(PresentationWorkbenchResolver.IsTargetKind(value));
    }

    [Fact]
    public void ResolverRejectsUnknownTargetWithoutUsingRuntimeRegistries()
    {
        Assert.False(
            PresentationWorkbenchResolver.TryResolve(
                "example",
                "unknown",
                out PresentationWorkbenchDefinition? definition,
                out string failure));
        Assert.Null(definition);
        Assert.Contains("Unknown presentation target", failure);
    }

    [Fact]
    public void WorkbenchDoesNotExportAModFacingApi()
    {
        Type[] exportedTypes = typeof(IconFactory).Assembly.GetExportedTypes();

        Assert.DoesNotContain(
            exportedTypes,
            type => type.Name.StartsWith(
                "PresentationWorkbench",
                StringComparison.Ordinal));
    }

#if MONOMELON
    [Fact]
    public void ExportUsesExistingApisAndInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            string product =
                PresentationWorkbenchExporter.FormatTransform(
                    PresentationWorkbenchExportKind.ProductTransform,
                    new Vector3(0f, -0.16f, 0f),
                    new Vector3(0f, 270f, 0f),
                    Vector3.one * 2.25f);
            string itemVisual =
                PresentationWorkbenchExporter.FormatTransform(
                    PresentationWorkbenchExportKind.LocalTransformAssignments,
                    Vector3.zero,
                    new Vector3(0f, 180f, 0f),
                    Vector3.one * 1.8f);
            string icon =
                PresentationWorkbenchExporter.FormatIcon(
                    new Vector3(18.5f, -32f, 0f),
                    Vector3.one * 0.45f,
                    fitToCamera: false,
                    cameraFill: 0.8f,
                    size: 512);

            Assert.StartsWith("new ProductPresentationTransform(", product);
            Assert.StartsWith(
                "visual.transform.localPosition =",
                itemVisual);
            Assert.Contains("IconFactory.GenerateIconSprite(", icon);
            Assert.Contains("new Vector3(18.5f, -32f, 0f)", icon);
            Assert.Contains("cameraFill: 0.8f", icon);
            Assert.DoesNotContain("PresentationWorkbench", icon);
            Assert.DoesNotContain("18,5", icon);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
#endif
}
