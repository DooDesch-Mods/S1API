namespace S1API.Products
{
    /// <summary>
    /// Describes a logical product kind without depending on a native product definition or enum identity.
    /// </summary>
    /// <remarks>
    /// Product-kind descriptors are immutable and remain registered for the lifetime of the current process.
    /// </remarks>
    public sealed class ProductKindDescriptor
    {
        /// <summary>
        /// Gets the stable, namespaced identifier for this product kind.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the optional vanilla drug type that best represents this product kind for compatibility.
        /// </summary>
        /// <remarks>
        /// This value is metadata only. It does not define the descriptor's identity or create native product support.
        /// </remarks>
        public DrugType? CompatibilityDrugType { get; }

        internal ProductKindDescriptor(string id, DrugType? compatibilityDrugType)
        {
            Id = id;
            CompatibilityDrugType = compatibilityDrugType;
        }

        internal bool IsEquivalentTo(ProductKindDescriptor other)
        {
            return string.Equals(Id, other.Id, System.StringComparison.OrdinalIgnoreCase)
                   && CompatibilityDrugType == other.CompatibilityDrugType;
        }
    }
}
