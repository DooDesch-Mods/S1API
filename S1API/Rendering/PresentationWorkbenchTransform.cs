using UnityEngine;

namespace S1API.Rendering
{
    /// <summary>
    /// Defines an immutable local transform used by a presentation preview.
    /// </summary>
    public sealed class PresentationWorkbenchTransform
    {
        /// <summary>
        /// Initializes a presentation transform.
        /// </summary>
        /// <param name="localPosition">The visual root's local position.</param>
        /// <param name="localEulerAngles">The visual root's local Euler angles.</param>
        /// <param name="localScale">The visual root's local scale.</param>
        public PresentationWorkbenchTransform(
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
            LocalScale = localScale;
        }

        /// <summary>
        /// Gets the visual root's local position.
        /// </summary>
        public Vector3 LocalPosition { get; }

        /// <summary>
        /// Gets the visual root's local Euler angles.
        /// </summary>
        public Vector3 LocalEulerAngles { get; }

        /// <summary>
        /// Gets the visual root's local scale.
        /// </summary>
        public Vector3 LocalScale { get; }

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
}
