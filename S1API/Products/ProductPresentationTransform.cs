using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Defines an explicit local transform for a product presentation visual.
    /// </summary>
    /// <remarks>
    /// Presentation transforms are applied to S1API's cloned visual root. Omit a transform
    /// override to preserve the position, rotation, and scale authored by the provider.
    /// </remarks>
    public sealed class ProductPresentationTransform
    {
        /// <summary>
        /// Initializes a product presentation transform.
        /// </summary>
        /// <param name="localPosition">The cloned visual root's local position.</param>
        /// <param name="localEulerAngles">The cloned visual root's local Euler rotation.</param>
        /// <param name="localScale">The cloned visual root's local scale.</param>
        public ProductPresentationTransform(
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
            LocalScale = localScale;
        }

        /// <summary>
        /// Gets the cloned visual root's local position.
        /// </summary>
        public Vector3 LocalPosition { get; }

        /// <summary>
        /// Gets the cloned visual root's local Euler rotation.
        /// </summary>
        public Vector3 LocalEulerAngles { get; }

        /// <summary>
        /// Gets the cloned visual root's local scale.
        /// </summary>
        public Vector3 LocalScale { get; }

        internal void ApplyTo(Transform target)
        {
            target.localPosition = LocalPosition;
            target.localEulerAngles = LocalEulerAngles;
            target.localScale = LocalScale;
        }
    }
}
