namespace S1API.Cutscenes
{
    /// <summary>
    /// Describes why an S1API-managed cutscene ended.
    /// </summary>
    public enum CutsceneEndReason
    {
        /// <summary>The configured duration elapsed normally.</summary>
        Completed,

        /// <summary>The cutscene was skipped by the caller or hold-to-skip input.</summary>
        Skipped,

        /// <summary>The cutscene was stopped explicitly or disposed while playing.</summary>
        Stopped,

        /// <summary>The active game scene changed while the cutscene was playing.</summary>
        SceneChanged,

        /// <summary>The cutscene could not continue because runtime setup or a callback failed.</summary>
        Failed
    }
}
