namespace S1API.Rendering
{
    /// <summary>
    /// Identifies a presentation context supported by the in-game workbench.
    /// </summary>
    public enum PresentationWorkbenchMode
    {
        /// <summary>
        /// Shows the visual through the local player's first-person viewmodel.
        /// </summary>
        FirstPerson = 0,

        /// <summary>
        /// Shows the visual aligned to the local player's avatar hand.
        /// </summary>
        Avatar = 1,

        /// <summary>
        /// Shows a generated inventory icon captured by the native icon rig.
        /// </summary>
        Icon = 2,
    }
}
