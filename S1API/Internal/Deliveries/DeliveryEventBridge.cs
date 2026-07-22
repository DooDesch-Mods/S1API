#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
#elif MONOMELON || MONOBEPINEX || IL2CPPBEPINEX
using S1Delivery = ScheduleOne.Delivery;
#endif

using System;
using System.Collections.Generic;
using S1API.Deliveries;

namespace S1API.Internal.Deliveries
{
    /// <summary>
    /// Translates native delivery lifecycle calls into the public delivery registry events.
    /// </summary>
    internal static class DeliveryEventBridge
    {
        private static readonly HashSet<string> SeenDeliveryIds =
            new HashSet<string>(StringComparer.Ordinal);

        internal static DeliveryStatus BeforeStatusChange(S1Delivery.DeliveryInstance delivery)
        {
            return delivery == null
                ? DeliveryStatus.InTransit
                : (DeliveryStatus)(int)delivery.Status;
        }

        internal static void AfterStatusChange(
            S1Delivery.DeliveryInstance delivery,
            DeliveryStatus previousStatus)
        {
            if (delivery == null)
                return;

            string id = delivery.DeliveryID ?? string.Empty;
            if (!string.IsNullOrEmpty(id) && SeenDeliveryIds.Add(id))
                DeliveryRegistry.NotifyCreated(delivery);

            DeliveryStatus currentStatus = (DeliveryStatus)(int)delivery.Status;
            DeliveryRegistry.NotifyStatusChanged(delivery, previousStatus, currentStatus);
            if (currentStatus != DeliveryStatus.Completed)
                return;

            DeliveryRegistry.NotifyCompleted(delivery);
        }

        internal static void Reset()
        {
            SeenDeliveryIds.Clear();
        }
    }
}
