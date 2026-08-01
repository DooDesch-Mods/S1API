using System;
using S1API.Internal.Cutscenes;
using S1API.Logging;

namespace S1API.Cutscenes
{
    /// <summary>
    /// Coordinates the single S1API-managed local cutscene that may play at a time.
    /// </summary>
    public static class CutsceneManager
    {
        private static readonly Log Logger = new Log("Cutscenes");
        private static NativeCutsceneAdapter? _adapter;
        private static bool _ending;

        /// <summary>Gets the active S1API-managed cutscene, or null when none is playing.</summary>
        public static CutsceneHandle? Active { get; private set; }

        /// <summary>Gets whether an S1API-managed local cutscene is currently playing.</summary>
        public static bool IsPlaying =>
            Active != null;

        /// <summary>Stops the active cutscene, if one exists.</summary>
        /// <returns><see langword="true"/> when active playback was stopped.</returns>
        public static bool StopActive()
        {
            CutsceneHandle? active = Active;
            return active != null && TryEnd(active, CutsceneEndReason.Stopped);
        }

        internal static bool TryPlay(CutsceneHandle handle)
        {
            if (Active != null)
            {
                Logger.Warning(
                    $"Could not play cutscene '{handle.Id}' because '{Active.Id}' is already active.");
                return false;
            }

            var adapter = new NativeCutsceneAdapter();
            _adapter = adapter;
            Active = handle;

            if (handle.Begin(adapter))
            {
                return true;
            }

            if (ReferenceEquals(Active, handle))
            {
                EndInternal(handle, CutsceneEndReason.Failed);
            }

            return false;
        }

        internal static bool TryEnd(CutsceneHandle handle, CutsceneEndReason reason)
        {
            if (!ReferenceEquals(Active, handle) || _ending)
            {
                return false;
            }

            EndInternal(handle, reason);
            return true;
        }

        internal static void Tick(float unscaledDeltaTime)
        {
            Active?.Tick(unscaledDeltaTime);
        }

        internal static void DrawPresentation()
        {
            Active?.DrawPresentation();
        }

        internal static void CleanupForSceneChange()
        {
            CutsceneHandle? active = Active;
            if (active != null)
            {
                EndInternal(active, CutsceneEndReason.SceneChanged);
            }
        }

        internal static void Deinitialize()
        {
            CutsceneHandle? active = Active;
            if (active != null)
            {
                EndInternal(active, CutsceneEndReason.Stopped);
            }
        }

        internal static void FailFromCallback(CutsceneHandle handle, Exception exception)
        {
            LogCallbackFailure(handle.Id, "camera/started", exception);
            TryEnd(handle, CutsceneEndReason.Failed);
        }

        internal static void LogCallbackFailure(string id, string callbackName, Exception exception)
        {
            Logger.Error(
                $"Cutscene '{id}' {callbackName} callback failed: " +
                $"{exception.GetType().Name}: {exception.Message}");
        }

        private static void EndInternal(CutsceneHandle handle, CutsceneEndReason requestedReason)
        {
            _ending = true;
            CutsceneEndReason finalReason = requestedReason;

            try
            {
                if (_adapter != null && !_adapter.TryEnd(out Exception? cleanupException))
                {
                    finalReason = CutsceneEndReason.Failed;
                    if (cleanupException != null)
                    {
                        Logger.Error(
                            $"Cutscene '{handle.Id}' native cleanup failed: " +
                            $"{cleanupException.GetType().Name}: {cleanupException.Message}");
                    }
                }
            }
            finally
            {
                _adapter = null;
                Active = null;
                _ending = false;
                handle.Complete(finalReason);
            }
        }
    }
}
