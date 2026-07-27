using System;
using S1API.Items;
using UnityEngine;

namespace S1API.Rendering
{
    /// <summary>
    /// Builds an immutable definition for the in-game presentation workbench.
    /// </summary>
    public sealed class PresentationWorkbenchDefinitionBuilder
    {
        private readonly string _id;
        private readonly string _displayName;
        private PresentationWorkbenchDefinition.PreviewContext? _firstPerson;
        private PresentationWorkbenchDefinition.AvatarPreviewContext? _avatar;
        private PresentationWorkbenchDefinition.IconPreviewContext? _icon;

        /// <summary>
        /// Initializes a workbench definition builder.
        /// </summary>
        /// <param name="id">
        /// The stable namespaced authoring ID, such as <c>example.mod:items/pill</c>.
        /// </param>
        /// <param name="displayName">The non-empty name displayed by the workbench.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when an argument is empty or <paramref name="id"/> is not namespaced.
        /// </exception>
        public PresentationWorkbenchDefinitionBuilder(string id, string displayName)
        {
            _id = NormalizeId(id, nameof(id));
            _displayName = NormalizeRequired(displayName, nameof(displayName));
        }

        /// <summary>
        /// Adds a first-person preview that preserves the source's authored transform.
        /// </summary>
        /// <param name="provider">A provider that returns a reusable visual source.</param>
        /// <returns>This builder.</returns>
        public PresentationWorkbenchDefinitionBuilder WithFirstPersonPreview(
            Func<GameObject?> provider)
        {
            _firstPerson =
                new PresentationWorkbenchDefinition.PreviewContext(
                    RequireProvider(provider),
                    initialTransform: null);
            return this;
        }

        /// <summary>
        /// Adds a first-person preview with an explicit initial transform.
        /// </summary>
        /// <param name="provider">A provider that returns a reusable visual source.</param>
        /// <param name="initialTransform">The initial viewmodel transform.</param>
        /// <returns>This builder.</returns>
        public PresentationWorkbenchDefinitionBuilder WithFirstPersonPreview(
            Func<GameObject?> provider,
            PresentationWorkbenchTransform initialTransform)
        {
            _firstPerson =
                new PresentationWorkbenchDefinition.PreviewContext(
                    RequireProvider(provider),
                    initialTransform ??
                    throw new ArgumentNullException(nameof(initialTransform)));
            return this;
        }

        /// <summary>
        /// Adds an avatar preview that preserves the source's authored transform.
        /// </summary>
        /// <param name="provider">A provider that returns a reusable visual source.</param>
        /// <param name="hand">The avatar hand used for alignment.</param>
        /// <param name="animationTrigger">
        /// The animation trigger exported with the authored values. Preview sessions do not
        /// send or apply the trigger.
        /// </param>
        /// <returns>This builder.</returns>
        public PresentationWorkbenchDefinitionBuilder WithAvatarPreview(
            Func<GameObject?> provider,
            AvatarHand hand = AvatarHand.Right,
            string animationTrigger = "RightArm_Hold_ClosedHand")
        {
            _avatar =
                CreateAvatarContext(
                    provider,
                    initialTransform: null,
                    hand,
                    animationTrigger);
            return this;
        }

        /// <summary>
        /// Adds an avatar preview with an explicit initial transform.
        /// </summary>
        /// <param name="provider">A provider that returns a reusable visual source.</param>
        /// <param name="initialTransform">The initial avatar-held transform.</param>
        /// <param name="hand">The avatar hand used for alignment.</param>
        /// <param name="animationTrigger">
        /// The animation trigger exported with the authored values. Preview sessions do not
        /// send or apply the trigger.
        /// </param>
        /// <returns>This builder.</returns>
        public PresentationWorkbenchDefinitionBuilder WithAvatarPreview(
            Func<GameObject?> provider,
            PresentationWorkbenchTransform initialTransform,
            AvatarHand hand = AvatarHand.Right,
            string animationTrigger = "RightArm_Hold_ClosedHand")
        {
            _avatar =
                CreateAvatarContext(
                    provider,
                    initialTransform ??
                    throw new ArgumentNullException(nameof(initialTransform)),
                    hand,
                    animationTrigger);
            return this;
        }

        /// <summary>
        /// Adds a native icon preview with explicit framing values.
        /// </summary>
        /// <param name="provider">A provider that returns a reusable icon visual source.</param>
        /// <param name="initialEulerAngles">The initial icon rotation.</param>
        /// <param name="fitToCamera">Whether renderer bounds are fitted to the native camera.</param>
        /// <param name="cameraFill">
        /// The target share of the native camera view, greater than zero through 2.
        /// </param>
        /// <param name="size">The square capture size, from 32 through 2048 pixels.</param>
        /// <param name="initialScale">
        /// The source scale used when automatic fitting is disabled.
        /// </param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="cameraFill"/> or <paramref name="size"/> is outside
        /// the supported range.
        /// </exception>
        public PresentationWorkbenchDefinitionBuilder WithIconPreview(
            Func<GameObject?> provider,
            Vector3 initialEulerAngles,
            bool fitToCamera = true,
            float cameraFill = 0.72f,
            int size = 512,
            Vector3? initialScale = null)
        {
            ValidateIconSettings(size, cameraFill);
            _icon =
                new PresentationWorkbenchDefinition.IconPreviewContext(
                    RequireProvider(provider),
                    initialEulerAngles,
                    initialScale ?? Vector3.one,
                    size,
                    fitToCamera,
                    cameraFill);
            return this;
        }

        /// <summary>
        /// Creates an immutable workbench definition snapshot.
        /// </summary>
        /// <returns>A new definition containing the currently configured contexts.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no preview context has been configured.
        /// </exception>
        public PresentationWorkbenchDefinition Build()
        {
            if (_firstPerson == null && _avatar == null && _icon == null)
            {
                throw new InvalidOperationException(
                    "A presentation workbench definition must configure at least one preview.");
            }

            return new PresentationWorkbenchDefinition(
                _id,
                _displayName,
                _firstPerson,
                _avatar,
                _icon);
        }

        private static PresentationWorkbenchDefinition.AvatarPreviewContext
            CreateAvatarContext(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform? initialTransform,
                AvatarHand hand,
                string animationTrigger)
        {
            if (!Enum.IsDefined(typeof(AvatarHand), hand))
                throw new ArgumentOutOfRangeException(nameof(hand), hand, null);

            return new PresentationWorkbenchDefinition.AvatarPreviewContext(
                RequireProvider(provider),
                initialTransform,
                hand,
                NormalizeRequired(animationTrigger, nameof(animationTrigger)));
        }

        private static Func<GameObject?> RequireProvider(Func<GameObject?> provider) =>
            provider ?? throw new ArgumentNullException(nameof(provider));

        private static void ValidateIconSettings(int size, float cameraFill)
        {
            if (size < 32 || size > 2048)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(size),
                    size,
                    "Icon preview size must be between 32 and 2048 pixels.");
            }

            if (cameraFill <= 0f || cameraFill > 2f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cameraFill),
                    cameraFill,
                    "Icon preview camera fill must be greater than zero and at most 2.");
            }
        }

        private static string NormalizeId(string value, string parameterName)
        {
            string normalized = NormalizeRequired(value, parameterName);
            int separator = normalized.IndexOf(':');
            if (separator <= 0 || separator == normalized.Length - 1)
            {
                throw new ArgumentException(
                    "Presentation workbench IDs must be namespaced, for example " +
                    "'example.mod:items/pill'.",
                    parameterName);
            }

            return normalized;
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);

            string normalized = value.Trim();
            if (normalized.Length == 0)
                throw new ArgumentException("Value cannot be empty.", parameterName);
            return normalized;
        }
    }
}
