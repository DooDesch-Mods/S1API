#if IL2CPPMELON
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace S1API.Deliveries
{
    /// <summary>
    /// An immutable item-and-quantity entry from a delivery or receipt.
    /// </summary>
    public sealed class DeliveryItem
    {
        private static readonly IReadOnlyList<DeliveryItem> EmptyItems =
            new ReadOnlyCollection<DeliveryItem>(Array.Empty<DeliveryItem>());

        internal DeliveryItem(string itemId, int quantity)
        {
            ItemId = itemId ?? string.Empty;
            Quantity = quantity;
        }

        /// <summary>
        /// Gets the registry ID of the ordered item.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// Gets the ordered quantity.
        /// </summary>
        public int Quantity { get; }

        internal static IReadOnlyList<DeliveryItem> Snapshot(
            IEnumerable<S1DevUtilities.StringIntPair>? nativeItems)
        {
            if (nativeItems == null)
                return EmptyItems;

            var snapshot = new List<DeliveryItem>();
            foreach (S1DevUtilities.StringIntPair nativeItem in nativeItems)
            {
                if (nativeItem != null)
                    snapshot.Add(new DeliveryItem(nativeItem.String, nativeItem.Int));
            }

            return snapshot.Count == 0
                ? EmptyItems
                : new ReadOnlyCollection<DeliveryItem>(snapshot);
        }
    }
}
