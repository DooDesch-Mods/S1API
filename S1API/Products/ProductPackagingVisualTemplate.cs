namespace S1API.Products
{
    /// <summary>
    /// Selects a game-owned filled-product visual template to clone at runtime.
    /// </summary>
    /// <remarks>
    /// This value controls presentation only. It does not change a custom product's
    /// logical kind, compatibility drug type, save data, or network representation.
    /// The template is resolved independently for stored, equipped, and icon contexts.
    /// </remarks>
    public enum ProductPackagingVisualTemplate
    {
        /// <summary>
        /// Uses the marijuana-family filled visual template.
        /// </summary>
        Marijuana = 0,

        /// <summary>
        /// Uses the methamphetamine-family filled visual template.
        /// </summary>
        Methamphetamine = 1,

        /// <summary>
        /// Uses the cocaine-family filled visual template.
        /// </summary>
        Cocaine = 2,

        /// <summary>
        /// Uses the shroom-family filled visual template.
        /// </summary>
        Shrooms = 3
    }
}
