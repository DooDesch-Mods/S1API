using System;
using S1API.Internal.Products;

namespace S1API.Internal.Rendering
{
    internal static class PresentationWorkbenchResolver
    {
        internal const string ProductTarget = "product";
        internal const string ItemTarget = "item";

        internal static bool IsTargetKind(string value) =>
            value.Equals(ProductTarget, StringComparison.OrdinalIgnoreCase) ||
            value.Equals(ItemTarget, StringComparison.OrdinalIgnoreCase);

        internal static bool TryResolve(
            string id,
            string? targetKind,
            out PresentationWorkbenchDefinition? definition,
            out string failure)
        {
            definition = null;
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                failure = "A product ID or item ID is required.";
                return false;
            }

            string normalizedId = id.Trim();
            string? normalizedKind = string.IsNullOrWhiteSpace(targetKind)
                ? null
                : targetKind.Trim().ToLowerInvariant();

            if (normalizedKind == ProductTarget)
            {
                if (ProductPresentationWorkbenchAdapter.TryCreateDefinition(
                        normalizedId,
                        out definition))
                {
                    return true;
                }

                failure =
                    $"No product presentation profile is registered for '{normalizedId}'.";
                return false;
            }

            if (normalizedKind == ItemTarget)
            {
                if (ItemPresentationWorkbenchAdapter.TryCreateDefinition(
                        normalizedId,
                        out definition))
                {
                    return true;
                }

                failure =
                    $"Item '{normalizedId}' has no discoverable equippable visual.";
                return false;
            }

            if (normalizedKind != null)
            {
                failure =
                    $"Unknown presentation target '{targetKind}'. Expected product or item.";
                return false;
            }

            if (ProductPresentationWorkbenchAdapter.TryCreateDefinition(
                    normalizedId,
                    out definition) ||
                ItemPresentationWorkbenchAdapter.TryCreateDefinition(
                    normalizedId,
                    out definition))
            {
                return true;
            }

            failure =
                $"No product profile or item equippable was found for '{normalizedId}'.";
            return false;
        }
    }
}
