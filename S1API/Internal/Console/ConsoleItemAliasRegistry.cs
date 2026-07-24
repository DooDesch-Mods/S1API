using System;
using System.Collections.Generic;

namespace S1API.Internal.Console
{
    /// <summary>
    /// INTERNAL: Process-lifetime console alias registration and resolution.
    /// </summary>
    internal static class ConsoleItemAliasRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, string> Aliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal static void Register(
            string alias,
            string canonicalItemId,
            Func<string, bool> itemExists)
        {
            if (itemExists == null)
                throw new ArgumentNullException(nameof(itemExists));

            string normalizedAlias = NormalizeAlias(alias);
            string normalizedCanonicalItemId =
                NormalizeCanonicalItemId(canonicalItemId);

            lock (Gate)
            {
                if (IsIdempotentOrThrow(
                        normalizedAlias,
                        normalizedCanonicalItemId))
                    return;
            }

            if (!itemExists(normalizedCanonicalItemId))
            {
                throw new ArgumentException(
                    $"Canonical item ID '{normalizedCanonicalItemId}' is not " +
                    "registered. Register the item before its console alias.",
                    nameof(canonicalItemId));
            }

            if (itemExists(normalizedAlias))
            {
                throw new InvalidOperationException(
                    $"Console item alias '{normalizedAlias}' is already a " +
                    "native or canonical item ID and cannot be shadowed.");
            }

            lock (Gate)
            {
                if (IsIdempotentOrThrow(
                        normalizedAlias,
                        normalizedCanonicalItemId))
                    return;

                Aliases.Add(normalizedAlias, normalizedCanonicalItemId);
            }
        }

        internal static bool TryResolveForGive(
            string itemCode,
            Func<string, bool> itemExists,
            out string canonicalItemId)
        {
            canonicalItemId = string.Empty;
            if (string.IsNullOrWhiteSpace(itemCode) || itemExists == null)
                return false;

            string normalizedItemCode = itemCode.Trim();
            if (itemExists(normalizedItemCode))
                return false;

            string? registeredCanonicalItemId;
            lock (Gate)
            {
                if (!Aliases.TryGetValue(
                        normalizedItemCode,
                        out registeredCanonicalItemId))
                {
                    return false;
                }
            }

            if (registeredCanonicalItemId == null ||
                !itemExists(registeredCanonicalItemId))
                return false;

            canonicalItemId = registeredCanonicalItemId;
            return true;
        }

        internal static string ResolveItemCodeForGive(
            string itemCode,
            Func<string, bool> itemExists)
        {
            return TryResolveForGive(
                itemCode,
                itemExists,
                out string canonicalItemId)
                ? canonicalItemId
                : itemCode;
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
                Aliases.Clear();
        }

        private static string NormalizeAlias(string alias)
        {
            if (alias == null)
                throw new ArgumentNullException(nameof(alias));

            string normalized = alias.Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException(
                    "Console item aliases cannot be empty or whitespace.",
                    nameof(alias));
            }

            for (int i = 0; i < normalized.Length; i++)
            {
                char character = normalized[i];
                if (IsAsciiLetterOrDigit(character) ||
                    character == '.' ||
                    character == '_' ||
                    character == '-' ||
                    character == '/')
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Console item alias contains invalid character " +
                    $"'{character}' at position {i}. Use ASCII letters, " +
                    "digits, '.', '_', '-', or '/'; aliases do not include a " +
                    "namespace.",
                    nameof(alias));
            }

            return normalized;
        }

        private static bool IsIdempotentOrThrow(
            string alias,
            string canonicalItemId)
        {
            if (!Aliases.TryGetValue(
                    alias,
                    out string? existingCanonicalItemId))
            {
                return false;
            }

            if (string.Equals(
                    existingCanonicalItemId,
                    canonicalItemId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            throw new InvalidOperationException(
                $"Console item alias '{alias}' is already registered for " +
                $"'{existingCanonicalItemId}' and cannot also target " +
                $"'{canonicalItemId}'.");
        }

        private static string NormalizeCanonicalItemId(string canonicalItemId)
        {
            if (canonicalItemId == null)
                throw new ArgumentNullException(nameof(canonicalItemId));

            string normalized = canonicalItemId.Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException(
                    "Canonical item IDs cannot be empty or whitespace.",
                    nameof(canonicalItemId));
            }

            return normalized;
        }

        private static bool IsAsciiLetterOrDigit(char character)
        {
            return character >= 'a' && character <= 'z'
                   || character >= 'A' && character <= 'Z'
                   || character >= '0' && character <= '9';
        }
    }
}
