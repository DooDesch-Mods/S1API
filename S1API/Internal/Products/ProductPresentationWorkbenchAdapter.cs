#if (IL2CPPMELON)
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
#elif MONOMELON
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
#endif

using System;
using S1API.Internal.Rendering;
using S1API.Items;
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
                ResolveAvatarMetadata(
                    id,
                    out AvatarHand hand,
                    out string animationTrigger,
                    out bool animationUsesBool);
                avatar =
                    new PresentationWorkbenchDefinition.AvatarPreviewContext(
                        avatarProvider,
                        avatarTransform,
                        hand,
                        animationTrigger,
                        PresentationWorkbenchExportKind.ProductTransform,
                        animationUsesBool: animationUsesBool);
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

        private static void ResolveAvatarMetadata(
            string productId,
            out AvatarHand hand,
            out string animationTrigger,
            out bool animationUsesBool)
        {
            hand = AvatarHand.Right;
            animationTrigger = "RightArm_Hold_ClosedHand";
            animationUsesBool = true;

            ItemDefinition? item = ItemManager.GetDefinition(productId);
            S1AvatarEquipping.AvatarEquippable? avatar =
                item?.S1ItemDefinition.Equippable
                    ?.GetComponentInChildren<
                        S1AvatarEquipping.AvatarEquippable>(true);
            if (avatar == null)
                return;

            hand =
                avatar.Hand == S1AvatarEquipping.AvatarEquippable.EHand.Left
                    ? AvatarHand.Left
                    : AvatarHand.Right;
            if (!string.IsNullOrWhiteSpace(avatar.AnimationTrigger))
                animationTrigger = avatar.AnimationTrigger;
            animationUsesBool =
                avatar.TriggerType ==
                S1AvatarEquipping.AvatarEquippable.ETriggerType.Bool;
        }
    }
}
