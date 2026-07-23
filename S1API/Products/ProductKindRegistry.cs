using System;
using System.Collections.Generic;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Provides process-lifetime lookup access to logical product kinds.
    /// </summary>
    public static class ProductKindRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ProductKind> Kinds =
            new Dictionary<string, ProductKind>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a read-only snapshot of all registered product kinds, ordered by identifier.
        /// </summary>
        public static IReadOnlyCollection<ProductKind> All
        {
            get
            {
                lock (Gate)
                {
                    var snapshot = new List<ProductKind>(Kinds.Values);
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
        /// <returns>The registered product kind, or <see langword="null"/> when the identifier is not registered.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="id"/> is empty or does not use the supported namespaced format.
        /// </exception>
        public static ProductKind? Get(string id)
        {
            TryGet(id, out ProductKind? productKind);
            return productKind;
        }

        /// <summary>
        /// Attempts to get a registered product kind by identifier.
        /// </summary>
        /// <param name="id">The stable, namespaced product-kind identifier.</param>
        /// <param name="productKind">
        /// The registered product kind when found; otherwise <see langword="null"/>.
        /// </param>
        /// <returns><see langword="true"/> when the identifier is registered; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="id"/> is empty or does not use the supported namespaced format.
        /// </exception>
        public static bool TryGet(string id, out ProductKind? productKind)
        {
            string normalizedId = ProductKindId.Normalize(id, nameof(id));
            lock (Gate)
            {
                return Kinds.TryGetValue(normalizedId, out productKind);
            }
        }

        internal static ProductKind Register(ProductKind productKind)
        {
            if (productKind == null)
            {
                throw new ArgumentNullException(nameof(productKind));
            }

            lock (Gate)
            {
                if (!Kinds.TryGetValue(productKind.Id, out ProductKind? existing))
                {
                    Kinds.Add(productKind.Id, productKind);
                    return productKind;
                }

                if (existing.IsEquivalentTo(productKind))
                {
                    return existing;
                }

                throw new InvalidOperationException(
                    $"Product kind ID '{productKind.Id}' is already registered with compatibility drug type "
                    + $"'{FormatCompatibility(existing.CompatibilityDrugType)}'; the conflicting registration requested "
                    + $"'{FormatCompatibility(productKind.CompatibilityDrugType)}'. Product kind IDs are case-insensitive. "
                    + "Use a unique namespaced ID or register equivalent metadata.");
            }
        }

        private static string FormatCompatibility(DrugType? drugType)
        {
            return drugType?.ToString() ?? "none";
        }
    }
}
