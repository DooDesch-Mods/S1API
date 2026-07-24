#if IL2CPPMELON
using S1Equipping = Il2CppScheduleOne.Equipping;
using S1Product = Il2CppScheduleOne.Product;
using S1Station = Il2CppScheduleOne.StationFramework;
using S1Storage = Il2CppScheduleOne.Storage;
#elif MONOMELON
using S1Equipping = ScheduleOne.Equipping;
using S1Product = ScheduleOne.Product;
using S1Station = ScheduleOne.StationFramework;
using S1Storage = ScheduleOne.Storage;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Retains immutable template references and generated presentation objects.
    /// </summary>
    internal sealed class CustomProductPresentationState
    {
        internal CustomProductPresentationState(
            S1Product.ProductDefinition definition,
            CustomProductDefinitionMetadata? metadata)
        {
            BaselineIcon = definition.Icon;
            BaselineStoredItem = definition.StoredItem;
            BaselineEquippable = definition.Equippable;
            BaselineStationItem = definition.StationItem;
            BaselineFunctionalProduct = definition.FunctionalProduct;
            BaselineConsumeAnimation = definition.ConsumeAnimation;
            TemplateStationItem = metadata?.RepresentationTemplate?.StationItem;
        }

        internal Sprite? BaselineIcon { get; }

        internal S1Storage.StoredItem? BaselineStoredItem { get; }

        internal S1Equipping.Equippable? BaselineEquippable { get; }

        internal S1Station.StationItem? BaselineStationItem { get; }

        internal S1Station.StationItem? TemplateStationItem { get; }

        internal S1Product.FunctionalProduct? BaselineFunctionalProduct { get; }

        internal S1Product.ProductConsumeAnimation? BaselineConsumeAnimation { get; }

        internal ProductPresentationProfileRegistration? AppliedRegistration { get; set; }

        internal bool IsGeneratedIconPending { get; set; }

        internal bool IsGeneratedIconQueued { get; set; }

        internal List<Object> GeneratedObjects { get; set; } = new List<Object>();
    }
}
