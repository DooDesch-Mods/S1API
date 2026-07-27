using System;
using S1API.Items;
using UnityEngine;

namespace S1API.Rendering
{
    /// <summary>
    /// Describes the visual sources and initial values available to the presentation workbench.
    /// </summary>
    /// <remarks>
    /// Source providers return reusable prefab roots. The workbench clones each returned object
    /// and never mutates the provider-owned source.
    /// </remarks>
    public sealed class PresentationWorkbenchDefinition
    {
        internal PresentationWorkbenchDefinition(
            string id,
            string displayName,
            PreviewContext? firstPerson,
            AvatarPreviewContext? avatar,
            IconPreviewContext? icon)
        {
            Id = id;
            DisplayName = displayName;
            FirstPerson = firstPerson;
            Avatar = avatar;
            Icon = icon;
        }

        /// <summary>
        /// Gets the stable namespaced authoring ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the name displayed by the workbench.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets whether the definition supports first-person preview.
        /// </summary>
        public bool SupportsFirstPerson => FirstPerson != null;

        /// <summary>
        /// Gets whether the definition supports avatar preview.
        /// </summary>
        public bool SupportsAvatar => Avatar != null;

        /// <summary>
        /// Gets whether the definition supports native icon preview.
        /// </summary>
        public bool SupportsIcon => Icon != null;

        internal PreviewContext? FirstPerson { get; }

        internal AvatarPreviewContext? Avatar { get; }

        internal IconPreviewContext? Icon { get; }

        internal sealed class PreviewContext
        {
            internal PreviewContext(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform? initialTransform)
            {
                Provider = provider;
                InitialTransform = initialTransform;
            }

            internal Func<GameObject?> Provider { get; }

            internal PresentationWorkbenchTransform? InitialTransform { get; }
        }

        internal sealed class AvatarPreviewContext
        {
            internal AvatarPreviewContext(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform? initialTransform,
                AvatarHand hand,
                string animationTrigger)
            {
                Provider = provider;
                InitialTransform = initialTransform;
                Hand = hand;
                AnimationTrigger = animationTrigger;
            }

            internal Func<GameObject?> Provider { get; }

            internal PresentationWorkbenchTransform? InitialTransform { get; }

            internal AvatarHand Hand { get; }

            internal string AnimationTrigger { get; }
        }

        internal sealed class IconPreviewContext
        {
            internal IconPreviewContext(
                Func<GameObject?> provider,
                Vector3 initialEulerAngles,
                Vector3 initialScale,
                int size,
                bool fitToCamera,
                float cameraFill)
            {
                Provider = provider;
                InitialEulerAngles = initialEulerAngles;
                InitialScale = initialScale;
                Size = size;
                FitToCamera = fitToCamera;
                CameraFill = cameraFill;
            }

            internal Func<GameObject?> Provider { get; }

            internal Vector3 InitialEulerAngles { get; }

            internal Vector3 InitialScale { get; }

            internal int Size { get; }

            internal bool FitToCamera { get; }

            internal float CameraFill { get; }
        }
    }
}
