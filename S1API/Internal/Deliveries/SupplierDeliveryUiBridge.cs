#if IL2CPPMELON
using Il2CppInterop.Runtime;
using S1Delivery = Il2CppScheduleOne.UI.Phone.Delivery;
using S1Shop = Il2CppScheduleOne.UI.Shop;
using DeliveryElementList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.UI.Phone.Delivery.DeliveryApp.DeliveryShopElement>;
using DeliveryShopList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.UI.Phone.Delivery.DeliveryShop>;
using DeliveryListingEntryList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.UI.Phone.Delivery.ListingEntry>;
#elif MONOMELON
using S1Delivery = ScheduleOne.UI.Phone.Delivery;
using S1Shop = ScheduleOne.UI.Shop;
using DeliveryElementList = System.Collections.Generic.List<ScheduleOne.UI.Phone.Delivery.DeliveryApp.DeliveryShopElement>;
using DeliveryShopList = System.Collections.Generic.List<ScheduleOne.UI.Phone.Delivery.DeliveryShop>;
using DeliveryListingEntryList = System.Collections.Generic.List<ScheduleOne.UI.Phone.Delivery.ListingEntry>;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Entities.Suppliers;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace S1API.Internal.Deliveries
{
    /// <summary>
    /// Integrates generated supplier shops into the native delivery phone app.
    /// </summary>
    internal static class SupplierDeliveryUiBridge
    {
        private static readonly Log Logger = new Log("SupplierDeliveryUiBridge");
        private static readonly Dictionary<int, DeliveryRegistration> Registrations =
            new Dictionary<int, DeliveryRegistration>();
        private static readonly Dictionary<int, PendingAvailability> Pending =
            new Dictionary<int, PendingAvailability>();

        internal static bool EnsureEntry(
            S1Delivery.DeliveryApp app,
            S1Shop.ShopInterface shop,
            bool available)
        {
            if (app == null || shop == null || !SupplierRuntimeIds.IsOwnedShop(shop))
                return false;

            int shopKey = shop.GetInstanceID();
            if (shop.DeliveryVehicle == null)
            {
                // DeliveryInstance.SetStatus(Arrived) assumes this reference exists. Keep the UI hidden
                // until the peer has bound the matching networked delivery vehicle.
                Pending[shopKey] = new PendingAvailability(app, shop, available);
            if (Registrations.TryGetValue(shopKey, out DeliveryRegistration? existingRegistration))
                    existingRegistration.Element.Button?.gameObject.SetActive(false);
                return true;
            }

            Pending.Remove(shopKey);
            DeliveryElementList? elements =
                ReflectionUtils.TryGetFieldOrProperty(app, "_shopElements") as DeliveryElementList;
            if (elements == null)
                return false;

            for (int i = 0; i < elements.Count; i++)
            {
                var existing = elements[i];
                if (existing?.Shop == null)
                    continue;
                if (existing.Shop.MatchingShop == shop
                    || string.Equals(existing.Shop.MatchingShopInterfaceName, shop.ShopName, StringComparison.Ordinal))
                {
                    existing.Shop.AvailableByDefault = available;
                    existing.Button?.gameObject.SetActive(available);
                    Registrations[shopKey] = new DeliveryRegistration(app, existing);
                    SupplierDeliveryStatusDisplayRecovery.OnShopReady(app, shop.ShopName);
                    return true;
                }
            }

            var donor = FindDonor(elements);
            if (donor?.Shop == null || donor.Button == null)
            {
                Logger.Warning($"No delivery UI donor was available for custom supplier shop '{shop.ShopName}'.");
                return false;
            }

            S1Delivery.DeliveryShop deliveryShop =
                InactiveObjectCloner.CloneComponent(donor.Shop, donor.Shop.transform.parent);
            Button button = InactiveObjectCloner.CloneComponent(donor.Button, donor.Button.transform.parent);
            ResetShopState(deliveryShop);
            button.onClick.RemoveAllListeners();
            deliveryShop.gameObject.name = $"S1API_{SupplierRuntimeIds.Sanitize(shop.ShopName)}_DeliveryShop";
            deliveryShop.MatchingShopInterfaceName = shop.ShopName;
            deliveryShop.AvailableByDefault = available;
            button.gameObject.name = $"S1API_{SupplierRuntimeIds.Sanitize(shop.ShopName)}_DeliveryButton";
            Text? label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = shop.StoreNameLabel != null ? shop.StoreNameLabel.text : shop.ShopName;

            var element = new S1Delivery.DeliveryApp.DeliveryShopElement
            {
                Shop = deliveryShop,
                Button = button
            };
            elements.Add(element);

            DeliveryShopList? deliveryShops =
                ReflectionUtils.TryGetFieldOrProperty(app, "deliveryShops") as DeliveryShopList;
            deliveryShops?.Add(deliveryShop);

            Registrations[shopKey] = new DeliveryRegistration(app, element);
            bool started = ReflectionUtils.TryGetFieldOrProperty(app, "started") is bool value && value;
            if (started)
                InitializeElement(app, element);

            button.gameObject.SetActive(available);
            SupplierDeliveryStatusDisplayRecovery.OnShopReady(app, shop.ShopName);
            return true;
        }

        internal static void OnVehicleBound(S1Shop.ShopInterface shop)
        {
            if (shop == null)
                return;

            int shopKey = shop.GetInstanceID();
            if (!Pending.TryGetValue(shopKey, out PendingAvailability? pending))
                return;

            Pending.Remove(shopKey);
            if (!EnsureEntry(pending.App, pending.Shop, pending.Available))
            {
                Logger.Warning($"Delivery UI could not be provisioned for custom supplier shop '{shop.ShopName}'.");
            }
        }

        internal static void CleanupShop(S1Shop.ShopInterface? shop)
        {
            if (shop == null)
                return;

            int shopKey = shop.GetInstanceID();
            Pending.Remove(shopKey);
            if (!Registrations.TryGetValue(shopKey, out DeliveryRegistration? registration))
                return;

            DestroyRegistration(registration, removeFromApp: true);
            Registrations.Remove(shopKey);
        }

        internal static void CleanupForSceneChange()
        {
            foreach (DeliveryRegistration registration in Registrations.Values.ToArray())
                DestroyRegistration(registration, removeFromApp: false);

            Registrations.Clear();
            Pending.Clear();
        }

        private static S1Delivery.DeliveryApp.DeliveryShopElement? FindDonor(DeliveryElementList elements)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element?.Shop != null && element.Button != null)
                    return element;
            }

            return null;
        }

        private static void InitializeElement(
            S1Delivery.DeliveryApp app,
            S1Delivery.DeliveryApp.DeliveryShopElement element)
        {
            element.Button.onClick.AddListener((UnityAction)(() => app.OpenShop(element.Shop)));
#if IL2CPPMELON
            var onSelect = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<S1Delivery.DeliveryShop>>(
                new Action<S1Delivery.DeliveryShop>(app.CloseShop));
            element.Shop.OnSelect = Il2CppSystem.Delegate
                .Combine(element.Shop.OnSelect, onSelect)
                .Cast<Il2CppSystem.Action<S1Delivery.DeliveryShop>>();
#else
            element.Shop.OnSelect += app.CloseShop;
#endif
            element.Shop.Initialize();

            object? screen = ReflectionUtils.TryGetFieldOrProperty(app, "_deliveryScreen");
            if (screen == null)
                return;

            var addPanel = ReflectionUtils.GetMethod(
                screen.GetType(),
                "AddPanel",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance);
            if (addPanel == null)
                return;

            foreach (var panel in element.Shop.Panels)
                addPanel.Invoke(screen, new object[] { panel });
        }

        private static void ResetShopState(S1Delivery.DeliveryShop shop)
        {
            if (shop.ListingContainer != null)
            {
                for (int i = shop.ListingContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = shop.ListingContainer.GetChild(i);
                    if (shop.ListingEntryPrefab != null
                        && child.gameObject == shop.ListingEntryPrefab.gameObject)
                    {
                        continue;
                    }

                    Object.DestroyImmediate(child.gameObject);
                }
            }

            ReflectionUtils.TrySetFieldOrProperty(shop, "listingEntries", new DeliveryListingEntryList());
            ReflectionUtils.TrySetFieldOrProperty(shop, "MatchingShop", null);
            ReflectionUtils.TrySetFieldOrProperty(shop, "IsOpen", false);
            ReflectionUtils.TrySetFieldOrProperty(shop, "destinationProperty", null);
            ReflectionUtils.TrySetFieldOrProperty(shop, "loadingDockIndex", 0);
            shop.OnSelect = null;
            shop.BackButton?.onClick.RemoveAllListeners();
            shop.OrderButton?.onClick.RemoveAllListeners();
            shop.DestinationDropdown?.onValueChanged.RemoveAllListeners();
            shop.LoadingDockDropdown?.onValueChanged.RemoveAllListeners();

            object? entriesPanel = ReflectionUtils.TryGetFieldOrProperty(shop, "_entriesPanel");
            ReflectionUtils.GetMethod(
                    entriesPanel?.GetType(),
                    "ClearAllSelectables",
                    System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance)
                ?.Invoke(entriesPanel, null);
        }

        private static void DestroyRegistration(DeliveryRegistration registration, bool removeFromApp)
        {
            if (removeFromApp && registration.App != null)
            {
                DeliveryElementList? elements =
                    ReflectionUtils.TryGetFieldOrProperty(registration.App, "_shopElements") as DeliveryElementList;
                elements?.Remove(registration.Element);

                DeliveryShopList? deliveryShops =
                    ReflectionUtils.TryGetFieldOrProperty(registration.App, "deliveryShops") as DeliveryShopList;
                if (registration.Element?.Shop != null)
                    deliveryShops?.Remove(registration.Element.Shop);
            }

            if (registration.Element?.Button != null)
                Object.Destroy(registration.Element.Button.gameObject);
            if (registration.Element?.Shop != null)
                Object.Destroy(registration.Element.Shop.gameObject);
        }

        private sealed class DeliveryRegistration
        {
            internal DeliveryRegistration(
                S1Delivery.DeliveryApp app,
                S1Delivery.DeliveryApp.DeliveryShopElement element)
            {
                App = app;
                Element = element;
            }

            internal S1Delivery.DeliveryApp App { get; }
            internal S1Delivery.DeliveryApp.DeliveryShopElement Element { get; }
        }

        private sealed class PendingAvailability
        {
            internal PendingAvailability(S1Delivery.DeliveryApp app, S1Shop.ShopInterface shop, bool available)
            {
                App = app;
                Shop = shop;
                Available = available;
            }

            internal S1Delivery.DeliveryApp App { get; }
            internal S1Shop.ShopInterface Shop { get; }
            internal bool Available { get; }
        }

    }
}
