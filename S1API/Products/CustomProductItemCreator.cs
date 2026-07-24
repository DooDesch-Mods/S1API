namespace S1API.Products
{
    /// <summary>
    /// Creates generic non-mixable custom product definitions.
    /// </summary>
    public static class CustomProductItemCreator
    {
        /// <summary>
        /// Creates a builder for a generic custom product.
        /// </summary>
        /// <param name="id">A stable namespaced product ID in <c>mod-id:product-id</c> form.</param>
        /// <param name="productKind">
        /// The registered logical product kind, including the vanilla compatibility drug type
        /// required by native save and product-item data.
        /// </param>
        /// <returns>A new custom product definition builder.</returns>
        public static CustomProductDefinitionBuilder CreateBuilder(
            string id,
            ProductKind productKind)
        {
            return new CustomProductDefinitionBuilder(id, productKind);
        }
    }
}
