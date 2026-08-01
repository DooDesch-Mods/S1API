using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Describes the mod-owned presentation sources for a custom product.
    /// </summary>
    /// <remarks>
    /// Providers return reusable prefab sources. S1API clones returned objects before attaching
    /// them to native presentation scaffolds. The profile does not transmit assets to other peers;
    /// every participating peer must register the same profile locally.
    /// </remarks>
    public sealed class ProductPresentationProfile
    {
        internal ProductPresentationProfile(
            IReadOnlyDictionary<ProductPresentationContext, Func<GameObject?>>
                visualProviders,
            IReadOnlyDictionary<ProductPresentationContext, ProductPresentationTransform>
                visualTransforms,
            Func<Sprite?>? iconProvider,
            Func<GameObject?>? consumptionPrefabProvider,
            bool generateIconFromLooseVisual,
            int generatedIconSize,
            bool fitGeneratedIconToCamera,
            float generatedIconCameraFill,
            ProductPresentationTransform? generatedIconTransform,
            Func<GameObject?>? avatarHeldVisualProvider,
            ProductPresentationTransform? avatarHeldTransform,
            bool useFunctionalProductConvexMeshColliders,
            IReadOnlyCollection<ProductPresentationContext> requiredContexts)
        {
            VisualProviders = visualProviders;
            VisualTransforms = visualTransforms;
            IconProvider = iconProvider;
            ConsumptionPrefabProvider = consumptionPrefabProvider;
            GenerateIconFromLooseVisual = generateIconFromLooseVisual;
            GeneratedIconSize = generatedIconSize;
            FitGeneratedIconToCamera = fitGeneratedIconToCamera;
            GeneratedIconCameraFill = generatedIconCameraFill;
            GeneratedIconTransform = generatedIconTransform;
            AvatarHeldVisualProvider = avatarHeldVisualProvider;
            AvatarHeldTransform = avatarHeldTransform;
            UseFunctionalProductConvexMeshColliders =
                useFunctionalProductConvexMeshColliders;
            RequiredContexts = requiredContexts;
        }

        internal IReadOnlyDictionary<ProductPresentationContext, Func<GameObject?>>
            VisualProviders { get; }

        internal IReadOnlyDictionary<
            ProductPresentationContext,
            ProductPresentationTransform> VisualTransforms { get; }

        internal Func<Sprite?>? IconProvider { get; }

        internal Func<GameObject?>? ConsumptionPrefabProvider { get; }

        internal bool GenerateIconFromLooseVisual { get; }

        internal int GeneratedIconSize { get; }

        internal bool FitGeneratedIconToCamera { get; }

        internal float GeneratedIconCameraFill { get; }

        internal ProductPresentationTransform? GeneratedIconTransform { get; }

        internal Func<GameObject?>? AvatarHeldVisualProvider { get; }

        internal ProductPresentationTransform? AvatarHeldTransform { get; }

        internal bool UseFunctionalProductConvexMeshColliders { get; }

        internal IReadOnlyCollection<ProductPresentationContext> RequiredContexts { get; }

        internal bool IsRequired(ProductPresentationContext context)
        {
            foreach (ProductPresentationContext required in RequiredContexts)
            {
                if (required == context)
                    return true;
            }

            return false;
        }

        internal bool TryGetVisualProvider(
            ProductPresentationContext context,
            out Func<GameObject?>? provider)
        {
            if (VisualProviders.TryGetValue(context, out provider))
                return true;

            if (context != ProductPresentationContext.Loose &&
                VisualProviders.TryGetValue(
                    ProductPresentationContext.Loose,
                    out provider))
            {
                return true;
            }

            provider = null;
            return false;
        }

        internal bool TryGetVisualTransform(
            ProductPresentationContext context,
            out ProductPresentationTransform? presentationTransform)
        {
            if (VisualTransforms.TryGetValue(context, out presentationTransform))
                return true;

            if (context != ProductPresentationContext.Loose &&
                !VisualProviders.ContainsKey(context) &&
                VisualTransforms.TryGetValue(
                    ProductPresentationContext.Loose,
                    out presentationTransform))
            {
                return true;
            }

            presentationTransform = null;
            return false;
        }

        internal bool TryGetAvatarHeldVisualProvider(
            out Func<GameObject?>? provider)
        {
            if (AvatarHeldVisualProvider != null)
            {
                provider = AvatarHeldVisualProvider;
                return true;
            }

            return TryGetVisualProvider(ProductPresentationContext.Held, out provider);
        }

        internal bool TryGetAvatarHeldTransform(
            out ProductPresentationTransform? presentationTransform)
        {
            if (AvatarHeldTransform != null)
            {
                presentationTransform = AvatarHeldTransform;
                return true;
            }

            return TryGetVisualTransform(
                ProductPresentationContext.Held,
                out presentationTransform);
        }
    }
}
