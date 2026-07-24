using System;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Resolves a selected native mixer-map strategy.</summary>
    internal static class ProductMixingMapContract
    {
        internal static DrugType GetNativeDrugType(ProductMixingMap mixerMap)
        {
            switch (mixerMap)
            {
                case ProductMixingMap.Marijuana:
                    return DrugType.Marijuana;
                case ProductMixingMap.Methamphetamine:
                    return DrugType.Methamphetamine;
                case ProductMixingMap.Cocaine:
                    return DrugType.Cocaine;
                case ProductMixingMap.Shrooms:
                    return DrugType.Shrooms;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mixerMap));
            }
        }
    }
}
