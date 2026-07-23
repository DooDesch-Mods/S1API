using System;
using System.Collections.Generic;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Provides process-lifetime lookup access to logical product-kind descriptors.
    /// </summary>
    public static class ProductKindRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ProductKindDescriptor> Descriptors =
            new Dictionary<string, ProductKindDescriptor>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a read-only snapshot of all registered product kinds, ordered by identifier.
        /// </summary>
        public static IReadOnlyCollection<ProductKindDescriptor> All
        {
            get
            {
                lock (Gate)
                {
                    var snapshot = new List<ProductKindDescriptor>(Descriptors.Values);
                    snapshot.Sort((left, right) =>
                        StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id));
                    return snapshot.AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Gets a registered product kind by identifier.
        /// </summary>
        /// <param name="id">The stable, namespaced product-kind identifier.</param>
        /// <returns>The registered descriptor, or <see langword="null"/> when the identifier is not registered.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="id"/> is empty or does not use the supported namespaced format.
        /// </exception>
        public static ProductKindDescriptor? Get(string id)
        {
            TryGet(id, out ProductKindDescriptor? descriptor);
            return descriptor;
        }

        /// <summary>
        /// Attempts to get a registered product kind by identifier.
        /// </summary>
        /// <param name="id">The stable, namespaced product-kind identifier.</param>
        /// <param name="descriptor">
        /// The registered descriptor when found; otherwise <see langword="null"/>.
        /// </param>
        /// <returns><see langword="true"/> when the identifier is registered; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="id"/> is empty or does not use the supported namespaced format.
        /// </exception>
        public static bool TryGet(string id, out ProductKindDescriptor? descriptor)
        {
            string normalizedId = ProductKindId.Normalize(id, nameof(id));
            lock (Gate)
            {
                return Descriptors.TryGetValue(normalizedId, out descriptor);
            }
        }

        internal static ProductKindDescriptor Register(ProductKindDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            lock (Gate)
            {
                if (!Descriptors.TryGetValue(descriptor.Id, out ProductKindDescriptor? existing))
                {
                    Descriptors.Add(descriptor.Id, descriptor);
                    return descriptor;
                }

                if (existing.IsEquivalentTo(descriptor))
                {
                    return existing;
                }

                throw new InvalidOperationException(
                    $"Product kind ID '{descriptor.Id}' is already registered with compatibility drug type "
                    + $"'{FormatCompatibility(existing.CompatibilityDrugType)}'; the conflicting registration requested "
                    + $"'{FormatCompatibility(descriptor.CompatibilityDrugType)}'. Product kind IDs are case-insensitive. "
                    + "Use a unique namespaced ID or register equivalent metadata.");
            }
        }

        private static string FormatCompatibility(DrugType? drugType)
        {
            return drugType?.ToString() ?? "none";
        }
    }
}
