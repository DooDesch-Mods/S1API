using UnityEngine;

namespace S1API.Cutscenes
{
    /// <summary>
    /// Provides managed control over the camera transform used by an active cutscene.
    /// </summary>
    public sealed class CutsceneCamera
    {
        private readonly Transform _transform;

        internal CutsceneCamera(Transform transform)
        {
            _transform = transform;
        }

        /// <summary>Gets or sets the camera-control position in world space.</summary>
        public Vector3 Position
        {
            get => _transform.position;
            set => _transform.position = value;
        }

        /// <summary>Gets or sets the camera-control rotation in world space.</summary>
        public Quaternion Rotation
        {
            get => _transform.rotation;
            set => _transform.rotation = value;
        }

        /// <summary>Gets the camera-control forward direction.</summary>
        public Vector3 Forward =>
            _transform.forward;

        /// <summary>
        /// Sets the camera-control position and rotation in world space.
        /// </summary>
        /// <param name="position">The new world-space position.</param>
        /// <param name="rotation">The new world-space rotation.</param>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _transform.SetPositionAndRotation(position, rotation);
        }
    }
}
