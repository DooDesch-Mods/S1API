namespace S1API.Cutscenes
{
    /// <summary>
    /// Supplies timing and camera state to a cutscene's per-frame update callback.
    /// </summary>
    public sealed class CutsceneFrame
    {
        /// <summary>Gets the managed camera-control surface for the active cutscene.</summary>
        public CutsceneCamera Camera { get; }

        /// <summary>Gets the current frame's unscaled delta time in seconds.</summary>
        public float UnscaledDeltaTime { get; }

        /// <summary>Gets the elapsed playback time in seconds.</summary>
        public float Elapsed { get; }

        /// <summary>Gets normalized playback progress in the inclusive range from zero to one.</summary>
        public float Progress { get; }

        internal CutsceneFrame(
            CutsceneCamera camera,
            float unscaledDeltaTime,
            float elapsed,
            float progress)
        {
            Camera = camera;
            UnscaledDeltaTime = unscaledDeltaTime;
            Elapsed = elapsed;
            Progress = progress;
        }
    }
}
