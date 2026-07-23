using System;

namespace S1API.Products
{
    /// <summary>
    /// INTERNAL: Runtime-independent validation shared by the weed builder and its contract tests.
    /// </summary>
    internal static class WeedDefinitionBuilderContract
    {
        internal const int MaximumPropertyCount = 8;

        internal static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Weed product ID cannot be null or whitespace.", nameof(id));

            var normalized = id.Trim();
            var separator = normalized.IndexOf(':');
            if (separator <= 0 ||
                separator == normalized.Length - 1 ||
                separator != normalized.LastIndexOf(':'))
            {
                throw new ArgumentException(
                    "Weed product ID must be namespaced in the form 'mod-id:product-id'.",
                    nameof(id));
            }

            for (var i = 0; i < normalized.Length; i++)
            {
                var character = normalized[i];
                if (character == ':')
                    continue;

                if (!IsSafeIdCharacter(character))
                {
                    throw new ArgumentException(
                        "Weed product ID may only contain ASCII letters, digits, '.', '_' and '-' around its namespace separator.",
                        nameof(id));
                }
            }

            return normalized;
        }

        internal static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Weed product name cannot be null or whitespace.", nameof(name));

            return name.Trim();
        }

        internal static void ValidatePropertyCounts(int requestedCount, int resolvedCount)
        {
            if (requestedCount < 1)
            {
                throw new InvalidOperationException(
                    "Cannot build a weed product without a property. Use WithProperty(...) or WithProperties(...).");
            }

            if (requestedCount > MaximumPropertyCount)
            {
                throw new InvalidOperationException(
                    $"A weed product can have at most {MaximumPropertyCount} properties.");
            }

            if (requestedCount != resolvedCount)
            {
                throw new InvalidOperationException(
                    "Every supplied property must resolve to a distinct native property. " +
                    "Use vanilla property tokens or registered custom properties and remove duplicates.");
            }
        }

        private static bool IsSafeIdCharacter(char character)
        {
            return character >= 'a' && character <= 'z' ||
                   character >= 'A' && character <= 'Z' ||
                   character >= '0' && character <= '9' ||
                   character == '.' ||
                   character == '_' ||
                   character == '-';
        }
    }
}
