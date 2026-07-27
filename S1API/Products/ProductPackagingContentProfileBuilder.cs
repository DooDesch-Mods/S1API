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
        private ProductPackagingContentSource _source =
            ProductPackagingContentSource.RepeatedContent;
        private Func<GameObject?>? _contentProvider;
        private ProductPresentationTransform? _completeVisualTransform;
        private ProductPackagingVisualTemplate? _nativeVisualTemplate;
        private Action<GameObject>? _nativeVisualCustomizer;

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
            _source = ProductPackagingContentSource.RepeatedContent;
            _completeVisualTransform = null;
            _nativeVisualTemplate = null;
            _nativeVisualCustomizer = null;
            return this;
        }

        /// <summary>
        /// Sets one complete mod-owned visual for packaging whose filled form is the product.
        /// </summary>
        /// <param name="provider">A provider that returns a reusable complete visual source.</param>
        /// <param name="transform">
        /// An optional local transform applied to the cloned visual. When omitted, S1API
        /// preserves the transform authored by the provider.
        /// </param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// Use this for packaging such as <c>brick</c>, where the native variants represent
        /// the complete filled item instead of repeated contents inside a shared shell.
        /// The same strategy is applied to stored, equipped, and composite-icon contexts.
        /// </remarks>
        public ProductPackagingContentProfileBuilder WithCompleteFilledVisual(
            Func<GameObject?> provider,
            ProductPresentationTransform? transform = null)
        {
            _contentProvider =
                provider ?? throw new ArgumentNullException(nameof(provider));
            _source = ProductPackagingContentSource.CompleteFilledVisual;
            _completeVisualTransform = transform;
            _nativeVisualTemplate = null;
            _nativeVisualCustomizer = null;
            return this;
        }

        /// <summary>
        /// Clones one game-owned filled visual template and optionally customizes the clone.
        /// </summary>
        /// <param name="template">The presentation-only game-owned visual template.</param>
        /// <param name="customize">
        /// An optional callback that receives each newly cloned visual before it is shown.
        /// Only the clone is passed; shared game-owned prefabs are never exposed or mutated.
        /// </param>
        /// <param name="transform">
        /// An optional local transform applied to the clone. When omitted, S1API preserves
        /// the context-specific transform authored by the game-owned template.
        /// </param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="template"/> is not a defined value.
        /// </exception>
        /// <remarks>
        /// The template controls presentation only and does not alter the custom product's
        /// logical kind or native execution strategy. The callback should assign only
        /// mod-owned materials or other local presentation data.
        /// </remarks>
        public ProductPackagingContentProfileBuilder WithNativeFilledVisualScaffold(
            ProductPackagingVisualTemplate template,
            Action<GameObject>? customize = null,
            ProductPresentationTransform? transform = null)
        {
            if (!Enum.IsDefined(typeof(ProductPackagingVisualTemplate), template))
                throw new ArgumentOutOfRangeException(nameof(template));

            _source = ProductPackagingContentSource.NativeFilledVisualScaffold;
            _contentProvider = null;
            _completeVisualTransform = transform;
            _nativeVisualTemplate = template;
            _nativeVisualCustomizer = customize;
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
        /// Thrown when no visual source is configured or placements are combined with a
        /// complete-filled strategy.
        /// </exception>
        public ProductPackagingContentProfile Build()
        {
            if (_source != ProductPackagingContentSource.NativeFilledVisualScaffold &&
                _contentProvider == null)
            {
                throw new InvalidOperationException(
                    "A packaging content provider must be configured before Build().");
            }

            if (_source != ProductPackagingContentSource.RepeatedContent &&
                _placements.Count > 0)
            {
                throw new InvalidOperationException(
                    "Explicit content placements cannot be combined with a complete " +
                    "filled visual. Configure its optional transform instead.");
            }

            return new ProductPackagingContentProfile(
                _source,
                _contentProvider,
                new List<ProductPresentationTransform>(_placements).AsReadOnly(),
                _completeVisualTransform,
                _nativeVisualTemplate,
                _nativeVisualCustomizer);
        }
    }
}
