using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>Registers process-lifetime providers for custom-product save descriptors.</summary>
    public static class CustomProductSaveProviderRegistry
    {
        /// <summary>Registers a provider, or returns the existing equivalent provider.</summary>
        public static ICustomProductSaveProvider Register(ICustomProductSaveProvider provider)
        {
            return CustomProductSavePersistence.RegisterProvider(provider);
        }
    }
}
