using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Describes the presentation and Product Manager behavior of a logical product kind.
    /// </summary>
    /// <remarks>
    /// Metadata is immutable and is registered separately from product definitions,
    /// discovery, and listing state.
    /// </remarks>
    public sealed class ProductKindMetadata
    {
        private readonly IReadOnlyList<string> _searchAliases;

        /// <summary>
        /// Gets the logical product kind described by this metadata.
        /// </summary>
        public ProductKind ProductKind { get; }

        /// <summary>
        /// Gets the user-facing product-kind name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the color used for native metadata and Product Manager presentation.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Gets the optional icon used by a visible Product Manager section.
        /// </summary>
        public Sprite? Icon { get; }

        /// <summary>
        /// Gets the relative order of this product kind among custom Product Manager sections.
        /// </summary>
        public int SortOrder { get; }

        /// <summary>
        /// Gets the additional case-insensitive terms that identify this product kind.
        /// </summary>
        public IReadOnlyList<string> SearchAliases => _searchAliases;

        /// <summary>
        /// Gets whether custom products of this kind are shown in the Product Manager.
        /// </summary>
        public bool IsVisibleInProductManager { get; }

        internal ProductKindMetadata(
            ProductKind productKind,
            string displayName,
            Color color,
            Sprite? icon,
            int sortOrder,
            IReadOnlyList<string> searchAliases,
            bool isVisibleInProductManager)
        {
            ProductKind = productKind;
            DisplayName = displayName;
            Color = color;
            Icon = icon;
            SortOrder = sortOrder;
            _searchAliases = searchAliases;
            IsVisibleInProductManager = isVisibleInProductManager;
        }

        /// <summary>
        /// Determines whether a search term matches this metadata's identifier,
        /// display name, or aliases.
        /// </summary>
        /// <param name="searchTerm">The term to match.</param>
        /// <returns><see langword="true"/> when any searchable value contains the term.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="searchTerm"/> is <see langword="null"/>.
        /// </exception>
        public bool MatchesSearch(string searchTerm)
        {
            if (searchTerm == null)
                throw new ArgumentNullException(nameof(searchTerm));

            string normalized = searchTerm.Trim();
            if (normalized.Length == 0)
                return true;

            if (Contains(ProductKind.Id, normalized) || Contains(DisplayName, normalized))
                return true;

            for (int i = 0; i < _searchAliases.Count; i++)
            {
                if (Contains(_searchAliases[i], normalized))
                    return true;
            }

            return false;
        }

        internal bool IsEquivalentTo(ProductKindMetadata other)
        {
            if (!ProductKind.IsEquivalentTo(other.ProductKind)
                || !string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                || !HasSameColor(other)
                || !ReferenceEquals(Icon, other.Icon)
                || SortOrder != other.SortOrder
                || IsVisibleInProductManager != other.IsVisibleInProductManager
                || _searchAliases.Count != other._searchAliases.Count)
            {
                return false;
            }

            for (int i = 0; i < _searchAliases.Count; i++)
            {
                if (!string.Equals(
                        _searchAliases[i],
                        other._searchAliases[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool HasSameColor(ProductKindMetadata other)
        {
            return Color.r.Equals(other.Color.r)
                   && Color.g.Equals(other.Color.g)
                   && Color.b.Equals(other.Color.b)
                   && Color.a.Equals(other.Color.a);
        }

        private static bool Contains(string value, string searchTerm)
        {
            return value.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
