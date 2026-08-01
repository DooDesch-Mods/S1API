using System;

namespace S1API.Products
{
    /// <summary>
    /// Supplies deterministic, runtime-independent input to a custom mixing-output factory.
    /// </summary>
    public sealed class ProductMixingOutput
    {
        internal ProductMixingOutput(string outputId, string mixName, string sourceProductId, ProductKind sourceKind, float sourcePrice)
        {
            OutputId = outputId;
            MixName = mixName;
            SourceProductId = sourceProductId;
            SourceKind = sourceKind;
            SourcePrice = sourcePrice;
        }

        /// <summary>Gets the stable ID allocated by the native mixing flow.</summary>
        public string OutputId { get; }
        /// <summary>Gets the player-selected display name for the output.</summary>
        public string MixName { get; }
        /// <summary>Gets the source custom-product ID.</summary>
        public string SourceProductId { get; }
        /// <summary>Gets the source logical product kind.</summary>
        public ProductKind SourceKind { get; }
        /// <summary>Gets the source product's current base price.</summary>
        public float SourcePrice { get; }
    }
}
