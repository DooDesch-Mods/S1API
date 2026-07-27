#if (IL2CPPMELON)
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
#elif MONOMELON
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
#endif

using System;
using S1API.Items;
using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal static class ItemPresentationWorkbenchAdapter
    {
        private const int DefaultIconSize = 512;
        private const float DefaultCameraFill = 0.72f;

        internal static bool TryCreateDefinition(
            string itemId,
            out PresentationWorkbenchDefinition? definition)
        {
            definition = null;
            ItemDefinition? item = ItemManager.GetDefinition(itemId);
            if (item?.S1ItemDefinition.Equippable == null)
                return false;

            GameObject equippableRoot =
                item.S1ItemDefinition.Equippable.gameObject;
            PresentationWorkbenchDefinition.AvatarPreviewContext? avatar =
                TryCreateAvatarContext(equippableRoot);
            GameObject? firstPersonVisual =
                PresentationWorkbenchVisualResolver.FindVisibleRoot(
                    equippableRoot);
            if (firstPersonVisual == null && avatar == null)
                return false;

            PresentationWorkbenchDefinition.PreviewContext? firstPerson =
                firstPersonVisual == null
                    ? null
                    : new PresentationWorkbenchDefinition.PreviewContext(
                        () => ResolveItemVisual(itemId),
                        PresentationWorkbenchTransform.From(
                            firstPersonVisual.transform),
                        PresentationWorkbenchExportKind.LocalTransformAssignments);

            GameObject? iconVisual =
                firstPersonVisual ?? avatar!.Provider();
            if (iconVisual == null)
                return false;

            Func<GameObject?> iconProvider =
                firstPersonVisual != null
                    ? () => ResolveItemVisual(itemId)
                    : avatar!.Provider;
            var icon =
                new PresentationWorkbenchDefinition.IconPreviewContext(
                    iconProvider,
                    new PresentationWorkbenchTransform(
                        Vector3.zero,
                        iconVisual.transform.localEulerAngles,
                        iconVisual.transform.localScale),
                    DefaultIconSize,
                    fitToCamera: true,
                    DefaultCameraFill);

            definition =
                new PresentationWorkbenchDefinition(
                    item.ID,
                    $"Item presentation: {item.Name}",
                    firstPerson,
                    avatar,
                    icon);
            return true;
        }

        private static PresentationWorkbenchDefinition.AvatarPreviewContext?
            TryCreateAvatarContext(GameObject equippableRoot)
        {
            S1AvatarEquipping.AvatarEquippable? avatarEquippable =
                equippableRoot.GetComponentInChildren<
                    S1AvatarEquipping.AvatarEquippable>(true);
            if (avatarEquippable == null ||
                string.IsNullOrWhiteSpace(avatarEquippable.AssetPath))
            {
                return null;
            }

            string assetPath = avatarEquippable.AssetPath;
            GameObject? visual = ResolveAvatarVisual(assetPath);
            if (visual == null)
                return null;

            AvatarHand hand =
                avatarEquippable.Hand ==
                S1AvatarEquipping.AvatarEquippable.EHand.Left
                    ? AvatarHand.Left
                    : AvatarHand.Right;
            string animationTrigger =
                string.IsNullOrWhiteSpace(avatarEquippable.AnimationTrigger)
                    ? "RightArm_Hold_ClosedHand"
                    : avatarEquippable.AnimationTrigger;
            return new PresentationWorkbenchDefinition.AvatarPreviewContext(
                () => ResolveAvatarVisual(assetPath),
                PresentationWorkbenchTransform.From(visual.transform),
                hand,
                animationTrigger,
                PresentationWorkbenchExportKind.LocalTransformAssignments,
                () => ResolveAvatarPrefab(assetPath),
                PresentationWorkbenchVisualResolver.FindVisibleRoot,
                alignAvatarEquippable: true,
                animationUsesBool:
                    avatarEquippable.TriggerType ==
                    S1AvatarEquipping.AvatarEquippable.ETriggerType.Bool);
        }

        private static GameObject? ResolveItemVisual(string itemId)
        {
            ItemDefinition? item = ItemManager.GetDefinition(itemId);
            return PresentationWorkbenchVisualResolver.FindVisibleRoot(
                item?.S1ItemDefinition.Equippable?.gameObject);
        }

        private static GameObject? ResolveAvatarVisual(string assetPath) =>
            PresentationWorkbenchVisualResolver.FindVisibleRoot(
                ResolveAvatarPrefab(assetPath));

        private static GameObject? ResolveAvatarPrefab(string assetPath) =>
            AvatarEquippableRegistry.GetRegisteredPrefab(assetPath) ??
            Resources.Load<GameObject>(assetPath);
    }
}
