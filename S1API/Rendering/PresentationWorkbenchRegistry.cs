using System;
using System.Collections.Generic;
using S1API.Internal.Products;

namespace S1API.Rendering
{
    /// <summary>
    /// Registers process-lifetime definitions for the presentation workbench.
    /// </summary>
    /// <remarks>
    /// IDs and owner IDs are case-insensitive. Repeating the same definition instance is
    /// idempotent. Product presentation profiles are resolved automatically when no explicit
    /// workbench definition owns the requested ID.
    /// </remarks>
    public static class PresentationWorkbenchRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Registration> Registrations =
            new Dictionary<string, Registration>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a mod-owned workbench definition.
        /// </summary>
        /// <param name="ownerId">The stable non-empty ID of the owning mod.</param>
        /// <param name="definition">The immutable definition to register.</param>
        /// <returns>
        /// The registered definition, or the retained definition after an idempotent call.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="ownerId"/> or <paramref name="definition"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="ownerId"/> is empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the ID is owned by another mod or the owner attempts to replace its
        /// existing definition instance.
        /// </exception>
        public static PresentationWorkbenchDefinition Register(
            string ownerId,
            PresentationWorkbenchDefinition definition)
        {
            string normalizedOwnerId = NormalizeRequired(ownerId, nameof(ownerId));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            lock (Gate)
            {
                if (Registrations.TryGetValue(
                        definition.Id,
                        out Registration? existing))
                {
                    if (!string.Equals(
                            existing.OwnerId,
                            normalizedOwnerId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Presentation workbench ID '{definition.Id}' is already owned by " +
                            $"'{existing.OwnerId}' and cannot be registered by " +
                            $"'{normalizedOwnerId}'.");
                    }

                    if (!ReferenceEquals(existing.Definition, definition))
                    {
                        throw new InvalidOperationException(
                            $"Presentation workbench ID '{definition.Id}' is already registered " +
                            "by this owner with another definition. Reuse the original definition " +
                            "or choose another stable ID.");
                    }

                    return existing.Definition;
                }

                Registrations.Add(
                    definition.Id,
                    new Registration(normalizedOwnerId, definition));
                return definition;
            }
        }

        /// <summary>
        /// Resolves an explicit definition or an automatically exposed product profile.
        /// </summary>
        /// <param name="id">The stable namespaced workbench or product-profile ID.</param>
        /// <param name="definition">The resolved definition when this method returns true.</param>
        /// <returns>True if a definition is available; otherwise, false.</returns>
        public static bool TryGet(
            string id,
            out PresentationWorkbenchDefinition? definition)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                return false;
            }

            string normalizedId = id.Trim();
            lock (Gate)
            {
                if (Registrations.TryGetValue(
                        normalizedId,
                        out Registration? registration))
                {
                    definition = registration.Definition;
                    return true;
                }
            }

            return ProductPresentationWorkbenchAdapter.TryCreateDefinition(
                normalizedId,
                out definition);
        }

        /// <summary>
        /// Gets a snapshot of explicitly registered workbench IDs.
        /// </summary>
        /// <returns>A case-preserving snapshot of the registered IDs.</returns>
        public static IReadOnlyList<string> GetRegisteredIds()
        {
            lock (Gate)
            {
                var result = new List<string>(Registrations.Count);
                foreach (Registration registration in Registrations.Values)
                    result.Add(registration.Definition.Id);
                return result.AsReadOnly();
            }
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
                Registrations.Clear();
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);

            string normalized = value.Trim();
            if (normalized.Length == 0)
                throw new ArgumentException("Value cannot be empty.", parameterName);
            return normalized;
        }

        private sealed class Registration
        {
            internal Registration(
                string ownerId,
                PresentationWorkbenchDefinition definition)
            {
                OwnerId = ownerId;
                Definition = definition;
            }

            internal string OwnerId { get; }

            internal PresentationWorkbenchDefinition Definition { get; }
        }
    }
}
