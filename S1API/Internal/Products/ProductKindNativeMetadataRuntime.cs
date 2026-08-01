#if IL2CPPMELON
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Product = ScheduleOne.Product;
#endif

using System.Collections.Generic;
using S1API.Products;

namespace S1API.Internal.Products
{
    internal static class ProductKindNativeMetadataRuntime
    {
        internal static void Apply(IReadOnlyList<ProductKindMetadata> metadata)
        {
            S1Product.PropertyUtility utility =
                S1DevUtilities.Singleton<S1Product.PropertyUtility>.Instance;
            if (utility == null || utility.DrugTypeDatas == null)
                return;

            for (int i = 0; i < metadata.Count; i++)
            {
                ProductKindMetadata item = metadata[i];
                if (!item.ProductKind.CompatibilityDrugType.HasValue)
                    continue;

                DrugType compatibilityType =
                    item.ProductKind.CompatibilityDrugType.Value;
                if (!ProductKindNativeMetadataTypes.RequiresFallback(
                        compatibilityType))
                {
                    continue;
                }

                S1Product.EDrugType nativeType =
                    compatibilityType.ToInternal();
                if (Contains(utility, nativeType))
                    continue;

                utility.DrugTypeDatas.Add(new S1Product.PropertyUtility.DrugTypeData
                {
                    DrugType = nativeType,
                    Name = item.DisplayName,
                    Color = item.Color
                });
            }
        }

        private static bool Contains(
            S1Product.PropertyUtility utility,
            S1Product.EDrugType drugType)
        {
            for (int i = 0; i < utility.DrugTypeDatas.Count; i++)
            {
                S1Product.PropertyUtility.DrugTypeData data =
                    utility.DrugTypeDatas[i];
                if (data != null && data.DrugType == drugType)
                    return true;
            }

            return false;
        }
    }
}
