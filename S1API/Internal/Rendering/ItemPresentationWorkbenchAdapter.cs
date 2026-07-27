#if (IL2CPPMELON)
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
#elif MONOMELON
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
#endif

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
            GameObject? firstPersonVisual =
                PresentationWorkbenchVisualResolver.FindVisibleRoot(
                    equippableRoot);
            if (firstPersonVisual == null)
                return false;

            var firstPerson =
                new PresentationWorkbenchDefinition.PreviewContext(
                    () => ResolveItemVisual(itemId),
                    PresentationWorkbenchTransform.From(
                        firstPersonVisual.transform),
                    PresentationWorkbenchExportKind.LocalTransformAssignments);

            PresentationWorkbenchDefinition.AvatarPreviewContext? avatar =
                TryCreateAvatarContext(equippableRoot);
            var icon =
                new PresentationWorkbenchDefinition.IconPreviewContext(
                    () => ResolveItemVisual(itemId),
                    new PresentationWorkbenchTransform(
                        Vector3.zero,
                        firstPersonVisual.transform.localEulerAngles,
                        firstPersonVisual.transform.localScale),
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

        internal static bool TryCreateAvatarDefinition(
            string assetPath,
            out PresentationWorkbenchDefinition? definition)
        {
            definition = null;
            GameObject? prefab = ResolveAvatarPrefab(assetPath);
            GameObject? visual =
                PresentationWorkbenchVisualResolver.FindVisibleRoot(prefab);
            if (prefab == null || visual == null)
                return false;

            S1AvatarEquipping.AvatarEquippable? avatarEquippable =
                prefab.GetComponentInChildren<
                    S1AvatarEquipping.AvatarEquippable>(true);
            AvatarHand hand =
                avatarEquippable != null &&
                avatarEquippable.Hand ==
                S1AvatarEquipping.AvatarEquippable.EHand.Left
                    ? AvatarHand.Left
                    : AvatarHand.Right;
            string animationTrigger =
                avatarEquippable?.AnimationTrigger ??
                "RightArm_Hold_ClosedHand";

            definition =
                new PresentationWorkbenchDefinition(
                    assetPath,
                    $"Avatar equippable: {assetPath}",
                    firstPerson: null,
                    new PresentationWorkbenchDefinition.AvatarPreviewContext(
                        () => ResolveAvatarVisual(assetPath),
                        PresentationWorkbenchTransform.From(visual.transform),
                        hand,
                        animationTrigger,
                        PresentationWorkbenchExportKind.LocalTransformAssignments),
                    icon: null);
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
            return new PresentationWorkbenchDefinition.AvatarPreviewContext(
                () => ResolveAvatarVisual(assetPath),
                PresentationWorkbenchTransform.From(visual.transform),
                hand,
                avatarEquippable.AnimationTrigger ??
                "RightArm_Hold_ClosedHand",
                PresentationWorkbenchExportKind.LocalTransformAssignments);
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
