#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Shop = Il2CppScheduleOne.UI.Shop;
using S1Storage = Il2CppScheduleOne.Storage;
using ShopListingList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.UI.Shop.ShopListing>;
using ShopListingUiList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.UI.Shop.ListingUI>;
#elif MONOMELON
using S1Economy = ScheduleOne.Economy;
using S1Items = ScheduleOne.ItemFramework;
using S1Shop = ScheduleOne.UI.Shop;
using S1Storage = ScheduleOne.Storage;
using ShopListingList = System.Collections.Generic.List<ScheduleOne.UI.Shop.ShopListing>;
using ShopListingUiList = System.Collections.Generic.List<ScheduleOne.UI.Shop.ListingUI>;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Entities.Supplier;
using S1API.Internal.Utils;
using S1API.Shops;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace S1API.Internal.Entities.Suppliers
{
    /// <summary>
    /// Creates and owns the native ShopInterface used by each custom supplier.
    /// </summary>
    internal static class SupplierShopRuntime
    {
        private static readonly Dictionary<int, GameObject> OwnedShops = new Dictionary<int, GameObject>();

        internal static S1Shop.ShopInterface Ensure(
            S1Economy.Supplier supplier,
            string stableId,
            SupplierDataBuilder.SupplierConfigData? config)
        {
            if (SupplierRuntimeIds.IsOwnedShop(supplier.Shop))
                return supplier.Shop;

            S1Economy.Supplier? donor = Resources
                .FindObjectsOfTypeAll<S1Economy.Supplier>()
                .FirstOrDefault(candidate => candidate != null
                                             && candidate != supplier
                                             && candidate.Shop != null
                                             && !SupplierRuntimeIds.IsOwnedShop(candidate.Shop));
            if (donor?.Shop == null)
                throw new InvalidOperationException("No initialized vanilla supplier shop is loaded in the current scene.");

            S1Shop.ShopInterface shop =
                InactiveObjectCloner.CloneComponent(donor.Shop, donor.Shop.transform.parent);
            string displayName = string.IsNullOrWhiteSpace(supplier.FullName)
                ? stableId
                : supplier.FullName;
            string shopName = SupplierRuntimeIds.GetShopName(stableId);

            shop.gameObject.name = $"S1API_SupplierShop_{SupplierRuntimeIds.Sanitize(stableId)}";
            // ShopName is persisted in DeliveryInstance.StoreName and must never depend on display text.
            shop.ShopName = shopName;
            shop.ShopCode = SupplierRuntimeIds.GetShopCode(stableId);
            shop.ShopDescription = $"Supplier shop for {displayName}.";
            shop.Listings = BuildListings(supplier, config);
            shop.onOrderCompleted = new UnityEvent();
            shop.onOrderCompletedWithSpend = null;
            shop.LoadingBayDetector = null;
            // Delivery vehicles are separate scene objects in the base game. Never retain the donor reference.
            shop.DeliveryVehicle = null;
            ClearRuntimeUi(shop);
#if IL2CPPMELON
            shop.DeliveryBays = new Il2CppReferenceArray<S1Storage.StorageEntity>(0);
#else
            shop.DeliveryBays = Array.Empty<S1Storage.StorageEntity>();
#endif
            OwnedShops[supplier.GetInstanceID()] = shop.gameObject;
            shop.gameObject.SetActive(true);
            if (shop.StoreNameLabel != null)
                shop.StoreNameLabel.text = displayName;

            supplier.Shop = shop;
            ShopManager.InvalidateCache();
            return shop;
        }

        internal static void ClearDonorReference(S1Economy.Supplier supplier)
        {
            if (!SupplierRuntimeIds.IsOwnedShop(supplier.Shop))
                supplier.Shop = null;
        }

        internal static void CleanupSupplier(S1Economy.Supplier supplier)
        {
            if (supplier == null)
                return;

            int supplierKey = supplier.GetInstanceID();
            if (!OwnedShops.TryGetValue(supplierKey, out GameObject? shopObject))
                return;

            if (shopObject != null)
                Object.Destroy(shopObject);
            OwnedShops.Remove(supplierKey);
            ShopManager.InvalidateCache();
        }

        internal static void CleanupForSceneChange()
        {
            foreach (GameObject shop in OwnedShops.Values.ToArray())
            {
                if (shop != null)
                    Object.Destroy(shop);
            }

            OwnedShops.Clear();
            ShopManager.InvalidateCache();
        }

        private static ShopListingList BuildListings(
            S1Economy.Supplier supplier,
            SupplierDataBuilder.SupplierConfigData? config)
        {
            var listings = new ShopListingList();
            if (config != null)
            {
                IReadOnlyList<S1Items.StorableItemDefinition> deliveryItems =
                    config.ResolveDeliveryItems();
                for (int i = 0; i < deliveryItems.Count; i++)
                    listings.Add(CreateListing(deliveryItems[i]));
                return listings;
            }

            var supplierData = supplier.SupplierData;
            if (supplierData?.DeliveryShopListings == null)
                return listings;

            for (int i = 0; i < supplierData.DeliveryShopListings.Length; i++)
            {
                S1Items.StorableItemDefinition? item = supplierData.DeliveryShopListings[i]?.Item;
                if (item != null)
                    listings.Add(CreateListing(item));
            }

            return listings;
        }

        private static S1Shop.ShopListing CreateListing(S1Items.StorableItemDefinition item)
        {
            return new S1Shop.ShopListing
            {
                name = $"{item.Name} (${item.BasePurchasePrice})",
                Item = item,
                LimitedStock = false,
                DefaultStock = -1,
                CanBeDelivered = true
            };
        }

        private static void ClearRuntimeUi(S1Shop.ShopInterface shop)
        {
            if (shop.ListingContainer != null)
            {
                for (int i = shop.ListingContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = shop.ListingContainer.GetChild(i);
                    if (shop.ListingUIPrefab != null && child.gameObject == shop.ListingUIPrefab.gameObject)
                        continue;
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            ReflectionUtils.TrySetFieldOrProperty(shop, "listingUI", new ShopListingUiList());
            object? listingPanel = ReflectionUtils.TryGetFieldOrProperty(shop, "listingPanel");
            ReflectionUtils.GetMethod(
                    listingPanel?.GetType(),
                    "ClearAllSelectables",
                    System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance)
                ?.Invoke(listingPanel, null);
        }
    }
}
