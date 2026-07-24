using System;
using System.Linq;
using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductManifestDataTests
{
    [Fact]
    public void CanonicalManifestHashIsIndependentOfConstructionOrder()
    {
        CustomProductManifestEntryData first = CreateEntry("example:alpha");
        CustomProductManifestEntryData second = CreateEntry("example:beta");

        string left = CustomProductManifestData.ComputeManifestHash(
            new[] { first, second });
        string right = CustomProductManifestData.ComputeManifestHash(
            new[] { second, first }.OrderBy(entry => entry.ProductId,
                StringComparer.OrdinalIgnoreCase).ToArray());

        Assert.Equal(left, right);
    }

    [Fact]
    public void ManifestRejectsDuplicateAndNonCanonicalEntries()
    {
        CustomProductManifestEntryData first = CreateEntry("example:alpha");
        CustomProductManifestEntryData duplicate = CreateEntry("EXAMPLE:ALPHA");
        var manifest = new CustomProductManifestData
        {
            SessionId = new string('a', 32),
            Entries = new[] { first, duplicate }
        };
        manifest.CompatibilityHash =
            CustomProductManifestData.ComputeManifestHash(manifest.Entries);

        Assert.False(CustomProductManifestData.TryValidate(manifest, out string failure));
        Assert.Contains("duplicate", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestRejectsUnsupportedVersionAndPayloadBounds()
    {
        var manifest = new CustomProductManifestData
        {
            ProtocolVersion = CustomProductManifestData.CurrentProtocolVersion + 1,
            SessionId = new string('a', 32),
            Entries = Array.Empty<CustomProductManifestEntryData>(),
            CompatibilityHash = CustomProductManifestData.ComputeManifestHash(
                Array.Empty<CustomProductManifestEntryData>())
        };

        Assert.False(CustomProductManifestData.TryValidate(manifest, out string failure));
        Assert.Contains("protocol", failure, StringComparison.Ordinal);
        Assert.False(CustomProductManifestData.TryDeserialize(
            new string('x', CustomProductManifestData.MaximumPayloadLength + 1),
            out _,
            out string payloadFailure));
        Assert.Contains("size", payloadFailure, StringComparison.Ordinal);

        Assert.False(CustomProductManifestData.TryDeserialize(
            new string('é', (CustomProductManifestData.MaximumPayloadLength / 2) + 1),
            out _,
            out string utf8PayloadFailure));
        Assert.Contains("size", utf8PayloadFailure, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeDoesNotMutateTheManifestWhenItSucceedsOrFails()
    {
        var manifest = new CustomProductManifestData
        {
            SessionId = "original-session",
            Entries = Array.Empty<CustomProductManifestEntryData>()
        };
        manifest.CompatibilityHash =
            CustomProductManifestData.ComputeManifestHash(manifest.Entries);

        string payload = manifest.Serialize(new string('a', 32));

        Assert.Equal("original-session", manifest.SessionId);
        Assert.True(CustomProductManifestData.TryDeserialize(
            payload,
            out CustomProductManifestData serialized,
            out string failure),
            failure);
        Assert.Equal(new string('a', 32), serialized.SessionId);

        manifest.Entries = new[] { CreateEntry(new string('x', 65536)) };
        Assert.Throws<InvalidOperationException>(
            () => manifest.Serialize(new string('b', 32)));
        Assert.Equal("original-session", manifest.SessionId);
    }

    [Fact]
    public void ManifestRejectsEntryCountAndUnsafeIdentifierBounds()
    {
        var tooManyEntries = new CustomProductManifestData
        {
            SessionId = new string('a', 32),
            Entries = Enumerable.Range(
                    0,
                    CustomProductManifestData.MaximumEntryCount + 1)
                .Select(index => CreateEntry("example:" + index))
                .ToArray()
        };
        tooManyEntries.CompatibilityHash =
            CustomProductManifestData.ComputeManifestHash(tooManyEntries.Entries);

        Assert.False(CustomProductManifestData.TryValidate(
            tooManyEntries,
            out string countFailure));
        Assert.Contains("entry count", countFailure, StringComparison.Ordinal);

        CustomProductManifestEntryData unsafeEntry = CreateEntry("example:\nforged-log");
        Assert.False(unsafeEntry.IsValid());

        CustomProductManifestEntryData longIdentifier = CreateEntry(
            new string('x', CustomProductManifestData.MaximumIdentifierLength + 1));
        Assert.False(longIdentifier.IsValid());

        CustomProductManifestEntryData excessivePackaging = CreateEntry("example:packaging");
        excessivePackaging.PackagingIds = Enumerable.Range(
                0,
                CustomProductManifestData.MaximumPackagingCount + 1)
            .Select(index => "package:" + index)
            .ToArray();
        Assert.False(excessivePackaging.IsValid());
    }

    [Fact]
    public void StableIdentifierCasingDoesNotChangeCompatibilityHash()
    {
        CustomProductManifestEntryData lower = CreateEntry("example:alpha");
        CustomProductManifestEntryData upper = CreateEntry("EXAMPLE:ALPHA");
        upper.OwnerId = lower.OwnerId.ToUpperInvariant();
        upper.ProductKindId = lower.ProductKindId.ToUpperInvariant();
        upper.ProviderId = lower.ProviderId!.ToUpperInvariant();
        upper.RepresentationTemplateId =
            lower.RepresentationTemplateId.ToUpperInvariant();
        upper.PresentationProfileId =
            lower.PresentationProfileId.ToUpperInvariant();
        upper.PackagingIds = lower.PackagingIds
            .Select(value => value.ToUpperInvariant())
            .ToArray();

        Assert.Equal(
            CustomProductManifestData.ComputeManifestHash(new[] { lower }),
            CustomProductManifestData.ComputeManifestHash(new[] { upper }));
    }

    [Fact]
    public void ManifestRejectsTamperedCompatibilityHash()
    {
        var manifest = new CustomProductManifestData
        {
            SessionId = new string('a', 32),
            Entries = new[] { CreateEntry("example:alpha") },
            CompatibilityHash = new string('0', 64)
        };

        Assert.False(CustomProductManifestData.TryValidate(manifest, out string failure));
        Assert.Contains("compatibility hash", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestEntryContainsNoProviderPayload()
    {
        string[] names = typeof(CustomProductManifestEntryData)
            .GetFields()
            .Select(field => field.Name)
            .ToArray();

        Assert.DoesNotContain("ProviderData", names);
        Assert.Contains("ProviderId", names);
        Assert.Contains("CompatibilityHash", names);
    }

    [Fact]
    public void ManifestRejectsMalformedDuplicateAndUnknownJsonMembers()
    {
        Assert.False(CustomProductManifestData.TryDeserialize(
            """{"ProtocolVersion":1,"ProtocolVersion":1}""",
            out _,
            out string duplicateFailure));
        Assert.Contains("malformed", duplicateFailure, StringComparison.Ordinal);

        Assert.False(CustomProductManifestData.TryDeserialize(
            """{"Unexpected":"data"}""",
            out _,
            out string unknownFailure));
        Assert.Contains("malformed", unknownFailure, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestRejectsInvalidEntryScalars()
    {
        CustomProductManifestEntryData entry = CreateEntry("example:alpha");
        entry.CompatibilityDrugType = int.MaxValue;
        var manifest = new CustomProductManifestData
        {
            SessionId = new string('a', 32),
            Entries = new[] { entry }
        };
        manifest.CompatibilityHash =
            CustomProductManifestData.ComputeManifestHash(manifest.Entries);

        Assert.False(CustomProductManifestData.TryValidate(manifest, out string failure));
        Assert.Contains("invalid", failure, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ProviderId")]
    [InlineData("ProviderVersion")]
    [InlineData("ProviderAvailable")]
    [InlineData("DescriptorFormatVersion")]
    [InlineData("PresentationProfileId")]
    [InlineData("PackagingIds")]
    public void CompatibilityHashChangesForCompatibilityRelevantMetadata(string field)
    {
        CustomProductManifestEntryData baseline = CreateEntry("example:alpha");
        CustomProductManifestEntryData changed = CreateEntry("example:alpha");
        switch (field)
        {
            case "ProviderId":
                changed.ProviderId = "example:other-provider";
                break;
            case "ProviderVersion":
                changed.ProviderVersion++;
                break;
            case "ProviderAvailable":
                changed.ProviderAvailable = false;
                break;
            case "DescriptorFormatVersion":
                changed.DescriptorFormatVersion++;
                break;
            case "PresentationProfileId":
                changed.PresentationProfileId = "example:other-profile";
                break;
            case "PackagingIds":
                changed.PackagingIds = new[] { "bag" };
                break;
        }

        Assert.NotEqual(
            CustomProductManifestData.ComputeManifestHash(new[] { baseline }),
            CustomProductManifestData.ComputeManifestHash(new[] { changed }));
    }

    [Fact]
    public void MismatchDiagnosticsIdentifyProviderWithoutExposingProviderData()
    {
        CustomProductManifestEntryData hostEntry = CreateEntry("example:alpha");
        CustomProductManifestEntryData localEntry = CreateEntry("example:alpha");
        localEntry.ProviderVersion++;
        var host = new CustomProductManifestData { Entries = new[] { hostEntry } };
        var local = new CustomProductManifestData { Entries = new[] { localEntry } };

        string diagnostic =
            CustomProductManifestData.DescribeCompatibilityMismatch(host, local);

        Assert.Contains("provider", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data", diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    private static CustomProductManifestEntryData CreateEntry(string productId)
    {
        return new CustomProductManifestEntryData
        {
            ProductId = productId,
            OwnerId = "example",
            ProductKindId = "example:kind",
            CompatibilityDrugType = 0,
            DescriptorFormatVersion = 1,
            ProviderId = "example:provider",
            ProviderVersion = 1,
            ProviderAvailable = true,
            RepresentationTemplateId = "ogkush",
            PresentationProfileId = "example:" + productId,
            PackagingIds = new[] { "bag", "jar" },
            CompatibilityHash = new string('a', 64)
        };
    }
}
