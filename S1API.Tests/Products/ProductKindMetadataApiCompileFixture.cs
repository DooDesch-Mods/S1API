using S1API.Products;
using UnityEngine;

namespace S1API.Tests.Products;

internal static class ProductKindMetadataApiCompileFixture
{
    internal static bool RepresentativeCallerCompiles(
        ProductKind productKind,
        Sprite icon,
        string query)
    {
        ProductKindMetadata metadata =
            new ProductKindMetadataBuilder(productKind)
                .WithDisplayName("MDMA")
                .WithColor(new Color(0.84f, 0.24f, 0.72f))
                .WithIcon(icon)
                .WithSortOrder(50)
                .WithSearchAliases("ecstasy", "molly")
                .WithProductManagerVisibility()
                .Build();

        ProductKindMetadata? byKind =
            ProductKindMetadataRegistry.Get(productKind);
        ProductKindMetadata? byId =
            ProductKindMetadataRegistry.Get(productKind.Id);
        bool foundByKind =
            ProductKindMetadataRegistry.TryGet(productKind, out _);
        bool foundById =
            ProductKindMetadataRegistry.TryGet(productKind.Id, out _);
        IReadOnlyCollection<ProductKindMetadata> all =
            ProductKindMetadataRegistry.All;

        return metadata.MatchesSearch(query)
               && byKind == metadata
               && byId == metadata
               && foundByKind
               && foundById
               && all.Contains(metadata);
    }
}
