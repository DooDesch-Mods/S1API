#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Shop = Il2CppScheduleOne.UI.Shop;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Shop = ScheduleOne.UI.Shop;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Entities.Suppliers;
using S1API.Logging;

namespace S1API.Internal.Deliveries
{
    /// <summary>
    /// Defers arrived-delivery activation and saved vehicle hydration until custom supplier infrastructure exists.
    /// </summary>
    internal static class SupplierDeliveryRecovery
    {
        private static readonly Log Logger = new Log("SupplierDeliveryRecovery");
        private static readonly Dictionary<S1Delivery.DeliveryInstance, int> PendingArrivals =
            new Dictionary<S1Delivery.DeliveryInstance, int>();
        private static readonly Dictionary<string, PendingVehicleLoad> PendingVehicleLoads =
            new Dictionary<string, PendingVehicleLoad>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<S1Delivery.DeliveryInstance> ReplayingArrivals =
            new HashSet<S1Delivery.DeliveryInstance>();

        internal static bool TryDeferStatus(
            S1Delivery.DeliveryInstance delivery,
            S1Delivery.EDeliveryStatus status)
        {
            if (delivery == null || !SupplierRuntimeIds.IsOwnedShopName(delivery.StoreName))
                return false;

            if (status != S1Delivery.EDeliveryStatus.Arrived)
            {
                PendingArrivals.Remove(delivery);
                return false;
            }

            if (ReplayingArrivals.Contains(delivery))
                return false;

            S1Shop.ShopInterface? shop = FindShop(delivery.StoreName);
            if (shop?.DeliveryVehicle != null)
                return false;

            if (!PendingArrivals.ContainsKey(delivery))
                PendingArrivals[delivery] = delivery.TimeUntilArrival;

            // Prevent DeliveryManager.OnTimePass from entering its Arrived branch before ActiveVehicle exists.
            delivery.Status = S1Delivery.EDeliveryStatus.Waiting;
            delivery.TimeUntilArrival = int.MaxValue;
            return true;
        }

        internal static bool TryDeferVehicleLoad(S1Datas.VehicleData data, string path)
        {
            if (data == null || !SupplierDeliveryVehicleRuntime.IsKnownGuid(data.GUID))
                return false;

            S1Delivery.DeliveryVehicle? existing =
                SupplierDeliveryVehicleRuntime.FindByGuid(data.GUID);
            if (existing?.Vehicle != null)
                return false;

            PendingVehicleLoads[data.GUID] = new PendingVehicleLoad(data, path ?? string.Empty);
            return true;
        }

        internal static void OnVehicleBound(
            S1Shop.ShopInterface shop,
            S1Delivery.DeliveryVehicle deliveryVehicle)
        {
            if (shop == null || deliveryVehicle == null)
                return;

            ApplyPendingVehicleLoad(deliveryVehicle);
            ReplayArrivals(shop.ShopName);
        }

        internal static void Reset()
        {
            PendingArrivals.Clear();
            PendingVehicleLoads.Clear();
            ReplayingArrivals.Clear();
        }

        private static void ApplyPendingVehicleLoad(S1Delivery.DeliveryVehicle deliveryVehicle)
        {
            if (string.IsNullOrWhiteSpace(deliveryVehicle.GUID)
                || !PendingVehicleLoads.TryGetValue(deliveryVehicle.GUID, out PendingVehicleLoad pending))
            {
                return;
            }

            try
            {
                deliveryVehicle.Vehicle.Load(pending.Data, pending.Path);
                PendingVehicleLoads.Remove(deliveryVehicle.GUID);
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"Failed to restore custom supplier delivery vehicle '{deliveryVehicle.GUID}': {ex.Message}");
            }
        }

        private static void ReplayArrivals(string shopName)
        {
            var pending = PendingArrivals
                .Where(entry => string.Equals(entry.Key?.StoreName, shopName, StringComparison.Ordinal))
                .ToArray();

            foreach (var entry in pending)
            {
                S1Delivery.DeliveryInstance delivery = entry.Key;
                PendingArrivals.Remove(delivery);
                if (delivery == null)
                    continue;

                try
                {
                    delivery.TimeUntilArrival = entry.Value;
                    ReplayingArrivals.Add(delivery);
                    delivery.SetStatus(S1Delivery.EDeliveryStatus.Arrived);
                }
                catch (Exception ex)
                {
                    Logger.Warning(
                        $"Failed to replay arrived delivery '{delivery.DeliveryID}' after vehicle binding: {ex.Message}");
                }
                finally
                {
                    ReplayingArrivals.Remove(delivery);
                }
            }
        }

        private static S1Shop.ShopInterface? FindShop(string shopName)
        {
            if (S1Shop.ShopInterface.AllShops == null)
                return null;

            foreach (S1Shop.ShopInterface shop in S1Shop.ShopInterface.AllShops)
            {
                if (shop != null && string.Equals(shop.ShopName, shopName, StringComparison.Ordinal))
                    return shop;
            }

            return null;
        }

        private sealed class PendingVehicleLoad
        {
            internal PendingVehicleLoad(S1Datas.VehicleData data, string path)
            {
                Data = data;
                Path = path;
            }

            internal S1Datas.VehicleData Data { get; }
            internal string Path { get; }
        }
    }
}
