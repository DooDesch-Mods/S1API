namespace S1API.Products
{
    /// <summary>
    /// Represents a logical product kind without depending on a native product definition or enum identity.
    /// </summary>
    /// <remarks>
    /// Product kinds are immutable and remain registered for the lifetime of the current process.
    /// </remarks>
    public sealed class ProductKind
    {
        /// <summary>
        /// Gets the stable, namespaced identifier for this product kind.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the optional vanilla drug type that best represents this product kind for compatibility.
        /// </summary>
        /// <remarks>
        /// This value is metadata only. It does not define the product kind's identity or create native product support.
        /// </remarks>
        public DrugType? CompatibilityDrugType { get; }

        internal ProductKind(string id, DrugType? compatibilityDrugType)
        {
            Id = id;
            CompatibilityDrugType = compatibilityDrugType;
        }

        internal bool IsEquivalentTo(ProductKind other)
        {
            return string.Equals(Id, other.Id, System.StringComparison.OrdinalIgnoreCase)
                   && CompatibilityDrugType == other.CompatibilityDrugType;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Id;
        }
    }
}
