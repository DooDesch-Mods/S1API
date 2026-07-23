using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Builds and registers immutable logical product-kind descriptors.
    /// </summary>
    public sealed class ProductKindDescriptorBuilder
    {
        private readonly string _id;
        private DrugType? _compatibilityDrugType;

        /// <summary>
        /// Creates a builder for a stable, namespaced logical product kind.
        /// </summary>
        /// <param name="id">
        /// A stable identifier in <c>&lt;namespace&gt;:&lt;name&gt;</c> form, such as <c>examplemod:mdma</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="id"/> is empty or does not use the supported namespaced format.
        /// </exception>
        public ProductKindDescriptorBuilder(string id)
        {
            _id = ProductKindId.Normalize(id, nameof(id));
        }

        /// <summary>
        /// Sets optional compatibility metadata for systems that still reason about vanilla drug types.
        /// </summary>
        /// <param name="drugType">The API-safe vanilla drug type that best represents this product kind.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="drugType"/> is not a defined <see cref="DrugType"/> value.
        /// </exception>
        public ProductKindDescriptorBuilder WithCompatibilityDrugType(DrugType drugType)
        {
            if (!Enum.IsDefined(typeof(DrugType), drugType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(drugType),
                    drugType,
                    "Compatibility drug type must be a defined S1API.Products.DrugType value.");
            }

            _compatibilityDrugType = drugType;
            return this;
        }

        /// <summary>
        /// Builds and registers the configured descriptor.
        /// </summary>
        /// <returns>
        /// The newly registered descriptor, or the existing descriptor when an equivalent registration already exists.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the identifier is already registered with different compatibility metadata.
        /// </exception>
        public ProductKindDescriptor Build()
        {
            var descriptor = new ProductKindDescriptor(_id, _compatibilityDrugType);
            return ProductKindRegistry.Register(descriptor);
        }
    }
}
