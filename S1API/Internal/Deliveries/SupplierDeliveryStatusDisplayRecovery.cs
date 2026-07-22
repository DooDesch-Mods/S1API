#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DeliveryUi = Il2CppScheduleOne.UI.Phone.Delivery;
using DeliveryShopList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.UI.Phone.Delivery.DeliveryShop>;
#elif MONOMELON || MONOBEPINEX || IL2CPPBEPINEX
using S1Delivery = ScheduleOne.Delivery;
using S1DeliveryUi = ScheduleOne.UI.Phone.Delivery;
using DeliveryShopList = System.Collections.Generic.List<ScheduleOne.UI.Phone.Delivery.DeliveryShop>;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Entities.Suppliers;
using S1API.Internal.Utils;
using S1API.Logging;

namespace S1API.Internal.Deliveries
{
    /// <summary>
    /// Defers native delivery status UI hydration until a custom supplier shop can be resolved.
    /// </summary>
    internal static class SupplierDeliveryStatusDisplayRecovery
    {
        private static readonly Log Logger = new Log("SupplierDeliveryStatusDisplayRecovery");
        private static readonly Dictionary<int, Dictionary<string, PendingStatusDisplay>> Pending =
            new Dictionary<int, Dictionary<string, PendingStatusDisplay>>();

        internal static bool TryDefer(
            S1DeliveryUi.DeliveryApp app,
            S1Delivery.DeliveryInstance delivery)
        {
            if (app == null
                || delivery == null
                || !SupplierRuntimeIds.IsOwnedShopName(delivery.StoreName))
            {
                return false;
            }

            string deliveryKey = GetDeliveryKey(delivery);
            if (HasReadyShop(app, delivery.StoreName))
            {
                Remove(app.GetInstanceID(), deliveryKey);
                return false;
            }

            int appKey = app.GetInstanceID();
            if (!Pending.TryGetValue(
                    appKey,
                    out Dictionary<string, PendingStatusDisplay>? pendingForApp)
                || pendingForApp == null)
            {
                pendingForApp = new Dictionary<string, PendingStatusDisplay>(StringComparer.Ordinal);
                Pending.Add(appKey, pendingForApp);
            }

            pendingForApp[deliveryKey] = new PendingStatusDisplay(app, delivery);
            return true;
        }

        internal static void OnShopReady(S1DeliveryUi.DeliveryApp app, string shopName)
        {
            Replay(app, shopName);
            RefreshPastDeliveries(app);
        }

        internal static void CleanupShop(string? shopName)
        {
            if (string.IsNullOrEmpty(shopName))
                return;

            foreach (int appKey in Pending.Keys.ToArray())
            {
                Dictionary<string, PendingStatusDisplay> pendingForApp = Pending[appKey];
                foreach (string deliveryKey in pendingForApp
                             .Where(pair => string.Equals(
                                 pair.Value.Delivery.StoreName,
                                 shopName,
                                 StringComparison.Ordinal))
                             .Select(pair => pair.Key)
                             .ToArray())
                {
                    pendingForApp.Remove(deliveryKey);
                }

                if (pendingForApp.Count == 0)
                    Pending.Remove(appKey);
            }
        }

        internal static void Reset()
        {
            Pending.Clear();
        }

        private static bool HasReadyShop(S1DeliveryUi.DeliveryApp app, string shopName)
        {
            DeliveryShopList? deliveryShops =
                ReflectionUtils.TryGetFieldOrProperty(app, "deliveryShops") as DeliveryShopList;
            if (deliveryShops == null)
                return false;

            for (int i = 0; i < deliveryShops.Count; i++)
            {
                S1DeliveryUi.DeliveryShop? deliveryShop = deliveryShops[i];
                if (deliveryShop != null
                    && deliveryShop.MatchingShop != null
                    && string.Equals(
                        deliveryShop.MatchingShopInterfaceName,
                        shopName,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Replay(S1DeliveryUi.DeliveryApp app, string shopName)
        {
            if (!HasReadyShop(app, shopName)
                || !Pending.TryGetValue(
                    app.GetInstanceID(),
                    out Dictionary<string, PendingStatusDisplay>? pendingForApp)
                || pendingForApp == null)
            {
                return;
            }

            PendingStatusDisplay[] ready = pendingForApp.Values
                .Where(pending => string.Equals(
                    pending.Delivery.StoreName,
                    shopName,
                    StringComparison.Ordinal))
                .ToArray();
            if (ready.Length == 0)
                return;

            var createStatusDisplay = ReflectionUtils.GetMethod(
                typeof(S1DeliveryUi.DeliveryApp),
                "CreateDeliveryStatusDisplay",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance);
            if (createStatusDisplay == null)
            {
                Logger.Warning("Could not find DeliveryApp.CreateDeliveryStatusDisplay to replay saved deliveries.");
                return;
            }

            foreach (PendingStatusDisplay pending in ready)
            {
                Remove(app.GetInstanceID(), GetDeliveryKey(pending.Delivery));
                if (pending.Delivery.Status == S1Delivery.EDeliveryStatus.Completed)
                    continue;

                try
                {
                    createStatusDisplay.Invoke(pending.App, new object[] { pending.Delivery });
                }
                catch (Exception ex)
                {
                    Logger.Warning(
                        $"Could not replay delivery status display '{pending.Delivery.DeliveryID}': {ex.Message}");
                }
            }
        }

        private static void RefreshPastDeliveries(S1DeliveryUi.DeliveryApp app)
        {
            if (ReflectionUtils.TryGetFieldOrProperty(app, "_pastDeliveries") == null)
                return;

            try
            {
                ReflectionUtils.GetMethod(
                        typeof(S1DeliveryUi.DeliveryApp),
                        "UpdatePastDeliveries",
                        System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(app, null);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not refresh delivery history after registering a supplier shop: {ex.Message}");
            }
        }

        private static void Remove(int appKey, string deliveryKey)
        {
            if (!Pending.TryGetValue(
                    appKey,
                    out Dictionary<string, PendingStatusDisplay>? pendingForApp)
                || pendingForApp == null)
            {
                return;
            }

            pendingForApp.Remove(deliveryKey);
            if (pendingForApp.Count == 0)
                Pending.Remove(appKey);
        }

        private static string GetDeliveryKey(S1Delivery.DeliveryInstance delivery)
        {
            return string.IsNullOrEmpty(delivery.DeliveryID)
                ? $"{delivery.StoreName}:{delivery.GetHashCode()}"
                : delivery.DeliveryID;
        }

        private sealed class PendingStatusDisplay
        {
            internal PendingStatusDisplay(
                S1DeliveryUi.DeliveryApp app,
                S1Delivery.DeliveryInstance delivery)
            {
                App = app;
                Delivery = delivery;
            }

            internal S1DeliveryUi.DeliveryApp App { get; }
            internal S1Delivery.DeliveryInstance Delivery { get; }
        }
    }
}
