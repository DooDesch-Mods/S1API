using System;
using S1API.Internal.Cutscenes;
using UnityEngine;

namespace S1API.Cutscenes
{
    /// <summary>
    /// Builds a local, camera-driven cutscene with a managed cross-runtime API.
    /// </summary>
    public sealed class CutsceneBuilder
    {
        private readonly string _id;
        private string _name;
        private float _duration;
        private float? _fov;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation = Quaternion.identity;
        private Action<CutsceneFrame>? _cameraUpdate;
        private Action? _started;
        private Action<CutsceneEndReason>? _ended;
        private float _fadeInDuration;
        private float _fadeInDelay;
        private float _fadeOutDuration;
        private float _holdToSkipDuration;
        private string? _titleCardText;
        private float _titleCardDuration;
        private bool _hasDuration;
        private bool _hasInitialCamera;

        /// <summary>
        /// Creates a builder for a uniquely identified local cutscene.
        /// </summary>
        /// <param name="id">
        /// A stable, mod-namespaced identifier such as <c>my-mod:crew-arrival</c>.
        /// </param>
        public CutsceneBuilder(string id)
        {
            _id = NormalizeId(id);
            _name = _id;
        }

        /// <summary>Sets the descriptive name used by the native cutscene state.</summary>
        /// <param name="name">A non-empty descriptive name.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder WithName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            _name = name.Trim();
            if (_name.Length == 0)
            {
                throw new ArgumentException("Cutscene names cannot be empty or whitespace.", nameof(name));
            }

            return this;
        }

        /// <summary>Sets the cutscene playback duration.</summary>
        /// <param name="seconds">A finite duration greater than zero, in seconds.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder WithDuration(float seconds)
        {
            ValidatePositiveFinite(seconds, nameof(seconds));
            _duration = seconds;
            _hasDuration = true;
            return this;
        }

        /// <summary>Overrides the player camera's field of view while the cutscene is active.</summary>
        /// <param name="fov">A finite field of view greater than zero and less than 180 degrees.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder WithFov(float fov)
        {
            if (!IsFinite(fov) || fov <= 0f || fov >= 180f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fov),
                    fov,
                    "Cutscene FOV must be finite, greater than zero, and less than 180 degrees.");
            }

            _fov = fov;
            return this;
        }

        /// <summary>Sets the initial world-space camera position and rotation.</summary>
        /// <param name="position">The initial world-space position.</param>
        /// <param name="rotation">The initial world-space rotation.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder WithInitialCamera(Vector3 position, Quaternion rotation)
        {
            if (!IsFinite(position))
            {
                throw new ArgumentException("The initial camera position must contain finite values.", nameof(position));
            }

            if (!IsFinite(rotation) || rotation == default)
            {
                throw new ArgumentException(
                    "The initial camera rotation must be a finite, non-zero quaternion.",
                    nameof(rotation));
            }

            _initialPosition = position;
            _initialRotation = rotation.normalized;
            _hasInitialCamera = true;
            return this;
        }

        /// <summary>Sets the callback that updates the managed camera transform each frame.</summary>
        /// <param name="update">The per-frame camera callback.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder WithCameraUpdate(Action<CutsceneFrame> update)
        {
            _cameraUpdate = update ?? throw new ArgumentNullException(nameof(update));
            return this;
        }

        /// <summary>Sets the callback invoked after native playback starts successfully.</summary>
        /// <param name="callback">The playback-started callback.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder OnStarted(Action callback)
        {
            _started = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }

        /// <summary>Sets the callback invoked exactly once when a playback attempt ends.</summary>
        /// <param name="callback">The callback that receives the reason playback ended.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder OnEnded(Action<CutsceneEndReason> callback)
        {
            _ended = callback ?? throw new ArgumentNullException(nameof(callback));
            return this;
        }

        /// <summary>Fades from black when playback begins.</summary>
        /// <param name="seconds">A finite fade duration greater than or equal to zero.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <remarks>
        /// The helper does not alter a black overlay that was already shown by another game system.
        /// A duration of zero disables the helper.
        /// </remarks>
        public CutsceneBuilder WithFadeIn(float seconds)
        {
            return WithFadeIn(seconds, 0f);
        }

        /// <summary>Holds on black, then fades in after a delay.</summary>
        /// <param name="seconds">A finite fade duration greater than or equal to zero.</param>
        /// <param name="delaySeconds">
        /// A finite delay greater than or equal to zero before the fade begins.
        /// </param>
        /// <returns>This builder for method chaining.</returns>
        /// <remarks>
        /// The helper does not alter a black overlay that was already shown by another game system.
        /// A fade duration of zero disables the helper.
        /// </remarks>
        public CutsceneBuilder WithFadeIn(float seconds, float delaySeconds)
        {
            ValidateNonNegativeFinite(seconds, nameof(seconds));
            ValidateNonNegativeFinite(delaySeconds, nameof(delaySeconds));
            _fadeInDuration = seconds;
            _fadeInDelay = delaySeconds;
            return this;
        }

        /// <summary>Fades to black near the end of normal playback, then releases the overlay.</summary>
        /// <param name="seconds">A finite fade duration greater than or equal to zero.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <remarks>
        /// The helper does not close a black overlay that was already owned by another game system.
        /// A duration of zero disables the helper.
        /// </remarks>
        public CutsceneBuilder WithFadeOut(float seconds)
        {
            ValidateNonNegativeFinite(seconds, nameof(seconds));
            _fadeOutDuration = seconds;
            return this;
        }

        /// <summary>Allows the local player to hold primary click to skip playback.</summary>
        /// <param name="seconds">The finite hold duration required to skip.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder WithHoldToSkip(float seconds = 0.5f)
        {
            ValidatePositiveFinite(seconds, nameof(seconds));
            _holdToSkipDuration = seconds;
            return this;
        }

        /// <summary>Displays a simple centered title card during the start of playback.</summary>
        /// <param name="text">The non-empty title text.</param>
        /// <param name="duration">The finite title duration greater than zero.</param>
        /// <returns>This builder for method chaining.</returns>
        public CutsceneBuilder WithTitleCard(string text, float duration = 2f)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            string normalizedText = text.Trim();
            if (normalizedText.Length == 0)
            {
                throw new ArgumentException("Title card text cannot be empty or whitespace.", nameof(text));
            }

            ValidatePositiveFinite(duration, nameof(duration));
            _titleCardText = normalizedText;
            _titleCardDuration = duration;
            return this;
        }

        /// <summary>Builds a reusable managed handle for the configured cutscene.</summary>
        /// <returns>A cutscene handle that can start and control local playback.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when duration or the initial camera pose has not been configured.
        /// </exception>
        public CutsceneHandle Build()
        {
            if (!_hasDuration)
            {
                throw new InvalidOperationException("A cutscene duration must be configured with WithDuration().");
            }

            if (!_hasInitialCamera)
            {
                throw new InvalidOperationException(
                    "An initial camera pose must be configured with WithInitialCamera().");
            }

            return new CutsceneHandle(
                new CutsceneConfiguration(
                    _id,
                    _name,
                    _duration,
                    _fov,
                    _initialPosition,
                    _initialRotation,
                    _cameraUpdate,
                    _started,
                    _ended,
                    _fadeInDuration,
                    _fadeInDelay,
                    _fadeOutDuration,
                    _holdToSkipDuration,
                    _titleCardText,
                    _titleCardDuration));
        }

        private static string NormalizeId(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            string normalized = id.Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Cutscene IDs cannot be empty or whitespace.", nameof(id));
            }

            return normalized;
        }

        private static void ValidatePositiveFinite(float value, string parameterName)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "The value must be finite and greater than zero.");
            }
        }

        private static void ValidateNonNegativeFinite(float value, string parameterName)
        {
            if (!IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "The value must be finite and greater than or equal to zero.");
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        private static bool IsFinite(Quaternion value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    }
}
