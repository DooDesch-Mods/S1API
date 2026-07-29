using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Records one owned presentation-profile registration.
    /// </summary>
    internal sealed class ProductPresentationProfileRegistration
    {
        internal ProductPresentationProfileRegistration(
            string ownerId,
            string key,
            ProductPresentationProfile profile)
        {
            OwnerId = ownerId;
            Key = key;
            Profile = profile;
        }

        internal string OwnerId { get; }

        internal string Key { get; }

        internal ProductPresentationProfile Profile { get; }
    }
}
