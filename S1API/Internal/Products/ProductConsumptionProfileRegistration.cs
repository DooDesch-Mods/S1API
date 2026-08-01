using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Associates a consumption profile with a stable resolution key.</summary>
    internal sealed class ProductConsumptionProfileRegistration
    {
        internal ProductConsumptionProfileRegistration(
            string key,
            ProductConsumptionProfile profile)
        {
            Key = key;
            Profile = profile;
        }

        internal string Key { get; }

        internal ProductConsumptionProfile Profile { get; }
    }
}
