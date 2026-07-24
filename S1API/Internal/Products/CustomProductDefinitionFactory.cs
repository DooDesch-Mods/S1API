#if (IL2CPPMELON)
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using NativeDrugTypeList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Product.DrugTypeContainer>;
using NativeEffect = Il2CppScheduleOne.Effects.Effect;
using NativeEffectList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Effects.Effect>;
using S1CoreItemFramework = Il2CppScheduleOne.Core.Items.Framework;
using S1Packaging = Il2CppScheduleOne.Product.Packaging;
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using NativeDrugTypeList = System.Collections.Generic.List<ScheduleOne.Product.DrugTypeContainer>;
using NativeEffect = ScheduleOne.Effects.Effect;
using NativeEffectList = System.Collections.Generic.List<ScheduleOne.Effects.Effect>;
using S1CoreItemFramework = ScheduleOne.Core.Items.Framework;
using S1Packaging = ScheduleOne.Product.Packaging;
using S1Product = ScheduleOne.Product;
#endif

using System.Collections.Generic;
using S1API.Items;
using S1API.Products;
using UnityEngine;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Creates the native generic definition without entering the native mix-product creator.
    /// </summary>
    internal static class CustomProductDefinitionFactory
    {
        internal static S1Product.ProductDefinition Create(
            string productId,
            string productName,
            string description,
            float productPrice,
            LegalStatus legalStatus,
            float baseAddictiveness,
            int playerEffectDurationSeconds,
            int npcEffectDurationSeconds,
            DrugType compatibilityDrugType,
            IReadOnlyList<NativeEffect> properties,
            IReadOnlyList<S1Packaging.PackagingDefinition> validPackaging,
            S1Product.ProductDefinition representationTemplate)
        {
            S1Product.ProductDefinition definition =
                ScriptableObject.CreateInstance<S1Product.ProductDefinition>();

            definition.ID = productId;
            definition.Name = productName;
            definition.Description = description;
            definition.name = productName;
            definition.Category = S1CoreItemFramework.EItemCategory.Product;
            definition.StackLimit = representationTemplate.StackLimit;
            definition.AvailableInDemo = representationTemplate.AvailableInDemo;
            definition.UsableInFilters = representationTemplate.UsableInFilters;
            definition.Icon = representationTemplate.Icon;
            definition.legalStatus =
                (S1CoreItemFramework.ELegalStatus)legalStatus;
            definition.PickpocketDifficultyMultiplier =
                representationTemplate.PickpocketDifficultyMultiplier;
            definition.CombatUtility = representationTemplate.CombatUtility;

            definition.BasePurchasePrice = productPrice;
            definition.ResellMultiplier = representationTemplate.ResellMultiplier;
            definition.ShopCategories = representationTemplate.ShopCategories;
            definition.RequiresLevelToPurchase =
                representationTemplate.RequiresLevelToPurchase;
            definition.RequiredRank = representationTemplate.RequiredRank;
            definition.StoredItem = representationTemplate.StoredItem;
            definition.StationItem = null;
            definition.Equippable = representationTemplate.Equippable;
            definition.EquipMode = representationTemplate.EquipMode;
            definition.CustomItemUI = representationTemplate.CustomItemUI;
            definition.CustomInfoContent = representationTemplate.CustomInfoContent;

            definition.Properties = new NativeEffectList();
            for (int i = 0; i < properties.Count; i++)
                definition.Properties.Add(properties[i]);

            definition.DrugTypes = new NativeDrugTypeList();
            definition.DrugTypes.Add(
                new S1Product.DrugTypeContainer
                {
                    DrugType = compatibilityDrugType.ToInternal()
                });

            definition.LawIntensityChange = representationTemplate.LawIntensityChange;
            definition.BasePrice = productPrice;
            definition.MarketValue = productPrice;
            definition.FunctionalProduct = representationTemplate.FunctionalProduct;
            definition.NPCEffectDuration = npcEffectDurationSeconds;
            definition.PlayerEffectDuration = playerEffectDurationSeconds;
            definition.BaseAddictiveness = baseAddictiveness;
            definition.ValidPackaging = CreatePackagingArray(validPackaging);
            definition.ConsumeAnimation = representationTemplate.ConsumeAnimation;
            return definition;
        }

        internal static void Destroy(S1Product.ProductDefinition definition)
        {
            if (!ReferenceEquals(definition, null))
                Object.Destroy(definition);
        }

#if (IL2CPPMELON)
        private static Il2CppReferenceArray<S1Packaging.PackagingDefinition>
            CreatePackagingArray(
                IReadOnlyList<S1Packaging.PackagingDefinition> validPackaging)
        {
            var array =
                new Il2CppReferenceArray<S1Packaging.PackagingDefinition>(
                    validPackaging.Count);
            for (int i = 0; i < validPackaging.Count; i++)
                array[i] = validPackaging[i];

            return array;
        }
#else
        private static S1Packaging.PackagingDefinition[] CreatePackagingArray(
            IReadOnlyList<S1Packaging.PackagingDefinition> validPackaging)
        {
            var array =
                new S1Packaging.PackagingDefinition[validPackaging.Count];
            for (int i = 0; i < validPackaging.Count; i++)
                array[i] = validPackaging[i];

            return array;
        }
#endif
    }
}
