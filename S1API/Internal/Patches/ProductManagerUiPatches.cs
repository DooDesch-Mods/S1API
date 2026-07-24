#if IL2CPPMELON
using S1Product = Il2CppScheduleOne.Product;
using S1ProductManagerUi = Il2CppScheduleOne.UI.Phone.ProductManagerApp;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
using S1ProductManagerUi = ScheduleOne.UI.Phone.ProductManagerApp;
#endif

using System;
using HarmonyLib;
using S1API.Internal.Products;
using S1API.Logging;
using S1API.Products;

namespace S1API.Internal.Patches
{
    [HarmonyPatch]
    internal static class ProductManagerUiPatches
    {
        private static readonly Log Logger = new Log("ProductManagerUiPatches");

        [HarmonyPatch(
            typeof(S1Product.PropertyUtility),
            "Awake")]
        [HarmonyPostfix]
        private static void PropertyUtilityAwakePostfix()
        {
            ProductKindMetadataRegistrationRegistry.ApplyAll(
                "PropertyUtility.Awake");
        }

        [HarmonyPatch(
            typeof(S1ProductManagerUi.ProductManagerApp),
            "Start")]
        [HarmonyPrefix]
        private static void ProductManagerStartPrefix(
            S1ProductManagerUi.ProductManagerApp __instance)
        {
            SafeApply(__instance, "ProductManagerApp.Start");
        }

        [HarmonyPatch(
            typeof(S1ProductManagerUi.ProductManagerApp),
            nameof(S1ProductManagerUi.ProductManagerApp.CreateEntry))]
        [HarmonyPrefix]
        private static bool CreateEntryPrefix(
            S1ProductManagerUi.ProductManagerApp __instance,
            S1Product.ProductDefinition definition)
        {
            try
            {
                return ProductManagerUiRuntime.BeforeCreateEntry(
                    __instance,
                    definition);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Product Manager entry preparation failed for "
                    + $"'{definition?.ID}': {exception}");
                return true;
            }
        }

        [HarmonyPatch(
            typeof(S1ProductManagerUi.ProductManagerApp),
            nameof(S1ProductManagerUi.ProductManagerApp.CreateEntry))]
        [HarmonyPostfix]
        private static void CreateEntryPostfix(
            S1ProductManagerUi.ProductManagerApp __instance,
            S1Product.ProductDefinition definition)
        {
            try
            {
                ProductManagerUiRuntime.AfterCreateEntry(__instance, definition);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Product Manager entry routing failed for "
                    + $"'{definition?.ID}': {exception}");
            }
        }

        [HarmonyPatch(
            typeof(S1ProductManagerUi.ProductManagerApp),
            "CreateFavouriteEntry")]
        [HarmonyPrefix]
        private static bool CreateFavouriteEntryPrefix(
            S1Product.ProductDefinition definition)
        {
            try
            {
                return ProductManagerUiRuntime.AllowFavouriteEntry(definition);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Product Manager favourite preparation failed for "
                    + $"'{definition?.ID}': {exception}");
                return true;
            }
        }

        [HarmonyPatch(
            typeof(S1ProductManagerUi.ProductManagerApp),
            "RemoveFavouriteEntry")]
        [HarmonyPrefix]
        private static bool RemoveFavouriteEntryPrefix(
            S1Product.ProductDefinition definition)
        {
            try
            {
                return ProductManagerUiRuntime.AllowFavouriteEntry(definition);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Product Manager favourite removal failed for "
                    + $"'{definition?.ID}': {exception}");
                return true;
            }
        }

        [HarmonyPatch(
            typeof(S1ProductManagerUi.ProductManagerApp),
            nameof(S1ProductManagerUi.ProductManagerApp.SetOpen))]
        [HarmonyPostfix]
        private static void SetOpenPostfix(
            S1ProductManagerUi.ProductManagerApp __instance)
        {
            SafeApply(__instance, "ProductManagerApp.SetOpen");
        }

        private static void SafeApply(
            S1ProductManagerUi.ProductManagerApp app,
            string phase)
        {
            try
            {
                ProductManagerUiRuntime.Apply(
                    app,
                    new System.Collections.Generic.List<ProductKindMetadata>(
                        ProductKindMetadataRegistry.All));
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Product Manager metadata reconciliation failed during "
                    + $"{phase}: {exception}");
            }
        }
    }
}
