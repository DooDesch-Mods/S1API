namespace S1API.Items.Buildable
{
    /// <summary>
    /// Creates first-class custom furniture backed by the game's native building system.
    /// </summary>
    public static class FurnitureCreator
    {
        /// <summary>
        /// Creates a builder for a custom placeable furniture item.
        /// </summary>
        public static FurnitureDefinitionBuilder CreateBuilder()
        {
            return new FurnitureDefinitionBuilder();
        }
    }
}
