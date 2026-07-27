using S1API.Internal.NPCWorkbench;
using S1API.Entities.Appearances.AccessoryFields;
using S1API.Entities.Appearances.BodyLayerFields;
using S1API.Entities.Appearances.CustomizationFields;
using S1API.Entities.Appearances.FaceLayerFields;

namespace S1API.Tests.NPCs;

public sealed class NPCWorkbenchExporterTests
{
    [Fact]
    public void ConsoleCommandHasDiscoverablePublicConstructor()
    {
        Assert.NotNull(typeof(NPCWorkbenchCommand).GetConstructor(Type.EmptyTypes));
        Assert.Equal("npcworkbench", new NPCWorkbenchCommand().CommandWord);
    }

    [Fact]
    public void ExportIsDeterministicAndUsesInvariantNumbers()
    {
        var draft = CreateDraft();
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR");

            var first = NPCWorkbenchExporter.Export(draft);
            var second = NPCWorkbenchExporter.Export(draft);

            Assert.True(first.CanExport);
            Assert.Equal(first.Code, second.Code);
            Assert.Contains("appearance.Height = 1.25f;", first.Code);
            Assert.DoesNotContain("1,25f", first.Code);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void ExportEmitsEverySupportedAppearanceControl()
    {
        var draft = CreateDraft();
        var appearance = draft.Appearance;
        appearance.EyeballMaterialIdentifier = "Bright";
        appearance.PupilDilation = 0.8f;
        appearance.EyebrowScale = 1.1f;
        appearance.EyebrowThickness = 0.7f;
        appearance.EyebrowRestingHeight = 0.2f;
        appearance.EyebrowRestingAngle = -0.1f;
        appearance.LeftEye = new NPCWorkbenchEyeSettings(0.4f, 0.6f);
        appearance.RightEye = new NPCWorkbenchEyeSettings(0.45f, 0.55f);
        appearance.HairPath = "hair/long";
        appearance.ImpostorId = "Kyle";
        appearance.FaceLayers.Add(new NPCWorkbenchLayer { Path = "face/freckles" });
        appearance.BodyLayers.Add(new NPCWorkbenchLayer { Path = "clothes/shirt" });
        appearance.Accessories.Add(new NPCWorkbenchLayer { Path = "accessories/glasses" });

        var result = NPCWorkbenchExporter.Export(draft);

        Assert.True(result.CanExport);
        Assert.Contains("appearance.EyeballMaterialIdentifier = \"Bright\";", result.Code);
        Assert.Contains("appearance.LeftEye = (0.4f, 0.6f);", result.Code);
        Assert.Contains("appearance.RightEye = (0.45f, 0.55f);", result.Code);
        Assert.Contains("appearance.WithFaceLayer(\"face/freckles\"", result.Code);
        Assert.Contains("appearance.WithBodyLayer(\"clothes/shirt\"", result.Code);
        Assert.Contains("appearance.WithAccessoryLayer(\"accessories/glasses\"", result.Code);
        Assert.Contains("appearance.WithImpostor(\"Kyle\");", result.Code);
    }

    [Fact]
    public void CatalogIncludesEveryAppearancePathFamilyAndOmitsRemovedPaths()
    {
        Assert.Equal(
            HairStyle.Afro,
            NPCWorkbenchPathCatalog.Find(NPCWorkbenchPathKind.Hair, HairStyle.Afro)?.Path);
        Assert.Equal(
            Face.Neutral,
            NPCWorkbenchPathCatalog.Find(NPCWorkbenchPathKind.FaceLayer, Face.Neutral)?.Path);
        Assert.Equal(
            Shirts.TShirt,
            NPCWorkbenchPathCatalog.Find(NPCWorkbenchPathKind.BodyLayer, Shirts.TShirt)?.Path);
        Assert.Equal(
            Head.Cap,
            NPCWorkbenchPathCatalog.Find(NPCWorkbenchPathKind.Accessory, Head.Cap)?.Path);
        Assert.DoesNotContain(
            NPCWorkbenchPathCatalog.Get(NPCWorkbenchPathKind.Hair),
            entry => entry.MemberName == "Jesus");
    }

    [Fact]
    public void ExportUsesTypedS1ApiPathsWhenCatalogEntriesAreKnown()
    {
        var draft = CreateDraft();
        draft.Appearance.HairPath = HairStyle.Afro;
        draft.Appearance.FaceLayers.Add(new NPCWorkbenchLayer { Path = Face.Neutral });
        draft.Appearance.BodyLayers.Add(new NPCWorkbenchLayer { Path = Shirts.TShirt });
        draft.Appearance.Accessories.Add(new NPCWorkbenchLayer { Path = Head.Cap });

        var result = NPCWorkbenchExporter.Export(draft);

        Assert.Contains(
            "appearance.HairPath = global::S1API.Entities.Appearances.CustomizationFields.HairStyle.Afro;",
            result.Code);
        Assert.Contains(
            "appearance.WithFaceLayer<global::S1API.Entities.Appearances.FaceLayerFields.Face>(" +
            "global::S1API.Entities.Appearances.FaceLayerFields.Face.Neutral,",
            result.Code);
        Assert.Contains(
            "appearance.WithBodyLayer<global::S1API.Entities.Appearances.BodyLayerFields.Shirts>(" +
            "global::S1API.Entities.Appearances.BodyLayerFields.Shirts.TShirt,",
            result.Code);
        Assert.Contains(
            "appearance.WithAccessoryLayer<global::S1API.Entities.Appearances.AccessoryFields.Head>(" +
            "global::S1API.Entities.Appearances.AccessoryFields.Head.Cap,",
            result.Code);
    }

    [Fact]
    public void LayerOrderIsPreservedBecauseItAffectsRendering()
    {
        var draft = CreateDraft();
        draft.Appearance.FaceLayers.Add(new NPCWorkbenchLayer { Path = "face/base" });
        draft.Appearance.FaceLayers.Add(new NPCWorkbenchLayer { Path = "face/top" });

        var result = NPCWorkbenchExporter.Export(draft);

        Assert.True(
            result.Code.IndexOf("\"face/base\"", StringComparison.Ordinal) <
            result.Code.IndexOf("\"face/top\"", StringComparison.Ordinal));
    }

    [Fact]
    public void ExportEscapesResourcePaths()
    {
        var draft = CreateDraft();
        draft.Appearance.HairPath = "hair/\"quoted\"\nvariant";

        var result = NPCWorkbenchExporter.Export(draft);

        Assert.Contains("appearance.HairPath = \"hair/\\\"quoted\\\"\\nvariant\";", result.Code);
    }

    [Fact]
    public void InvalidAppearanceDoesNotProduceCode()
    {
        var draft = CreateDraft();
        draft.Appearance.Height = float.NaN;
        draft.Appearance.LeftEye = new NPCWorkbenchEyeSettings(2f, 0.5f);
        draft.Appearance.FaceLayers.Add(new NPCWorkbenchLayer());

        var result = NPCWorkbenchExporter.Export(draft);

        Assert.False(result.CanExport);
        Assert.Empty(result.Code);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "appearance.height");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "appearance.left-eye-top");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "appearance.face-layer.path");
    }

    [Fact]
    public void CloneDetachesAllAppearanceLayerCollections()
    {
        var draft = CreateDraft();
        draft.Appearance.FaceLayers.Add(new NPCWorkbenchLayer { Path = "face/original" });

        var clone = draft.Clone();
        clone.Appearance.FaceLayers[0].Path = "face/changed";

        Assert.Equal("face/original", draft.Appearance.FaceLayers[0].Path);
    }

    [Fact]
    public void ExportContainsNoNpcBehaviorConfiguration()
    {
        var result = NPCWorkbenchExporter.Export(CreateDraft());

        Assert.DoesNotContain("WithRelationshipDefaults", result.Code);
        Assert.DoesNotContain("WithSchedule", result.Code);
        Assert.DoesNotContain("EnsureCustomer", result.Code);
        Assert.DoesNotContain("EnsureDealer", result.Code);
        Assert.DoesNotContain("EnsureSupplier", result.Code);
        Assert.DoesNotContain("WithInventoryDefaults", result.Code);
    }

    [Theory]
    [InlineData("#112233", "112233FF")]
    [InlineData("AABBCCDD", "AABBCCDD")]
    public void ColorParserAcceptsRgbAndRgbaHex(string input, string expected)
    {
        Assert.True(NPCWorkbenchColor.TryParse(input, out var color));
        Assert.Equal(expected, color.ToHex());
    }

    private static NPCWorkbenchDraft CreateDraft()
    {
        var draft = new NPCWorkbenchDraft();
        draft.Appearance.Height = 1.25f;
        return draft;
    }
}
