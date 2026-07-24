#if IL2CPPMELON
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Describes one process-owned product definition and the native
    /// ProductManager metadata that must be restored for each gameplay scene.
    /// </summary>
    internal sealed class CustomProductDefinitionRegistration
    {
        internal CustomProductDefinitionRegistration(
            string ownerId,
            string productId,
            string productName,
            float initialPrice,
            S1Product.ProductDefinition definition)
            : this(
                ownerId,
                productId,
                productName,
                initialPrice,
                definition,
                null)
        {
        }

        internal CustomProductDefinitionRegistration(
            string ownerId,
            string productId,
            string productName,
            float initialPrice,
            S1Product.ProductDefinition definition,
            CustomProductDefinitionMetadata? metadata)
        {
            OwnerId = ownerId;
            ProductId = productId;
            ProductName = productName;
            InitialPrice = initialPrice;
            Definition = definition;
            Metadata = metadata;
        }

        internal string OwnerId { get; }

        internal string ProductId { get; }

        internal string ProductName { get; }

        internal float InitialPrice { get; }

        internal S1Product.ProductDefinition Definition { get; }

        internal CustomProductDefinitionMetadata? Metadata { get; }
    }
}
