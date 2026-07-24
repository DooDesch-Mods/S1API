#if (IL2CPPMELON)
using ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Packaging = Il2CppScheduleOne.Product.Packaging;
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using ItemFramework = ScheduleOne.ItemFramework;
using S1Packaging = ScheduleOne.Product.Packaging;
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Products;
using S1API.Internal.Utils;
using S1API.Items;

namespace S1API.Products
{
    /// <summary>
    /// Represents a generic, non-mixable custom product registered through S1API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The stable <see cref="Items.ItemDefinition.ID"/> is used by native loose and packaged
    /// product-item save data and network serialization. The definition must be registered before
    /// restoration on every peer; missing-mod definition transfer is not supported by the game.
    /// </para>
    /// <para>
    /// This type does not add native mixing, generated variants, custom presentation providers,
    /// packaged-content visuals, station recipes, or product-manager UI categories.
    /// </para>
    /// </remarks>
    public sealed class CustomProductDefinition : ProductDefinition
    {
        private readonly CustomProductDefinitionMetadata _metadata;

        internal CustomProductDefinition(
            S1Product.ProductDefinition productDefinition,
            CustomProductDefinitionMetadata metadata)
            : base(productDefinition)
        {
            _metadata = metadata;
        }

        /// <summary>
        /// Gets the stable logical product kind.
        /// </summary>
        public ProductKind ProductKind => _metadata.ProductKind;

        /// <summary>
        /// Gets the quality used when an explicit quality is not supplied.
        /// </summary>
        public Quality DefaultQuality => _metadata.DefaultQuality;

        /// <summary>
        /// Gets the configured base addictiveness before property contributions.
        /// </summary>
        public float BaseAddictiveness =>
            S1ProductDefinition.BaseAddictiveness;

        /// <summary>
        /// Gets the configured player consumption-effect duration in seconds.
        /// </summary>
        public int PlayerEffectDurationSeconds =>
            S1ProductDefinition.PlayerEffectDuration;

        /// <summary>
        /// Gets the configured NPC consumption-effect duration in seconds.
        /// </summary>
        public int NpcEffectDurationSeconds =>
            S1ProductDefinition.NPCEffectDuration;

        /// <summary>
        /// Gets an immutable snapshot of packaging accepted by this product.
        /// </summary>
        public IReadOnlyList<PackagingDefinition> ValidPackaging =>
            _metadata.ValidPackaging;

        /// <summary>
        /// Creates a loose product instance using <see cref="DefaultQuality"/>.
        /// </summary>
        /// <param name="quantity">The native stack quantity.</param>
        /// <returns>A native-backed product item instance.</returns>
        public override ItemInstance CreateInstance(int quantity = 1)
        {
            return CreateInstance(quantity, DefaultQuality);
        }

        /// <summary>
        /// Creates a loose product instance with an explicit quality.
        /// </summary>
        /// <param name="quantity">The native stack quantity.</param>
        /// <param name="quality">A defined quality value.</param>
        /// <returns>A native-backed product item instance.</returns>
        public ProductInstance CreateInstance(int quantity, Quality quality)
        {
            CustomProductDefinitionBuilderContract.ValidateQuality(
                quality,
                nameof(quality));
            return new ProductInstance(
                new S1Product.ProductItemInstance(
                    S1ProductDefinition,
                    quantity,
                    quality.ToInternal()));
        }

        /// <summary>
        /// Creates a packaged product instance using <see cref="DefaultQuality"/>.
        /// </summary>
        /// <param name="quantity">The native stack quantity.</param>
        /// <param name="packaging">Packaging configured in <see cref="ValidPackaging"/>.</param>
        /// <returns>
        /// A packaged instance, or <see langword="null"/> when the packaging is null, unsupported,
        /// or cannot be converted to its native definition.
        /// </returns>
        public new ProductInstance? CreatePackagedInstance(
            int quantity,
            PackagingDefinition packaging)
        {
            return CreatePackagedInstance(
                quantity,
                packaging,
                DefaultQuality);
        }

        /// <summary>
        /// Creates a packaged product instance with an explicit quality.
        /// </summary>
        /// <param name="quantity">The native stack quantity.</param>
        /// <param name="packaging">Packaging configured in <see cref="ValidPackaging"/>.</param>
        /// <param name="quality">A defined quality value.</param>
        /// <returns>
        /// A packaged instance, or <see langword="null"/> when the packaging is null, unsupported,
        /// or cannot be converted to its native definition.
        /// </returns>
        public ProductInstance? CreatePackagedInstance(
            int quantity,
            PackagingDefinition packaging,
            Quality quality)
        {
            CustomProductDefinitionBuilderContract.ValidateQuality(
                quality,
                nameof(quality));
            if (!SupportsPackaging(packaging))
                return null;

            try
            {
                S1Packaging.PackagingDefinition nativePackaging =
                    CrossType.As<S1Packaging.PackagingDefinition>(
                        packaging.S1ItemDefinition);
                if (nativePackaging == null)
                    return null;

                return new ProductInstance(
                    new S1Product.ProductItemInstance(
                        S1ProductDefinition,
                        quantity,
                        quality.ToInternal(),
                        nativePackaging));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns whether the supplied packaging is accepted by this definition.
        /// </summary>
        /// <param name="packaging">The packaging to test.</param>
        /// <returns>
        /// <see langword="true"/> for a configured packaging definition; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool SupportsPackaging(PackagingDefinition packaging)
        {
            if (packaging == null ||
                S1ProductDefinition.ValidPackaging == null)
            {
                return false;
            }

            string packagingId = packaging.ID;
            for (int i = 0;
                 i < S1ProductDefinition.ValidPackaging.Length;
                 i++)
            {
                S1Packaging.PackagingDefinition candidate =
                    S1ProductDefinition.ValidPackaging[i];
                if (candidate != null &&
                    string.Equals(
                        candidate.ID,
                        packagingId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Explicitly discovers this product through the native host/server product manager.
        /// </summary>
        /// <param name="listForSale">
        /// Whether native discovery should also list the product. The default keeps listing opt-in.
        /// </param>
        /// <remarks>
        /// Call this from the host/server after the product manager is available. Build itself never
        /// discovers or lists the product.
        /// </remarks>
        public void Discover(bool listForSale = false)
        {
            S1Product.ProductManager productManager =
                S1Product.ProductManager.Instance;
            if (productManager == null)
            {
                throw new InvalidOperationException(
                    "Cannot discover a custom product before ProductManager is available.");
            }

            productManager.SetProductDiscovered(
                null,
                ID,
                listForSale);
        }

        /// <summary>
        /// Explicitly changes native product-manager listing state.
        /// </summary>
        /// <param name="listed">
        /// <see langword="true"/> to list the already discovered product; otherwise to unlist it.
        /// </param>
        /// <remarks>
        /// Call this from a network-active host/client after discovery. Shop inventory remains a
        /// separate explicit integration through <see cref="Shops.ShopManager"/>.
        /// </remarks>
        public void SetListed(bool listed = true)
        {
            S1Product.ProductManager productManager =
                S1Product.ProductManager.Instance;
            if (productManager == null)
            {
                throw new InvalidOperationException(
                    "Cannot change custom product listing before ProductManager is available.");
            }

            productManager.SetProductListed(ID, listed);
        }
    }
}
