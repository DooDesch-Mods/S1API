using System;
using S1API.Products;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Internal.Products
{
    internal static class ProductPresentationWorkbenchAdapter
    {
        internal static bool TryCreateDefinition(
            string id,
            out PresentationWorkbenchDefinition? definition)
        {
            definition = null;
            if (!ProductPresentationProfileRegistry.TryGetWorkbenchProfile(
                    id,
                    out ProductPresentationProfile? profile) ||
                profile == null)
            {
                return false;
            }

            var builder =
                new PresentationWorkbenchDefinitionBuilder(
                    id,
                    $"Product presentation: {id}");
            bool configured = false;

            if (profile.TryGetVisualProvider(
                    ProductPresentationContext.Held,
                    out Func<GameObject?>? heldProvider) &&
                heldProvider != null)
            {
                if (profile.TryGetVisualTransform(
                        ProductPresentationContext.Held,
                        out ProductPresentationTransform? heldTransform) &&
                    heldTransform != null)
                {
                    builder.WithFirstPersonPreview(
                        heldProvider,
                        Convert(heldTransform));
                }
                else
                {
                    builder.WithFirstPersonPreview(heldProvider);
                }

                configured = true;
            }

            if (profile.TryGetAvatarHeldVisualProvider(
                    out Func<GameObject?>? avatarProvider) &&
                avatarProvider != null)
            {
                if (profile.TryGetAvatarHeldTransform(
                        out ProductPresentationTransform? avatarTransform) &&
                    avatarTransform != null)
                {
                    builder.WithAvatarPreview(
                        avatarProvider,
                        Convert(avatarTransform));
                }
                else
                {
                    builder.WithAvatarPreview(avatarProvider);
                }

                configured = true;
            }

            if (profile.GenerateIconFromLooseVisual &&
                profile.TryGetVisualProvider(
                    ProductPresentationContext.Loose,
                    out Func<GameObject?>? iconProvider) &&
                iconProvider != null)
            {
                ProductPresentationTransform? iconTransform =
                    profile.GeneratedIconTransform;
                if (iconTransform == null)
                {
                    profile.TryGetVisualTransform(
                        ProductPresentationContext.Loose,
                        out iconTransform);
                }

                builder.WithIconPreview(
                    iconProvider,
                    iconTransform?.LocalEulerAngles ?? Vector3.zero,
                    profile.FitGeneratedIconToCamera,
                    profile.GeneratedIconCameraFill,
                    profile.GeneratedIconSize,
                    iconTransform?.LocalScale ?? Vector3.one);
                configured = true;
            }

            if (!configured)
                return false;

            definition = builder.Build();
            return true;
        }

        private static PresentationWorkbenchTransform Convert(
            ProductPresentationTransform transform) =>
            new PresentationWorkbenchTransform(
                transform.LocalPosition,
                transform.LocalEulerAngles,
                transform.LocalScale);
    }
}
