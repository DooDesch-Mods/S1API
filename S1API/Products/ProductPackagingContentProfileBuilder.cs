using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Builds an immutable product packaging-content profile.
    /// </summary>
    public sealed class ProductPackagingContentProfileBuilder
    {
        private readonly List<ProductPresentationTransform> _placements =
            new List<ProductPresentationTransform>();
        private Func<GameObject?>? _contentProvider;

        /// <summary>
        /// Sets the reusable mod-owned content prefab source.
        /// </summary>
        /// <param name="provider">A provider that returns a reusable prefab source.</param>
        /// <returns>This builder.</returns>
        public ProductPackagingContentProfileBuilder WithContent(
            Func<GameObject?> provider)
        {
            _contentProvider =
                provider ?? throw new ArgumentNullException(nameof(provider));
            return this;
        }

        /// <summary>
        /// Adds one explicit placement for a clone of the content prefab.
        /// </summary>
        /// <param name="placement">The clone's local transform inside the packaging shell.</param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// When no placements are added, S1API creates one clone and preserves the transform
        /// authored by the provider.
        /// </remarks>
        public ProductPackagingContentProfileBuilder AddPlacement(
            ProductPresentationTransform placement)
        {
            _placements.Add(
                placement ?? throw new ArgumentNullException(nameof(placement)));
            return this;
        }

        /// <summary>
        /// Adds explicit placements for repeated clones of the content prefab.
        /// </summary>
        /// <param name="placements">The clones' local transforms inside the packaging shell.</param>
        /// <returns>This builder.</returns>
        public ProductPackagingContentProfileBuilder AddPlacements(
            params ProductPresentationTransform[] placements)
        {
            if (placements == null)
                throw new ArgumentNullException(nameof(placements));

            for (int i = 0; i < placements.Length; i++)
                AddPlacement(placements[i]);

            return this;
        }

        /// <summary>
        /// Creates an immutable packaging-content profile snapshot.
        /// </summary>
        /// <returns>The packaging-content profile.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no content provider is configured.
        /// </exception>
        public ProductPackagingContentProfile Build()
        {
            if (_contentProvider == null)
            {
                throw new InvalidOperationException(
                    "A packaging content provider must be configured before Build().");
            }

            return new ProductPackagingContentProfile(
                _contentProvider,
                new List<ProductPresentationTransform>(_placements).AsReadOnly());
        }
    }
}
