using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// INTERNAL: Runtime-independent validation for generic custom products.
    /// </summary>
    internal static class CustomProductDefinitionBuilderContract
    {
        internal const int MaximumPropertyCount = 8;
        internal const float MinimumProductPrice = 1f;
        internal const float MaximumProductPrice = 999f;

        internal static string NormalizeId(string id)
        {
            return ProductKindId.Normalize(id, nameof(id));
        }

        internal static string GetOwnerId(string productId)
        {
            int separatorIndex = productId.IndexOf(':');
            return productId.Substring(0, separatorIndex);
        }

        internal static string NormalizeName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            string normalized = name.Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException(
                    "Custom product names cannot be empty or whitespace.",
                    nameof(name));
            }

            return normalized;
        }

        internal static string NormalizeDescription(string description)
        {
            if (description == null)
                throw new ArgumentNullException(nameof(description));

            return description.Trim();
        }

        internal static float NormalizeProductPrice(float productPrice)
        {
            if (float.IsNaN(productPrice) || float.IsInfinity(productPrice))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(productPrice),
                    productPrice,
                    "Product price must be finite.");
            }

            float clamped = Math.Max(
                MinimumProductPrice,
                Math.Min(MaximumProductPrice, productPrice));
            return (float)Math.Round(clamped);
        }

        internal static float NormalizeAddictiveness(float addictiveness)
        {
            if (float.IsNaN(addictiveness) || float.IsInfinity(addictiveness))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(addictiveness),
                    addictiveness,
                    "Base addictiveness must be finite.");
            }

            return Math.Max(0f, Math.Min(1f, addictiveness));
        }

        internal static void ValidateQuality(Quality quality, string parameterName)
        {
            if (!Enum.IsDefined(typeof(Quality), quality))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    quality,
                    "Quality must be a defined S1API.Products.Quality value.");
            }
        }

        internal static void ValidateEffectDuration(int seconds, string parameterName)
        {
            if (seconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    seconds,
                    "Effect durations cannot be negative.");
            }
        }

        internal static void ValidatePropertyCounts(int requestedCount, int resolvedCount)
        {
            if (requestedCount != resolvedCount)
            {
                throw new InvalidOperationException(
                    "Every supplied property must resolve to a distinct native property. " +
                    "Use vanilla property tokens or registered custom properties and remove duplicates.");
            }

            if (resolvedCount > MaximumPropertyCount)
            {
                throw new InvalidOperationException(
                    $"A custom product can have at most {MaximumPropertyCount} properties.");
            }
        }
    }
}
