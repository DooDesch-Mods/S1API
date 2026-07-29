namespace S1API.Products
{
    /// <summary>
    /// Identifies a custom product presentation context.
    /// </summary>
    public enum ProductPresentationContext
    {
        /// <summary>
        /// The shared loose visual used as the default source for rendered contexts.
        /// </summary>
        Loose = 0,

        /// <summary>
        /// The product while placed in storage.
        /// </summary>
        Stored = 1,

        /// <summary>
        /// The product while equipped in first or third person.
        /// </summary>
        Held = 2,

        /// <summary>
        /// The product while placed in a compatible station slot.
        /// </summary>
        Station = 3,

        /// <summary>
        /// The draggable product used by functional station interactions.
        /// </summary>
        FunctionalProduct = 4,

        /// <summary>
        /// The loose inventory icon.
        /// </summary>
        Icon = 5,

        /// <summary>
        /// The product consumption animation prefab.
        /// </summary>
        Consumption = 6
    }
}
