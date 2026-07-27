using S1API.Internal.Rendering;

namespace S1API.Rendering
{
    /// <summary>
    /// Opens and controls the local in-game presentation workbench.
    /// </summary>
    /// <remarks>
    /// The workbench is an authoring aid. It clones registered sources, does not mutate
    /// definitions, and does not persist or transmit edited values.
    /// </remarks>
    public static class PresentationWorkbench
    {
        /// <summary>
        /// Gets whether a workbench session is currently open.
        /// </summary>
        public static bool IsOpen => PresentationWorkbenchRuntime.IsOpen;

        /// <summary>
        /// Gets the active definition ID, or null when the workbench is closed.
        /// </summary>
        public static string? ActiveDefinitionId =>
            PresentationWorkbenchRuntime.ActiveDefinitionId;

        /// <summary>
        /// Opens a registered workbench definition.
        /// </summary>
        /// <param name="id">The stable workbench or product-presentation ID.</param>
        /// <returns>
        /// True if the workbench opened; otherwise, false when the definition or required
        /// gameplay objects are unavailable.
        /// </returns>
        public static bool Open(string id) =>
            PresentationWorkbenchRuntime.Open(id);

        /// <summary>
        /// Closes the active workbench and releases all temporary preview objects.
        /// </summary>
        public static void Close() =>
            PresentationWorkbenchRuntime.Close();
    }
}
