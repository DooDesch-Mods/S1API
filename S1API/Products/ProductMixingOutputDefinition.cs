using System;

namespace S1API.Products
{
    /// <summary>
    /// Describes the logical identity and price of a generated custom mixed product.
    /// </summary>
    public sealed class ProductMixingOutputDefinition
    {
        /// <summary>Creates an output definition.</summary>
        /// <param name="name">The non-empty output name.</param>
        /// <param name="productKind">The logical kind of the generated output.</param>
        /// <param name="price">The finite generated-output price.</param>
        public ProductMixingOutputDefinition(string name, ProductKind productKind, float price)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Output name cannot be empty or whitespace.", nameof(name));
            if (productKind == null)
                throw new ArgumentNullException(nameof(productKind));
            if (float.IsNaN(price) || float.IsInfinity(price) || price < 1f || price > 999f)
                throw new ArgumentOutOfRangeException(nameof(price), "Output price must be finite and within the native 1 through 999 range.");

            Name = name.Trim();
            ProductKind = productKind;
            Price = price;
        }

        /// <summary>Gets the generated display name.</summary>
        public string Name { get; }
        /// <summary>Gets the explicit output logical kind.</summary>
        public ProductKind ProductKind { get; }
        /// <summary>Gets the generated price.</summary>
        public float Price { get; }
    }
}
