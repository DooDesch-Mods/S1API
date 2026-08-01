#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using System.Collections.Generic;

namespace S1API.Deliveries
{
    /// <summary>
    /// An immutable snapshot of delivery order details.
    /// </summary>
    public sealed class DeliveryReceipt
    {
        private readonly IReadOnlyList<DeliveryItem> items;

        internal DeliveryReceipt(S1Delivery.DeliveryReceipt receipt)
            : this(
                receipt?.DeliveryID,
                receipt?.StoreName,
                receipt?.DestinationCode,
                receipt?.LoadingDockIndex ?? -1,
                receipt?.Items)
        {
        }

        internal DeliveryReceipt(S1Delivery.DeliveryInstance delivery)
            : this(
                delivery?.DeliveryID,
                delivery?.StoreName,
                delivery?.DestinationCode,
                delivery?.LoadingDockIndex ?? -1,
                delivery?.Items)
        {
        }

        private DeliveryReceipt(
            string? id,
            string? storeName,
            string? destinationCode,
            int loadingDockIndex,
            IEnumerable<S1DevUtilities.StringIntPair>? nativeItems)
        {
            Id = id ?? string.Empty;
            StoreName = storeName ?? string.Empty;
            DestinationCode = destinationCode ?? string.Empty;
            LoadingDockIndex = loadingDockIndex;
            items = DeliveryItem.Snapshot(nativeItems);
        }

        /// <summary>
        /// Gets the delivery's unique identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the stable shop name that accepted the order.
        /// </summary>
        public string StoreName { get; }

        /// <summary>
        /// Gets the property code of the delivery destination.
        /// </summary>
        public string DestinationCode { get; }

        /// <summary>
        /// Gets the zero-based loading-dock index selected for the order.
        /// </summary>
        public int LoadingDockIndex { get; }

        /// <summary>
        /// Gets an immutable snapshot of the ordered items.
        /// </summary>
        public IReadOnlyList<DeliveryItem> Items => items;
    }
}
