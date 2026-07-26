#if IL2CPPMELON
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items;
using StorableItemDefinition = S1API.Items.Storable.StorableItemDefinition;

namespace S1API.Entities.Supplier
{
    /// <summary>
    /// Builds the native framework data used by a custom supplier NPC.
    /// </summary>
    public sealed class SupplierDataBuilder
    {
        internal sealed class SupplierConfigData
        {
            public float MinimumDeaddropOrderLimit { get; set; } = 100f;
            public float MaximumDeaddropOrderLimit { get; set; } = 500f;
            public List<S1ItemFramework.StorableItemDefinition> DeliveryItems { get; } =
                new List<S1ItemFramework.StorableItemDefinition>();
            public List<string> DeliveryItemIds { get; } =
                new List<string>();
            public string SupplierRecommendMessage { get; set; } =
                "My friend <NAME> can hook you up with <PRODUCT>. I've passed your number on to them.";
            public string SupplierUnlockHint { get; set; } =
                "You can now order <PRODUCT> from <NAME>. <PRODUCT> can be used to <PURPOSE>.";

            internal IReadOnlyList<S1ItemFramework.StorableItemDefinition>
                ResolveDeliveryItems()
            {
                for (int idIndex = 0;
                     idIndex < DeliveryItemIds.Count;
                     idIndex++)
                {
                    ItemDefinition? item =
                        ItemManager.GetDefinition(DeliveryItemIds[idIndex]);
                    if (item == null ||
                        !CrossType.Is(
                            item.S1ItemDefinition,
                            out S1ItemFramework.StorableItemDefinition storableItem))
                    {
                        continue;
                    }

                    bool alreadyResolved = false;
                    for (int itemIndex = 0;
                         itemIndex < DeliveryItems.Count;
                         itemIndex++)
                    {
                        if (string.Equals(
                                DeliveryItems[itemIndex]?.ID,
                                storableItem.ID,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyResolved = true;
                            break;
                        }
                    }

                    if (!alreadyResolved)
                        DeliveryItems.Add(storableItem);
                }

                return DeliveryItems;
            }
        }

        private readonly SupplierConfigData data = new SupplierConfigData();

        internal SupplierDataBuilder()
        {
        }

        /// <summary>
        /// Sets the minimum and maximum dead-drop order values.
        /// </summary>
        /// <param name="minimum">Order limit at the lowest relationship level.</param>
        /// <param name="maximum">Order limit at the highest relationship level.</param>
        /// <returns>The current builder for chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when either value is not finite, the minimum is negative, the maximum is not positive,
        /// or the maximum is below the minimum.
        /// </exception>
        public SupplierDataBuilder WithOrderLimits(float minimum, float maximum)
        {
            if (float.IsNaN(minimum) || float.IsInfinity(minimum))
                throw new ArgumentOutOfRangeException(nameof(minimum), "The minimum order limit must be finite.");
            if (float.IsNaN(maximum) || float.IsInfinity(maximum))
                throw new ArgumentOutOfRangeException(nameof(maximum), "The maximum order limit must be finite.");
            if (minimum < 0f)
                throw new ArgumentOutOfRangeException(nameof(minimum), "The minimum order limit cannot be negative.");
            if (maximum <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximum), "The maximum order limit must be greater than zero.");
            if (maximum < minimum)
                throw new ArgumentOutOfRangeException(nameof(maximum), "The maximum order limit cannot be below the minimum.");

            data.MinimumDeaddropOrderLimit = minimum;
            data.MaximumDeaddropOrderLimit = maximum;
            return this;
        }

        /// <summary>
        /// Adds an item to this supplier's dead-drop and meeting-shop listings.
        /// </summary>
        /// <param name="item">A public wrapper around a native storable item definition.</param>
        /// <returns>The current builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the wrapped item is not storable.</exception>
        public SupplierDataBuilder WithDeliveryItem(ItemDefinition item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!CrossType.Is(item.S1ItemDefinition, out S1ItemFramework.StorableItemDefinition storableItem))
                throw new ArgumentException($"Item '{item.ID}' is not storable and cannot be sold by a supplier.", nameof(item));

            for (int i = 0; i < data.DeliveryItems.Count; i++)
            {
                if (string.Equals(
                        data.DeliveryItems[i]?.ID,
                        storableItem.ID,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return this;
                }
            }

            data.DeliveryItems.Add(storableItem);
            AddDeliveryItemId(storableItem.ID);
            return this;
        }

        /// <summary>
        /// Adds a storable item to this supplier's dead-drop and meeting-shop listings.
        /// </summary>
        /// <param name="item">The storable item wrapper to add.</param>
        /// <returns>The current builder for chaining.</returns>
        public SupplierDataBuilder WithDeliveryItem(StorableItemDefinition item) =>
            WithDeliveryItem((ItemDefinition)item);

        /// <summary>
        /// Declares an item ID for this supplier.
        /// </summary>
        /// <param name="itemId">
        /// The stable item ID. The item may be registered after NPC prefab discovery;
        /// S1API resolves it when supplier runtime data is materialized.
        /// </param>
        /// <returns>The current builder for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when the ID is empty.</exception>
        public SupplierDataBuilder WithDeliveryItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("The item ID cannot be empty.", nameof(itemId));

            AddDeliveryItemId(itemId.Trim());
            return this;
        }

        /// <summary>
        /// Adds multiple item wrappers to this supplier's listings.
        /// </summary>
        /// <param name="items">The item wrappers to add.</param>
        /// <returns>The current builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
        public SupplierDataBuilder WithDeliveryItems(IEnumerable<ItemDefinition> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (ItemDefinition item in items)
                WithDeliveryItem(item);
            return this;
        }

        /// <summary>
        /// Sets the message used when another NPC recommends this supplier.
        /// </summary>
        /// <param name="message">Recommendation text. Native placeholders such as &lt;NAME&gt; and &lt;PRODUCT&gt; are supported. Null clears the text.</param>
        /// <returns>The current builder for chaining.</returns>
        public SupplierDataBuilder WithRecommendationMessage(string? message)
        {
            data.SupplierRecommendMessage = message ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Sets the hint shown when this supplier and product are unlocked.
        /// </summary>
        /// <param name="hint">Unlock hint text. Native placeholders are supported. Null clears the text.</param>
        /// <returns>The current builder for chaining.</returns>
        public SupplierDataBuilder WithUnlockHint(string? hint)
        {
            data.SupplierUnlockHint = hint ?? string.Empty;
            return this;
        }

        internal SupplierConfigData BuildInternal() => data;

        private void AddDeliveryItemId(string itemId)
        {
            for (int index = 0;
                 index < data.DeliveryItemIds.Count;
                 index++)
            {
                if (string.Equals(
                        data.DeliveryItemIds[index],
                        itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            data.DeliveryItemIds.Add(itemId);
        }
    }
}
