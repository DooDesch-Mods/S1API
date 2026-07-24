using System;
using System.Collections.Generic;
using S1API.Internal.Products;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Builds and registers immutable presentation metadata for a logical product kind.
    /// </summary>
    public sealed class ProductKindMetadataBuilder
    {
        private readonly ProductKind _productKind;
        private string? _displayName;
        private Color? _color;
        private Sprite? _icon;
        private int _sortOrder;
        private readonly List<string> _searchAliases = new List<string>();
        private bool _isVisibleInProductManager;

        /// <summary>
        /// Creates a metadata builder for a registered logical product kind.
        /// </summary>
        /// <param name="productKind">The logical product kind to describe.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="productKind"/> is <see langword="null"/>.
        /// </exception>
        public ProductKindMetadataBuilder(ProductKind productKind)
        {
            _productKind = productKind ?? throw new ArgumentNullException(nameof(productKind));
        }

        /// <summary>
        /// Sets the user-facing product-kind name.
        /// </summary>
        /// <param name="displayName">The non-empty display name.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="displayName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="displayName"/> is empty or whitespace.
        /// </exception>
        public ProductKindMetadataBuilder WithDisplayName(string displayName)
        {
            _displayName = NormalizeRequired(displayName, nameof(displayName));
            return this;
        }

        /// <summary>
        /// Sets the presentation color.
        /// </summary>
        /// <param name="color">A color with finite component values.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when any color component is not finite.
        /// </exception>
        public ProductKindMetadataBuilder WithColor(Color color)
        {
            if (!IsFinite(color.r)
                || !IsFinite(color.g)
                || !IsFinite(color.b)
                || !IsFinite(color.a))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(color),
                    color,
                    "Product-kind color components must be finite values.");
            }

            _color = color;
            return this;
        }

        /// <summary>
        /// Sets the icon for a visible Product Manager section.
        /// </summary>
        /// <param name="icon">The section icon.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="icon"/> is <see langword="null"/>.
        /// </exception>
        public ProductKindMetadataBuilder WithIcon(Sprite icon)
        {
            if (ProductKindIconLifetime.IsNullOrDestroyed(icon))
                throw new ArgumentNullException(nameof(icon));

            _icon = icon;
            return this;
        }

        /// <summary>
        /// Sets the relative order among custom Product Manager sections.
        /// </summary>
        /// <param name="sortOrder">The custom-section order; lower values appear first.</param>
        /// <returns>This builder for method chaining.</returns>
        public ProductKindMetadataBuilder WithSortOrder(int sortOrder)
        {
            _sortOrder = sortOrder;
            return this;
        }

        /// <summary>
        /// Replaces the additional case-insensitive search aliases.
        /// </summary>
        /// <param name="searchAliases">The aliases to use.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the array or any alias is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an alias is empty or whitespace.
        /// </exception>
        /// <remarks>
        /// Input order is preserved after case-insensitive deduplication. Because
        /// aliases are exposed as an ordered immutable snapshot, changing their
        /// order is a conflicting registration rather than an idempotent repeat.
        /// </remarks>
        public ProductKindMetadataBuilder WithSearchAliases(params string[] searchAliases)
        {
            if (searchAliases == null)
                throw new ArgumentNullException(nameof(searchAliases));

            var normalizedAliases = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < searchAliases.Length; i++)
            {
                string normalized =
                    NormalizeRequired(searchAliases[i], nameof(searchAliases));
                if (seen.Add(normalized))
                    normalizedAliases.Add(normalized);
            }

            _searchAliases.Clear();
            _searchAliases.AddRange(normalizedAliases);
            return this;
        }

        /// <summary>
        /// Sets whether custom products of this kind appear in the Product Manager.
        /// </summary>
        /// <param name="visible">
        /// <see langword="true"/> to create and maintain a Product Manager section;
        /// otherwise <see langword="false"/>.
        /// </param>
        /// <returns>This builder for method chaining.</returns>
        public ProductKindMetadataBuilder WithProductManagerVisibility(bool visible = true)
        {
            _isVisibleInProductManager = visible;
            return this;
        }

        /// <summary>
        /// Builds and registers the configured metadata.
        /// </summary>
        /// <returns>
        /// The newly registered metadata, or the existing instance for an equivalent registration.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when required metadata is missing, visible metadata lacks a compatibility
        /// drug type or icon, or conflicting metadata is already registered.
        /// </exception>
        public ProductKindMetadata Build()
        {
            if (_displayName == null)
                throw new InvalidOperationException("A product-kind display name is required.");
            if (!_color.HasValue)
                throw new InvalidOperationException("A product-kind color is required.");

            if (_isVisibleInProductManager)
            {
                if (!_productKind.CompatibilityDrugType.HasValue)
                {
                    throw new InvalidOperationException(
                        "A visible Product Manager section requires a compatibility drug type.");
                }

                if (ProductKindIconLifetime.IsNullOrDestroyed(_icon))
                {
                    throw new InvalidOperationException(
                        "A visible Product Manager section requires an icon.");
                }
            }

            var metadata = new ProductKindMetadata(
                _productKind,
                _displayName,
                _color.Value,
                _icon,
                _sortOrder,
                new List<string>(_searchAliases).AsReadOnly(),
                _isVisibleInProductManager);
            return ProductKindMetadataRegistry.Register(metadata);
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);

            string normalized = value.Trim();
            if (normalized.Length == 0)
                throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
            return normalized;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
