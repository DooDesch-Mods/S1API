using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Builds an immutable custom product presentation profile.
    /// </summary>
    public sealed class ProductPresentationProfileBuilder
    {
        private readonly Dictionary<ProductPresentationContext, Func<GameObject?>>
            _visualProviders =
                new Dictionary<ProductPresentationContext, Func<GameObject?>>();
        private readonly HashSet<ProductPresentationContext> _requiredContexts =
            new HashSet<ProductPresentationContext>();
        private readonly Dictionary<
            ProductPresentationContext,
            ProductPresentationTransform> _visualTransforms =
                new Dictionary<
                    ProductPresentationContext,
                    ProductPresentationTransform>();

        private Func<Sprite?>? _iconProvider;
        private Func<GameObject?>? _consumptionPrefabProvider;
        private bool _generateIconFromLooseVisual;
        private int _generatedIconSize = 512;
        private bool _fitGeneratedIconToCamera = true;
        private float _generatedIconCameraFill = 0.72f;
        private ProductPresentationTransform? _generatedIconTransform;

        /// <summary>
        /// Sets the shared loose visual provider. Stored, held, station, and functional-product
        /// contexts use this provider when they do not define a context-specific visual.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithLooseVisual(
            Func<GameObject?> provider)
        {
            return SetVisualProvider(ProductPresentationContext.Loose, provider);
        }

        /// <summary>
        /// Sets the shared loose visual provider and an explicit local transform override.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <param name="presentationTransform">The cloned visual root transform.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithLooseVisual(
            Func<GameObject?> provider,
            ProductPresentationTransform presentationTransform)
        {
            return SetVisualProvider(
                ProductPresentationContext.Loose,
                provider,
                presentationTransform);
        }

        /// <summary>
        /// Sets the stored-item visual provider.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithStoredVisual(
            Func<GameObject?> provider)
        {
            return SetVisualProvider(ProductPresentationContext.Stored, provider);
        }

        /// <summary>
        /// Sets the stored-item visual provider and an explicit local transform override.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <param name="presentationTransform">The cloned visual root transform.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithStoredVisual(
            Func<GameObject?> provider,
            ProductPresentationTransform presentationTransform)
        {
            return SetVisualProvider(
                ProductPresentationContext.Stored,
                provider,
                presentationTransform);
        }

        /// <summary>
        /// Sets the first- and third-person held visual provider.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithHeldVisual(
            Func<GameObject?> provider)
        {
            return SetVisualProvider(ProductPresentationContext.Held, provider);
        }

        /// <summary>
        /// Sets the held visual provider and an explicit local transform override.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <param name="presentationTransform">The cloned visual root transform.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithHeldVisual(
            Func<GameObject?> provider,
            ProductPresentationTransform presentationTransform)
        {
            return SetVisualProvider(
                ProductPresentationContext.Held,
                provider,
                presentationTransform);
        }

        /// <summary>
        /// Sets the station-item visual provider.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithStationVisual(
            Func<GameObject?> provider)
        {
            return SetVisualProvider(ProductPresentationContext.Station, provider);
        }

        /// <summary>
        /// Sets the station-item visual provider and an explicit local transform override.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <param name="presentationTransform">The cloned visual root transform.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithStationVisual(
            Func<GameObject?> provider,
            ProductPresentationTransform presentationTransform)
        {
            return SetVisualProvider(
                ProductPresentationContext.Station,
                provider,
                presentationTransform);
        }

        /// <summary>
        /// Sets the functional-product visual provider.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithFunctionalProductVisual(
            Func<GameObject?> provider)
        {
            return SetVisualProvider(
                ProductPresentationContext.FunctionalProduct,
                provider);
        }

        /// <summary>
        /// Sets the functional-product visual provider and an explicit local transform override.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned visual prefab source.</param>
        /// <param name="presentationTransform">The cloned visual root transform.</param>
        /// <returns>This builder.</returns>
        public ProductPresentationProfileBuilder WithFunctionalProductVisual(
            Func<GameObject?> provider,
            ProductPresentationTransform presentationTransform)
        {
            return SetVisualProvider(
                ProductPresentationContext.FunctionalProduct,
                provider,
                presentationTransform);
        }

        /// <summary>
        /// Sets an explicit loose inventory icon provider.
        /// </summary>
        /// <param name="provider">A provider that returns a mod-owned sprite.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.
        /// </exception>
        public ProductPresentationProfileBuilder WithIcon(Func<Sprite?> provider)
        {
            _iconProvider =
                provider ?? throw new ArgumentNullException(nameof(provider));
            _generateIconFromLooseVisual = false;
            return this;
        }

        /// <summary>
        /// Generates the loose inventory icon from the loose visual through the game's
        /// item-icon rendering rig exposed by <see cref="Rendering.IconFactory"/>.
        /// </summary>
        /// <param name="size">The square icon size, from 32 through 2048 pixels.</param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// S1API keeps the representation template's icon while it queues capture. The queue
        /// waits for the native rig, yields through a complete render frame, rejects transparent
        /// cold-start captures, and finishes while the loading screen is still open.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="size"/> is outside 32 through 2048.
        /// </exception>
        public ProductPresentationProfileBuilder WithGeneratedIconFromLooseVisual(
            int size = 512)
        {
            return ConfigureGeneratedIcon(
                size,
                fitToCamera: true,
                cameraFill: 0.72f);
        }

        /// <summary>
        /// Generates the loose inventory icon with explicit native-camera framing controls.
        /// </summary>
        /// <param name="size">The square icon size, from 32 through 2048 pixels.</param>
        /// <param name="fitToCamera">
        /// Whether S1API should fit renderer bounds to the native fixed thumbnail camera.
        /// Set false to preserve the provider's authored scale.
        /// </param>
        /// <param name="cameraFill">
        /// The target share of the native camera's vertical view when fitting, greater than
        /// zero through 2. Values above 1 intentionally crop the model.
        /// </param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="size"/> is outside 32 through 2048 or
        /// <paramref name="cameraFill"/> is not greater than zero through 2.
        /// </exception>
        public ProductPresentationProfileBuilder WithGeneratedIconFromLooseVisual(
            int size,
            bool fitToCamera,
            float cameraFill = 0.72f)
        {
            return ConfigureGeneratedIcon(size, fitToCamera, cameraFill);
        }

        /// <summary>
        /// Sets an icon-only transform for generated loose-visual icons.
        /// </summary>
        /// <param name="presentationTransform">
        /// The cloned visual root transform used only during icon capture.
        /// </param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// This replaces the loose presentation transform during icon capture without changing
        /// stored, held, station, or world presentation. It may be configured before or after
        /// <see cref="WithGeneratedIconFromLooseVisual(int)"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="presentationTransform"/> is <see langword="null"/>.
        /// </exception>
        public ProductPresentationProfileBuilder WithGeneratedIconTransform(
            ProductPresentationTransform presentationTransform)
        {
            _generatedIconTransform =
                presentationTransform ??
                throw new ArgumentNullException(nameof(presentationTransform));
            return this;
        }

        /// <summary>
        /// Sets the consumption prefab provider.
        /// </summary>
        /// <param name="provider">
        /// A provider that returns a prefab source containing the game's
        /// <c>ProductConsumeAnimation</c> component.
        /// </param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.
        /// </exception>
        public ProductPresentationProfileBuilder WithConsumptionPrefab(
            Func<GameObject?> provider)
        {
            _consumptionPrefabProvider =
                provider ?? throw new ArgumentNullException(nameof(provider));
            return this;
        }

        /// <summary>
        /// Marks contexts as required. A missing provider, missing native scaffold, null result,
        /// or provider failure then produces a clear registration error instead of falling back.
        /// </summary>
        /// <param name="contexts">Defined presentation contexts to require.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="contexts"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a context is not a defined <see cref="ProductPresentationContext"/> value.
        /// </exception>
        public ProductPresentationProfileBuilder Require(
            params ProductPresentationContext[] contexts)
        {
            if (contexts == null)
                throw new ArgumentNullException(nameof(contexts));

            for (int i = 0; i < contexts.Length; i++)
            {
                ProductPresentationContext context = contexts[i];
                if (!Enum.IsDefined(typeof(ProductPresentationContext), context))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(contexts),
                        context,
                        "Presentation contexts must be defined values.");
                }

                _requiredContexts.Add(context);
            }

            return this;
        }

        /// <summary>
        /// Creates an immutable presentation profile snapshot.
        /// </summary>
        /// <returns>The presentation profile.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a required context has no configured provider or loose-visual fallback.
        /// </exception>
        public ProductPresentationProfile Build()
        {
            ValidateRequiredProviders();

            return new ProductPresentationProfile(
                new Dictionary<ProductPresentationContext, Func<GameObject?>>(
                    _visualProviders),
                new Dictionary<
                    ProductPresentationContext,
                    ProductPresentationTransform>(_visualTransforms),
                _iconProvider,
                _consumptionPrefabProvider,
                _generateIconFromLooseVisual,
                _generatedIconSize,
                _fitGeneratedIconToCamera,
                _generatedIconCameraFill,
                _generatedIconTransform,
                new List<ProductPresentationContext>(_requiredContexts).AsReadOnly());
        }

        private ProductPresentationProfileBuilder SetVisualProvider(
            ProductPresentationContext context,
            Func<GameObject?> provider)
        {
            _visualProviders[context] =
                provider ?? throw new ArgumentNullException(nameof(provider));
            _visualTransforms.Remove(context);
            return this;
        }

        private ProductPresentationProfileBuilder SetVisualProvider(
            ProductPresentationContext context,
            Func<GameObject?> provider,
            ProductPresentationTransform presentationTransform)
        {
            _visualProviders[context] =
                provider ?? throw new ArgumentNullException(nameof(provider));
            _visualTransforms[context] =
                presentationTransform ??
                throw new ArgumentNullException(nameof(presentationTransform));
            return this;
        }

        private ProductPresentationProfileBuilder ConfigureGeneratedIcon(
            int size,
            bool fitToCamera,
            float cameraFill)
        {
            if (size < 32 || size > 2048)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(size),
                    size,
                    "Generated icon size must be between 32 and 2048 pixels.");
            }

            if (cameraFill <= 0f || cameraFill > 2f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cameraFill),
                    cameraFill,
                    "Generated icon camera fill must be greater than zero and at most 2.");
            }

            _iconProvider = null;
            _generateIconFromLooseVisual = true;
            _generatedIconSize = size;
            _fitGeneratedIconToCamera = fitToCamera;
            _generatedIconCameraFill = cameraFill;
            return this;
        }

        private void ValidateRequiredProviders()
        {
            foreach (ProductPresentationContext context in _requiredContexts)
            {
                bool configured;
                switch (context)
                {
                    case ProductPresentationContext.Icon:
                        configured =
                            _iconProvider != null ||
                            (_generateIconFromLooseVisual &&
                             _visualProviders.ContainsKey(
                                 ProductPresentationContext.Loose));
                        break;
                    case ProductPresentationContext.Consumption:
                        configured = _consumptionPrefabProvider != null;
                        break;
                    default:
                        configured =
                            _visualProviders.ContainsKey(context) ||
                            (context != ProductPresentationContext.Loose &&
                             _visualProviders.ContainsKey(
                                 ProductPresentationContext.Loose));
                        break;
                }

                if (!configured)
                {
                    throw new InvalidOperationException(
                        $"Required presentation context '{context}' does not have a provider. " +
                        "Configure that context or a supported loose-visual fallback before Build().");
                }
            }
        }
    }
}
