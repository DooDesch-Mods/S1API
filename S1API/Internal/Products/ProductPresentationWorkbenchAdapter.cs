using System;
using S1API.Internal.Rendering;
using S1API.Products;
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

            PresentationWorkbenchDefinition.PreviewContext? firstPerson = null;
            PresentationWorkbenchDefinition.AvatarPreviewContext? avatar = null;
            PresentationWorkbenchDefinition.IconPreviewContext? icon = null;

            if (profile.TryGetVisualProvider(
                    ProductPresentationContext.Held,
                    out Func<GameObject?>? heldProvider) &&
                heldProvider != null)
            {
                PresentationWorkbenchTransform? heldTransform =
                    ResolveTransform(
                        profile,
                        ProductPresentationContext.Held);
                firstPerson =
                    new PresentationWorkbenchDefinition.PreviewContext(
                        heldProvider,
                        heldTransform,
                        PresentationWorkbenchExportKind.ProductTransform);
            }

            if (profile.TryGetAvatarHeldVisualProvider(
                    out Func<GameObject?>? avatarProvider) &&
                avatarProvider != null)
            {
                PresentationWorkbenchTransform? avatarTransform =
                    profile.TryGetAvatarHeldTransform(
                        out ProductPresentationTransform? configuredTransform) &&
                    configuredTransform != null
                        ? Convert(configuredTransform)
                        : null;
                avatar =
                    new PresentationWorkbenchDefinition.AvatarPreviewContext(
                        avatarProvider,
                        avatarTransform,
                        global::S1API.Items.AvatarHand.Right,
                        "RightArm_Hold_ClosedHand",
                        PresentationWorkbenchExportKind.ProductTransform);
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

                icon =
                    new PresentationWorkbenchDefinition.IconPreviewContext(
                        iconProvider,
                        new PresentationWorkbenchTransform(
                            Vector3.zero,
                            iconTransform?.LocalEulerAngles ?? Vector3.zero,
                            iconTransform?.LocalScale ?? Vector3.one),
                        profile.GeneratedIconSize,
                        profile.FitGeneratedIconToCamera,
                        profile.GeneratedIconCameraFill);
            }

            if (firstPerson == null && avatar == null && icon == null)
                return false;

            definition =
                new PresentationWorkbenchDefinition(
                    id,
                    $"Product presentation: {id}",
                    firstPerson,
                    avatar,
                    icon);
            return true;
        }

        private static PresentationWorkbenchTransform? ResolveTransform(
            ProductPresentationProfile profile,
            ProductPresentationContext context)
        {
            if (profile.TryGetVisualTransform(
                    context,
                    out ProductPresentationTransform? transform) &&
                transform != null)
            {
                return Convert(transform);
            }

            return null;
        }

        private static PresentationWorkbenchTransform Convert(
            ProductPresentationTransform transform) =>
            new PresentationWorkbenchTransform(
                transform.LocalPosition,
                transform.LocalEulerAngles,
                transform.LocalScale);
    }
}
