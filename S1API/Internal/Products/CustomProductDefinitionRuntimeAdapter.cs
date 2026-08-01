#if IL2CPPMELON
using NativeProductPrices = Il2CppSystem.Collections.Generic.Dictionary<Il2CppScheduleOne.Product.ProductDefinition, float>;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Product = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
#elif MONOMELON
using NativeProductPrices = System.Collections.Generic.Dictionary<ScheduleOne.Product.ProductDefinition, float>;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Product = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using System.Reflection;
using UnityEngine;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Applies one tracked definition to the active native registries.
    /// </summary>
    internal interface ICustomProductDefinitionRuntimeAdapter
    {
        bool Apply(CustomProductDefinitionRegistration registration);
    }

    /// <summary>
    /// INTERNAL: Centralizes native Registry and ProductManager collection access,
    /// including the runtime-specific ProductPrices representation.
    /// </summary>
    internal sealed class CustomProductDefinitionRuntimeAdapter :
        ICustomProductDefinitionRuntimeAdapter
    {
        internal static readonly CustomProductDefinitionRuntimeAdapter Instance =
            new CustomProductDefinitionRuntimeAdapter();

#if MONOMELON
        private static readonly FieldInfo? ProductPricesField =
            typeof(S1Product.ProductManager).GetField(
                "ProductPrices",
                BindingFlags.Instance | BindingFlags.NonPublic);
#endif

        private CustomProductDefinitionRuntimeAdapter()
        {
        }

        public bool Apply(CustomProductDefinitionRegistration registration)
        {
            EnsureDefinitionIdentity(registration);

            var registry = S1Registry.Instance;
            if (registry == null)
                return false;

            EnsureItemRegistry(registry, registration);

            var productManager = S1Product.ProductManager.Instance;
            if (productManager == null)
                return false;

            EnsureAllProducts(productManager, registration);
            EnsureProductName(productManager, registration);
            EnsureInitialPrice(productManager, registration);
            return true;
        }

        private static void EnsureDefinitionIdentity(
            CustomProductDefinitionRegistration registration)
        {
            if (string.Equals(
                    registration.Definition.ID,
                    registration.ProductId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Cannot register custom product '{registration.ProductId}' owned by " +
                $"'{registration.OwnerId}': the native definition ID is " +
                $"'{registration.Definition.ID ?? "<null>"}'. Stable registration IDs " +
                "must match the native definition ID case-insensitively.");
        }

        private static void EnsureItemRegistry(
            S1Registry registry,
            CustomProductDefinitionRegistration registration)
        {
            S1ItemFramework.ItemDefinition? existing =
                S1Registry.GetItem(registration.ProductId);

            if (existing == null)
            {
                registry.AddToRegistry(registration.Definition);
                return;
            }

            if (!AreSameDefinition(existing, registration.Definition))
            {
                throw CreateRuntimeConflict(
                    registration,
                    "ScheduleOne.Registry",
                    existing);
            }
        }

        private static void EnsureAllProducts(
            S1Product.ProductManager productManager,
            CustomProductDefinitionRegistration registration)
        {
            var allProducts = productManager.AllProducts;
            if (allProducts == null)
            {
                throw new InvalidOperationException(
                    "ProductManager.AllProducts is unavailable. " +
                    "The game update may have changed product registration internals.");
            }

            for (int index = 0; index < allProducts.Count; index++)
            {
                S1Product.ProductDefinition? existing = allProducts[index];
                if (existing == null ||
                    !string.Equals(
                        existing.ID,
                        registration.ProductId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!AreSameDefinition(existing, registration.Definition))
                {
                    throw CreateRuntimeConflict(
                        registration,
                        "ProductManager.AllProducts",
                        existing);
                }

                return;
            }

            allProducts.Add(registration.Definition);
        }

        private static void EnsureProductName(
            S1Product.ProductManager productManager,
            CustomProductDefinitionRegistration registration)
        {
            var productNames = productManager.ProductNames;
            if (productNames == null)
            {
                throw new InvalidOperationException(
                    "ProductManager.ProductNames is unavailable. " +
                    "The game update may have changed product registration internals.");
            }

            if (!productNames.Contains(registration.ProductName))
                productNames.Add(registration.ProductName);
        }

        private static void EnsureInitialPrice(
            S1Product.ProductManager productManager,
            CustomProductDefinitionRegistration registration)
        {
            NativeProductPrices productPrices = GetProductPrices(productManager);
            if (!productPrices.ContainsKey(registration.Definition))
            {
                productPrices.Add(
                    registration.Definition,
                    registration.InitialPrice);
            }
        }

        private static NativeProductPrices GetProductPrices(
            S1Product.ProductManager productManager)
        {
#if IL2CPPMELON
            NativeProductPrices? productPrices = productManager.ProductPrices;
#elif MONOMELON
            if (ProductPricesField == null)
            {
                throw new MissingFieldException(
                    typeof(S1Product.ProductManager).FullName,
                    "ProductPrices");
            }

            NativeProductPrices? productPrices =
                ProductPricesField.GetValue(productManager) as NativeProductPrices;
#endif

            return productPrices ?? throw new InvalidOperationException(
                "ProductManager.ProductPrices is unavailable. " +
                "The game update may have changed product registration internals.");
        }

        private static InvalidOperationException CreateRuntimeConflict(
            CustomProductDefinitionRegistration registration,
            string collectionName,
            S1ItemFramework.ItemDefinition existing)
        {
            string existingType = existing.GetType().FullName ?? existing.GetType().Name;
            return new InvalidOperationException(
                $"Cannot restore custom product '{registration.ProductId}' owned by " +
                $"'{registration.OwnerId}': {collectionName} already contains a different " +
                $"definition of type '{existingType}'. Check for a native ID collision or " +
                "another mod registering the same case-insensitive product ID.");
        }

        private static bool AreSameDefinition(
            S1ItemFramework.ItemDefinition left,
            S1ItemFramework.ItemDefinition right)
        {
            return ReferenceEquals(left, right) ||
                   (left is UnityEngine.Object leftObject &&
                    right is UnityEngine.Object rightObject &&
                    leftObject == rightObject);
        }
    }
}
