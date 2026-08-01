using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>
    /// Identifies native enum values that have no serialized DrugTypeData row.
    /// Update this list only after verifying both the native enum and serialized data.
    /// </summary>
    internal static class ProductKindNativeMetadataTypes
    {
        internal static bool RequiresFallback(DrugType drugType)
        {
            return drugType == DrugType.MDMA || drugType == DrugType.Heroin;
        }
    }
}
