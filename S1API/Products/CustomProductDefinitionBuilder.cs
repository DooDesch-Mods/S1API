#if (IL2CPPMELON)
using NativeEffect = Il2CppScheduleOne.Effects.Effect;
using NativePackagingDefinition = Il2CppScheduleOne.Product.Packaging.PackagingDefinition;
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using NativeEffect = ScheduleOne.Effects.Effect;
using NativePackagingDefinition = ScheduleOne.Product.Packaging.PackagingDefinition;
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Products;
using S1API.Internal.Properties;
using S1API.Items;
using S1API.Properties.Interfaces;

namespace S1API.Products
{
    /// <summary>
    /// Builds and lifecycle-registers a generic, non-mixable custom product definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The builder creates a plain native product definition. It does not create a weed,
    /// methamphetamine, cocaine, or shroom-family definition and does not register the product
    /// with native mix generation.
    /// </para>
    /// <para>
    /// Presentation references are borrowed from an existing product through
    /// <see cref="WithRepresentationsFrom(ProductDefinition)"/>. A registered
    /// <see cref="ProductPresentationProfile"/> may replace the generic product's loose presentation
    /// contexts while preserving the borrowed native scaffolds. Packaged-content visuals,
    /// product-manager UI tabs, station recipes, and mixing remain outside this builder's scope.
    /// Register the same definition and presentation profile before save-item restoration on every
    /// participating peer; the game cannot restore or transmit definitions or assets supplied by a
    /// missing mod.
    /// </para>
    /// </remarks>
    public sealed class CustomProductDefinitionBuilder
    {
        private readonly string _id;
        private readonly string _ownerId;
        private readonly ProductKind _productKind;
        private readonly List<PropertyBase> _properties =
            new List<PropertyBase>();
        private readonly List<PackagingDefinition> _validPackaging =
            new List<PackagingDefinition>();

        private string? _name;
        private string _description = string.Empty;
        private float _productPrice;
        private bool _hasProductPrice;
        private LegalStatus _legalStatus = LegalStatus.Illegal;
        private float _baseAddictiveness;
        private Quality _defaultQuality = Quality.Standard;
        private ProductDefinition? _representationTemplate;
        private int? _playerEffectDurationSeconds;
        private int? _npcEffectDurationSeconds;
        private string? _saveProviderId;
        private int _saveProviderVersion;
        private string _saveProviderData = string.Empty;

        /// <summary>Gets the stable product ID this builder will create.</summary>
        public string ProductId => _id;
        private CustomProductDefinition? _builtDefinition;

        /// <summary>
        /// Creates a builder for a stable generic custom product.
        /// </summary>
        /// <param name="id">A stable namespaced product ID in <c>mod-id:product-id</c> form.</param>
        /// <param name="productKind">
        /// The immutable logical product kind. Its compatibility drug type is required by native
        /// save and product-item data but does not enable mixing.
        /// </param>
        public CustomProductDefinitionBuilder(string id, ProductKind productKind)
        {
            _id = CustomProductDefinitionBuilderContract.NormalizeId(id);
            _ownerId = CustomProductDefinitionBuilderContract.GetOwnerId(_id);
            _productKind =
                productKind ?? throw new ArgumentNullException(nameof(productKind));
        }

        /// <summary>
        /// Sets the non-empty display name.
        /// </summary>
        /// <param name="name">The product display name.</param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithName(string name)
        {
            EnsureMutable();
            _name = CustomProductDefinitionBuilderContract.NormalizeName(name);
            return this;
        }

        /// <summary>
        /// Sets the product description. The default is an empty description.
        /// </summary>
        /// <param name="description">The description, which may be empty but not null.</param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithDescription(string description)
        {
            EnsureMutable();
            _description =
                CustomProductDefinitionBuilderContract.NormalizeDescription(
                    description);
            return this;
        }

        /// <summary>
        /// Sets the product's initial native price.
        /// </summary>
        /// <param name="productPrice">
        /// A finite price. Values are clamped to 1 through 999 and rounded to the nearest integer,
        /// matching the native product-manager price policy.
        /// </param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithProductPrice(float productPrice)
        {
            EnsureMutable();
            _productPrice =
                CustomProductDefinitionBuilderContract.NormalizeProductPrice(
                    productPrice);
            _hasProductPrice = true;
            return this;
        }

        /// <summary>
        /// Adds one vanilla or registered custom product property.
        /// </summary>
        /// <param name="property">The property token or wrapper to add.</param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithProperty(PropertyBase property)
        {
            EnsureMutable();
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _properties.Add(property);
            return this;
        }

        /// <summary>
        /// Replaces all configured product properties.
        /// </summary>
        /// <param name="properties">
        /// Distinct vanilla property tokens or registered custom property wrappers. An empty array
        /// creates a product without effects.
        /// </param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithProperties(
            params PropertyBase[] properties)
        {
            EnsureMutable();
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i] == null)
                {
                    throw new ArgumentException(
                        "Custom product properties cannot contain null values.",
                        nameof(properties));
                }
            }

            _properties.Clear();
            _properties.AddRange(properties);
            return this;
        }

        /// <summary>
        /// Sets whether the product is legal or illegal. The default is
        /// <see cref="LegalStatus.Illegal"/>.
        /// </summary>
        /// <param name="legalStatus">A defined legal-status value.</param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithLegalStatus(
            LegalStatus legalStatus)
        {
            EnsureMutable();
            if (!Enum.IsDefined(typeof(LegalStatus), legalStatus))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(legalStatus),
                    legalStatus,
                    "Legal status must be a defined S1API.Items.LegalStatus value.");
            }

            _legalStatus = legalStatus;
            return this;
        }

        /// <summary>
        /// Sets base addictiveness before native property contributions.
        /// </summary>
        /// <param name="baseAddictiveness">
        /// A finite value, clamped to the native 0 through 1 range.
        /// </param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithBaseAddictiveness(
            float baseAddictiveness)
        {
            EnsureMutable();
            _baseAddictiveness =
                CustomProductDefinitionBuilderContract.NormalizeAddictiveness(
                    baseAddictiveness);
            return this;
        }

        /// <summary>
        /// Sets the quality used by <see cref="CustomProductDefinition.CreateInstance(int)"/>.
        /// The default is <see cref="Quality.Standard"/>.
        /// </summary>
        /// <param name="quality">A defined quality value.</param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithDefaultQuality(Quality quality)
        {
            EnsureMutable();
            CustomProductDefinitionBuilderContract.ValidateQuality(
                quality,
                nameof(quality));
            _defaultQuality = quality;
            return this;
        }

        /// <summary>
        /// Replaces the packaging types accepted by this product.
        /// </summary>
        /// <param name="packaging">
        /// Distinct packaging definitions. The builder stores them in native ascending-capacity order.
        /// An empty array makes the product loose-only.
        /// </param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithValidPackaging(
            params PackagingDefinition[] packaging)
        {
            EnsureMutable();
            if (packaging == null)
                throw new ArgumentNullException(nameof(packaging));

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < packaging.Length; i++)
            {
                PackagingDefinition item = packaging[i];
                if (item == null)
                {
                    throw new ArgumentException(
                        "Valid packaging cannot contain null values.",
                        nameof(packaging));
                }

                if (!ids.Add(item.ID))
                {
                    throw new ArgumentException(
                        $"Packaging ID '{item.ID}' was supplied more than once. " +
                        "Packaging IDs are case-insensitive.",
                        nameof(packaging));
                }
            }

            _validPackaging.Clear();
            _validPackaging.AddRange(packaging);
            return this;
        }

        /// <summary>
        /// Borrows the existing loose/stored/held/icon/consumption references needed for item use.
        /// </summary>
        /// <param name="template">An existing native-backed product definition.</param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// The builder shares the template references; it does not clone or redistribute game
        /// assets. It deliberately does not copy the template's station representation because
        /// generic products are not production-station or mixing inputs.
        /// </remarks>
        public CustomProductDefinitionBuilder WithRepresentationsFrom(
            ProductDefinition template)
        {
            EnsureMutable();
            _representationTemplate =
                template ?? throw new ArgumentNullException(nameof(template));
            return this;
        }

        /// <summary>
        /// Sets native consumption-effect durations.
        /// </summary>
        /// <param name="playerSeconds">Non-negative player effect duration in seconds.</param>
        /// <param name="npcSeconds">Non-negative NPC effect duration in seconds.</param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// When omitted, both durations are copied from the representation template.
        /// </remarks>
        public CustomProductDefinitionBuilder WithEffectDurations(
            int playerSeconds,
            int npcSeconds)
        {
            EnsureMutable();
            CustomProductDefinitionBuilderContract.ValidateEffectDuration(
                playerSeconds,
                nameof(playerSeconds));
            CustomProductDefinitionBuilderContract.ValidateEffectDuration(
                npcSeconds,
                nameof(npcSeconds));
            _playerEffectDurationSeconds = playerSeconds;
            _npcEffectDurationSeconds = npcSeconds;
            return this;
        }

        /// <summary>
        /// Associates this definition with a process-registered provider that can recreate it on a
        /// fresh-process save load.
        /// </summary>
        /// <param name="providerId">The stable, namespaced provider ID.</param>
        /// <param name="providerVersion">The provider's non-negative scalar payload version.</param>
        /// <param name="providerData">Bounded provider-owned scalar data; never pass assets or paths.</param>
        /// <returns>This builder.</returns>
        public CustomProductDefinitionBuilder WithSaveProvider(
            string providerId,
            int providerVersion,
            string providerData = "")
        {
            EnsureMutable();
            if (providerVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(providerVersion));
            if (providerData == null)
                throw new ArgumentNullException(nameof(providerData));
            if (providerData.Length > CustomProductSavePersistence.MaximumStringLength)
                throw new ArgumentOutOfRangeException(nameof(providerData));
            _saveProviderId = ProductKindId.Normalize(providerId, nameof(providerId));
            _saveProviderVersion = providerVersion;
            _saveProviderData = providerData;
            return this;
        }

        /// <summary>
        /// Builds and registers the generic product through S1API's process-lifetime custom-product
        /// lifecycle registry.
        /// </summary>
        /// <returns>The new custom product definition, or the same instance on repeated calls.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when required configuration is missing, effects do not resolve, a product-kind
        /// compatibility type is absent, or the stable product ID collides.
        /// </exception>
        /// <remarks>
        /// Build does not discover, list, add to shops, or add this definition to the native
        /// created-products list. Call the explicit discovery, listing, and shop APIs when wanted.
        /// </remarks>
        public CustomProductDefinition Build()
        {
            if (_builtDefinition != null)
                return _builtDefinition;

            if (_name == null)
            {
                throw new InvalidOperationException(
                    "WithName must be called before Build().");
            }

            if (!_hasProductPrice)
            {
                throw new InvalidOperationException(
                    "WithProductPrice must be called before Build().");
            }

            if (_representationTemplate == null)
            {
                throw new InvalidOperationException(
                    "WithRepresentationsFrom must be called before Build().");
            }

            if (!_productKind.CompatibilityDrugType.HasValue)
            {
                throw new InvalidOperationException(
                    $"Product kind '{_productKind.Id}' does not define a compatibility drug type. " +
                    "Generic products require ProductKindBuilder.WithCompatibilityDrugType(...) " +
                    "for native save and product-item data.");
            }

            List<NativeEffect> resolvedProperties =
                PropertyResolver.ResolveToGameProperties(_properties);
            CustomProductDefinitionBuilderContract.ValidatePropertyCounts(
                _properties.Count,
                resolvedProperties.Count);

            _validPackaging.Sort(
                (left, right) =>
                {
                    int quantityComparison =
                        left.Quantity.CompareTo(right.Quantity);
                    return quantityComparison != 0
                        ? quantityComparison
                        : StringComparer.OrdinalIgnoreCase.Compare(
                            left.ID,
                            right.ID);
                });

            var nativePackaging =
                new List<NativePackagingDefinition>(_validPackaging.Count);
            for (int i = 0; i < _validPackaging.Count; i++)
            {
                nativePackaging.Add(
                    _validPackaging[i].S1PackagingDefinition);
            }

            var nativeDefinition = CustomProductDefinitionFactory.Create(
                _id,
                _name,
                _description,
                _productPrice,
                _legalStatus,
                _baseAddictiveness,
                _playerEffectDurationSeconds ??
                    _representationTemplate.S1ProductDefinition.PlayerEffectDuration,
                _npcEffectDurationSeconds ??
                    _representationTemplate.S1ProductDefinition.NPCEffectDuration,
                _productKind.CompatibilityDrugType.Value,
                resolvedProperties,
                nativePackaging,
                _representationTemplate.S1ProductDefinition);

            var packagingSnapshot =
                new List<PackagingDefinition>(nativePackaging.Count);
            for (int i = 0; i < nativePackaging.Count; i++)
            {
                packagingSnapshot.Add(
                    new PackagingDefinition(nativePackaging[i]));
            }

            var metadata = new CustomProductDefinitionMetadata(
                _productKind,
                _defaultQuality,
                packagingSnapshot,
                _representationTemplate.S1ProductDefinition);
            var saveDescriptor = new CustomProductSaveDescriptorData
            {
                ProductId = _id,
                OwnerId = _ownerId,
                ProductName = _name,
                Description = _description,
                InitialPrice = _productPrice,
                LegalStatus = (int)_legalStatus,
                BaseAddictiveness = _baseAddictiveness,
                DefaultQuality = (int)_defaultQuality,
                ProductKindId = _productKind.Id,
                CompatibilityDrugType = (int)_productKind.CompatibilityDrugType.Value,
                RepresentationTemplateId = _representationTemplate.ID,
                PlayerEffectDurationSeconds = _playerEffectDurationSeconds ?? _representationTemplate.S1ProductDefinition.PlayerEffectDuration,
                NpcEffectDurationSeconds = _npcEffectDurationSeconds ?? _representationTemplate.S1ProductDefinition.NPCEffectDuration,
                PropertyIds = resolvedProperties.ConvertAll(property => property.ID).ToArray(),
                PackagingIds = _validPackaging.ConvertAll(packaging => packaging.ID).ToArray(),
                ProviderId = _saveProviderId,
                ProviderVersion = _saveProviderVersion,
                ProviderData = _saveProviderData
            };
            RegisterCreatedDefinition(
                _ownerId,
                _id,
                _name,
                _productPrice,
                nativeDefinition,
                metadata,
                saveDescriptor,
                CustomProductDefinitionFactory.Destroy);

            _builtDefinition =
                new CustomProductDefinition(nativeDefinition, metadata);
            return _builtDefinition;
        }

        internal static S1Product.ProductDefinition RegisterCreatedDefinition(
            string ownerId,
            string productId,
            string productName,
            float initialPrice,
            S1Product.ProductDefinition nativeDefinition,
            CustomProductDefinitionMetadata metadata,
            Action<S1Product.ProductDefinition> destroy)
        {
            return RegisterCreatedDefinition(
                ownerId,
                productId,
                productName,
                initialPrice,
                nativeDefinition,
                metadata,
                null,
                destroy);
        }

        internal static S1Product.ProductDefinition RegisterCreatedDefinition(
            string ownerId,
            string productId,
            string productName,
            float initialPrice,
            S1Product.ProductDefinition nativeDefinition,
            CustomProductDefinitionMetadata metadata,
            CustomProductSaveDescriptorData? saveDescriptor,
            Action<S1Product.ProductDefinition> destroy)
        {
            if (destroy == null)
                throw new ArgumentNullException(nameof(destroy));

            try
            {
                S1Product.ProductDefinition registeredDefinition =
                    CustomProductDefinitionRegistry.Register(
                        ownerId,
                        productId,
                        productName,
                        initialPrice,
                        nativeDefinition,
                        metadata,
                        saveDescriptor);
                if (ReferenceEquals(registeredDefinition, nativeDefinition))
                    return registeredDefinition;

                throw new InvalidOperationException(
                    $"Product ID '{productId}' is already registered by this owner with another " +
                    "definition. Product IDs are case-insensitive; reuse the original builder " +
                    "or choose a unique stable namespaced ID.");
            }
            catch
            {
                try
                {
                    destroy(nativeDefinition);
                }
                catch (Exception cleanupException)
                {
                    MelonLoader.MelonLogger.Error(
                        $"Failed to destroy rejected custom product '{productId}': " +
                        cleanupException);
                }

                throw;
            }
        }

        private void EnsureMutable()
        {
            if (_builtDefinition != null)
            {
                throw new InvalidOperationException(
                    "A custom product builder cannot be changed after a successful Build(). " +
                    "Create a new builder for another product.");
            }
        }
    }
}
