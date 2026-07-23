using System;
using S1API.Internal.Cutscenes;

namespace S1API.Cutscenes
{
    /// <summary>
    /// Controls one configured local cutscene and reports its current playback state.
    /// </summary>
    public sealed class CutsceneHandle : IDisposable
    {
        private readonly CutsceneConfiguration _configuration;
        private CutsceneCamera? _camera;
        private CutscenePresentationRuntime? _presentation;
        private bool _disposed;
        private bool _endCallbackPending;

        /// <summary>Gets the stable, mod-namespaced cutscene identifier.</summary>
        public string Id =>
            _configuration.Id;

        /// <summary>Gets the descriptive cutscene name.</summary>
        public string Name =>
            _configuration.Name;

        /// <summary>Gets whether this handle is the currently playing S1API cutscene.</summary>
        public bool IsPlaying =>
            ReferenceEquals(CutsceneManager.Active, this);

        /// <summary>Gets elapsed playback time in seconds.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Gets normalized playback progress in the inclusive range from zero to one.</summary>
        public float Progress =>
            Math.Min(1f, Elapsed / _configuration.Duration);

        /// <summary>Gets the reason the most recent playback ended, or null before it ends.</summary>
        public CutsceneEndReason? EndReason { get; private set; }

        internal CutsceneHandle(CutsceneConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Attempts to start local playback.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when playback started; otherwise <see langword="false"/>.
        /// Runtime unavailability and overlapping playback are logged and fail safely.
        /// </returns>
        public bool Play()
        {
            if (_disposed)
            {
                return false;
            }

            return CutsceneManager.TryPlay(this);
        }

        /// <summary>Stops this cutscene if it is currently playing.</summary>
        /// <returns><see langword="true"/> when active playback was stopped.</returns>
        public bool Stop() =>
            CutsceneManager.TryEnd(this, CutsceneEndReason.Stopped);

        /// <summary>Skips this cutscene if it is currently playing.</summary>
        /// <returns><see langword="true"/> when active playback was skipped.</returns>
        public bool Skip() =>
            CutsceneManager.TryEnd(this, CutsceneEndReason.Skipped);

        /// <summary>
        /// Stops active playback and prevents this handle from being played again.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _disposed = true;
        }

        internal bool Begin(NativeCutsceneAdapter adapter)
        {
            Elapsed = 0f;
            EndReason = null;
            _endCallbackPending = true;

            if (!adapter.TryBegin(
                    _configuration.Id,
                    _configuration.Name,
                    _configuration.InitialPosition,
                    _configuration.InitialRotation,
                    _configuration.Fov,
                    out _camera))
            {
                _endCallbackPending = false;
                _camera = null;
                EndReason = CutsceneEndReason.Failed;
                return false;
            }

            try
            {
                _configuration.Started?.Invoke();
                InvokeCameraUpdate(0f);
                _presentation = new CutscenePresentationRuntime(_configuration);
                _presentation.Begin();
                return true;
            }
            catch (Exception ex)
            {
                CutsceneManager.FailFromCallback(this, ex);
                return false;
            }
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (_camera == null)
            {
                CutsceneManager.TryEnd(this, CutsceneEndReason.Failed);
                return;
            }

            float deltaTime = Math.Max(0f, unscaledDeltaTime);
            Elapsed = Math.Min(_configuration.Duration, Elapsed + deltaTime);

            try
            {
                InvokeCameraUpdate(deltaTime);
                if (_presentation?.Tick(deltaTime, Elapsed) == true)
                {
                    CutsceneManager.TryEnd(this, CutsceneEndReason.Skipped);
                    return;
                }
            }
            catch (Exception ex)
            {
                CutsceneManager.FailFromCallback(this, ex);
                return;
            }

            if (Elapsed >= _configuration.Duration)
            {
                CutsceneManager.TryEnd(this, CutsceneEndReason.Completed);
            }
        }

        internal void Complete(CutsceneEndReason reason)
        {
            _presentation?.End(reason);
            _presentation = null;
            _camera = null;
            EndReason = reason;

            if (!_endCallbackPending)
            {
                return;
            }

            _endCallbackPending = false;
            try
            {
                _configuration.Ended?.Invoke(reason);
            }
            catch (Exception ex)
            {
                CutsceneManager.LogCallbackFailure(Id, "ended", ex);
            }
        }

        internal void DrawPresentation()
        {
            _presentation?.Draw();
        }

        private void InvokeCameraUpdate(float unscaledDeltaTime)
        {
            if (_configuration.CameraUpdate == null || _camera == null)
            {
                return;
            }

            _configuration.CameraUpdate(
                new CutsceneFrame(_camera, unscaledDeltaTime, Elapsed, Progress));
        }
    }
}
