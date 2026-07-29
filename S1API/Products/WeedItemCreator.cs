namespace S1API.Products
{
    /// <summary>
    /// Creates native-family weed variants through Schedule One's product lifecycle.
    /// </summary>
    public static class WeedItemCreator
    {
        /// <summary>
        /// Creates a builder for a marijuana-family weed variant.
        /// </summary>
        /// <param name="id">A stable namespaced ID in the form <c>mod-id:product-id</c>.</param>
        /// <returns>A new weed definition builder.</returns>
        public static WeedDefinitionBuilder CreateBuilder(string id)
        {
            return new WeedDefinitionBuilder(id);
        }
    }
}
