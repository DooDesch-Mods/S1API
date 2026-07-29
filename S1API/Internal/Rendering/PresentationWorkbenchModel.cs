using System;
using S1API.Items;
using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal enum PresentationWorkbenchMode
    {
        FirstPerson = 0,
        Avatar = 1,
        Icon = 2,
    }

    internal enum PresentationWorkbenchExportKind
    {
        ProductTransform = 0,
        LocalTransformAssignments = 1,
        IconFactory = 2,
    }

    internal sealed class PresentationWorkbenchTransform
    {
        internal PresentationWorkbenchTransform(
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
            LocalScale = localScale;
        }

        internal Vector3 LocalPosition { get; }

        internal Vector3 LocalEulerAngles { get; }

        internal Vector3 LocalScale { get; }

        internal void ApplyTo(Transform target)
        {
            target.localPosition = LocalPosition;
            target.localEulerAngles = LocalEulerAngles;
            target.localScale = LocalScale;
        }

        internal static PresentationWorkbenchTransform From(Transform target) =>
            new PresentationWorkbenchTransform(
                target.localPosition,
                target.localEulerAngles,
                target.localScale);
    }

    internal sealed class PresentationWorkbenchDefinition
    {
        internal PresentationWorkbenchDefinition(
            string id,
            string displayName,
            PreviewContext? firstPerson,
            AvatarPreviewContext? avatar,
            IconPreviewContext? icon)
        {
            if (firstPerson == null && avatar == null && icon == null)
            {
                throw new ArgumentException(
                    "A presentation target must provide at least one preview.",
                    nameof(firstPerson));
            }

            Id = id;
            DisplayName = displayName;
            FirstPerson = firstPerson;
            Avatar = avatar;
            Icon = icon;
        }

        internal string Id { get; }

        internal string DisplayName { get; }

        internal bool SupportsFirstPerson => FirstPerson != null;

        internal bool SupportsAvatar => Avatar != null;

        internal bool SupportsIcon => Icon != null;

        internal PreviewContext? FirstPerson { get; }

        internal AvatarPreviewContext? Avatar { get; }

        internal IconPreviewContext? Icon { get; }

        internal class PreviewContext
        {
            internal PreviewContext(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform? initialTransform,
                PresentationWorkbenchExportKind exportKind)
            {
                Provider = provider;
                InitialTransform = initialTransform;
                ExportKind = exportKind;
            }

            internal Func<GameObject?> Provider { get; }

            internal PresentationWorkbenchTransform? InitialTransform { get; }

            internal PresentationWorkbenchExportKind ExportKind { get; }
        }

        internal sealed class AvatarPreviewContext : PreviewContext
        {
            internal AvatarPreviewContext(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform? initialTransform,
                AvatarHand hand,
                string animationTrigger,
                PresentationWorkbenchExportKind exportKind,
                Func<GameObject?>? previewRootProvider = null,
                Func<GameObject, GameObject?>? editableVisualResolver = null,
                bool alignAvatarEquippable = false,
                bool animationUsesBool = true)
                : base(provider, initialTransform, exportKind)
            {
                Hand = hand;
                AnimationTrigger = animationTrigger;
                PreviewRootProvider = previewRootProvider ?? provider;
                EditableVisualResolver =
                    editableVisualResolver ?? (root => root);
                AlignAvatarEquippable = alignAvatarEquippable;
                AnimationUsesBool = animationUsesBool;
            }

            internal AvatarHand Hand { get; }

            internal string AnimationTrigger { get; }

            internal Func<GameObject?> PreviewRootProvider { get; }

            internal Func<GameObject, GameObject?> EditableVisualResolver
            {
                get;
            }

            internal bool AlignAvatarEquippable { get; }

            internal bool AnimationUsesBool { get; }
        }

        internal sealed class IconPreviewContext : PreviewContext
        {
            internal IconPreviewContext(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform initialTransform,
                int size,
                bool fitToCamera,
                float cameraFill)
                : base(
                    provider,
                    initialTransform,
                    PresentationWorkbenchExportKind.IconFactory)
            {
                Size = size;
                FitToCamera = fitToCamera;
                CameraFill = cameraFill;
            }

            internal int Size { get; }

            internal bool FitToCamera { get; }

            internal float CameraFill { get; }
        }
    }
}
