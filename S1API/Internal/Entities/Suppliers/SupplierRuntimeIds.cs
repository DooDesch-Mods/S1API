#if IL2CPPMELON
using S1Economy = Il2CppScheduleOne.Economy;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Shop = Il2CppScheduleOne.UI.Shop;
#elif MONOMELON
using S1Economy = ScheduleOne.Economy;
using S1NPCs = ScheduleOne.NPCs;
using S1Shop = ScheduleOne.UI.Shop;
#endif

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace S1API.Internal.Entities.Suppliers
{
    /// <summary>
    /// Owns every persisted or cross-system identifier generated for a custom supplier.
    /// </summary>
    internal static class SupplierRuntimeIds
    {
        internal const string ShopNamePrefix = "s1api_supplier_";
        internal const string DeliveryVehiclePrefabPrefix = "S1API_SupplierDeliveryVehicle_";

        internal static string ResolveStableId(GameObject supplierObject, string? explicitId = null)
        {
            string? stableId = explicitId;
            NPCPrefabIdentity? identity = supplierObject != null
                ? supplierObject.GetComponent<NPCPrefabIdentity>()
                : null;

            if (string.IsNullOrWhiteSpace(stableId))
                stableId = identity?.Id;

            if (string.IsNullOrWhiteSpace(stableId) && supplierObject != null)
            {
                S1Economy.Supplier? supplier = supplierObject.GetComponent<S1Economy.Supplier>();
                stableId = supplier?.ID;
                if (string.IsNullOrWhiteSpace(stableId))
                    stableId = supplierObject.GetComponent<S1NPCs.NPC>()?.ID;
            }

            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new InvalidOperationException(
                    "A custom supplier requires an ID configured with NPCPrefabBuilder.WithIdentity().");
            }

            const string cloneSuffix = "(Clone)";
            stableId = stableId.Trim();
            if (stableId.EndsWith(cloneSuffix, StringComparison.Ordinal))
                stableId = stableId.Substring(0, stableId.Length - cloneSuffix.Length).Trim();

            return stableId.ToLowerInvariant();
        }

        internal static string GetShopName(string stableId)
        {
            return ShopNamePrefix + GetStableToken(stableId);
        }

        internal static string GetShopCode(string stableId)
        {
            return GetShopName(stableId);
        }

        internal static string GetDeliveryVehiclePrefabName(string stableId)
        {
            return DeliveryVehiclePrefabPrefix + GetStableToken(stableId);
        }

        internal static Guid GetStashGuid(string stableId)
        {
            return CreateStableGuid($"S1API.SupplierStash:v1:{Normalize(stableId)}");
        }

        internal static Guid GetDeliveryVehicleGuid(string stableId)
        {
            return CreateStableGuid($"S1API.SupplierDeliveryVehicle:v1:{Normalize(stableId)}");
        }

        internal static bool IsOwnedShop(S1Shop.ShopInterface? shop)
        {
            return shop != null && IsOwnedShopName(shop.ShopName);
        }

        internal static bool IsOwnedShopName(string? shopName)
        {
            return !string.IsNullOrWhiteSpace(shopName)
                   && shopName.StartsWith(ShopNamePrefix, StringComparison.OrdinalIgnoreCase);
        }

        internal static string Sanitize(string value)
        {
            char[] chars = Normalize(value)
                .Select(character => char.IsLetterOrDigit(character) ? character : '_')
                .ToArray();
            string result = new string(chars).Trim('_');
            return string.IsNullOrEmpty(result) ? "supplier" : result;
        }

        private static string GetStableToken(string stableId)
        {
            string normalized = Normalize(stableId);
            string slug = Sanitize(normalized);
            if (slug.Length > 48)
                slug = slug.Substring(0, 48);

            byte[] hash;
            using (SHA256 algorithm = SHA256.Create())
                hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(normalized));

            var suffix = new StringBuilder(8);
            for (int i = 0; i < 4; i++)
                suffix.Append(hash[i].ToString("x2"));
            return $"{slug}_{suffix}";
        }

        private static Guid CreateStableGuid(string value)
        {
            byte[] hash;
            using (SHA256 algorithm = SHA256.Create())
                hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));

            var guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, guidBytes.Length);
            guidBytes[6] = (byte)((guidBytes[6] & 0x0f) | 0x50);
            guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);
            return new Guid(guidBytes);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A stable supplier ID is required.", nameof(value));
            return value.Trim().ToLowerInvariant();
        }
    }
}
