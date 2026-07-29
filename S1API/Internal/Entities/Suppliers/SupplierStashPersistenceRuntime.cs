#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1Storage = Il2CppScheduleOne.Storage;
#elif MONOMELON
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1Storage = ScheduleOne.Storage;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Entities.Suppliers
{
    /// <summary>
    /// Retains custom supplier stash save data until its runtime storage entity is registered.
    /// </summary>
    internal static class SupplierStashPersistenceRuntime
    {
        private static readonly Log Logger = new Log("SupplierStashPersistenceRuntime");
        private static readonly HashSet<string> RegisteredStashGuids =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, S1Datas.WorldStorageEntityData> PendingData =
            new Dictionary<string, S1Datas.WorldStorageEntityData>(StringComparer.OrdinalIgnoreCase);

        internal static void Register(string stableId)
        {
            RegisteredStashGuids.Add(GetStashGuid(stableId));
        }

        internal static void CaptureFromLoader(S1Loaders.StorageLoader loader, string mainPath)
        {
            if (loader == null || string.IsNullOrWhiteSpace(mainPath))
                return;

            try
            {
                if (Directory.Exists(mainPath))
                {
                    foreach (string path in Directory.GetFiles(mainPath))
                    {
                        if (!loader.TryLoadFile(path, out string contents, false))
                            continue;

                        TryCaptureSingle(contents);
                    }
                }

                if (!loader.TryLoadFile(mainPath, out string collectionContents))
                    return;

                S1Datas.WorldStorageEntitiesData? collection =
                    JsonUtility.FromJson<S1Datas.WorldStorageEntitiesData>(collectionContents);
                if (collection?.Entities == null)
                    return;

                foreach (S1Datas.WorldStorageEntityData data in collection.Entities)
                    Capture(data);
            }
            catch (Exception ex)
            {
                // The native loader remains authoritative and will report malformed or inaccessible data.
                Logger.Warning(
                    $"Could not inspect supplier stash save data before native storage loading: {ex.Message}");
            }
        }

        internal static bool TryReplay(S1Storage.WorldStorageEntity storage, string stableId)
        {
            if (storage == null)
                return false;

            string guid = GetStashGuid(stableId);
            RegisteredStashGuids.Add(guid);
            if (!PendingData.TryGetValue(guid, out S1Datas.WorldStorageEntityData? data) || data == null)
                return false;

            try
            {
                storage.Load(data);
                PendingData.Remove(guid);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not restore custom supplier stash '{guid}': {ex.Message}");
                return false;
            }
        }

        internal static void NotifyLoadSucceeded(S1Datas.WorldStorageEntityData data)
        {
            if (data == null || !TryNormalizeGuid(data.GUID, out string guid))
                return;

            PendingData.Remove(guid);
        }

        internal static void ClearPending()
        {
            PendingData.Clear();
        }

        private static void Capture(S1Datas.WorldStorageEntityData? data)
        {
            if (data == null
                || !TryNormalizeGuid(data.GUID, out string guid)
                || !RegisteredStashGuids.Contains(guid))
            {
                return;
            }

            if (TryResolveStorage(guid))
            {
                PendingData.Remove(guid);
                return;
            }

            PendingData[guid] = data;
        }

        private static void TryCaptureSingle(string contents)
        {
            try
            {
                Capture(JsonUtility.FromJson<S1Datas.WorldStorageEntityData>(contents));
            }
            catch
            {
                // Native StorageLoader reports malformed files while continuing through the directory.
            }
        }

        private static string GetStashGuid(string stableId)
        {
            return SupplierRuntimeIds.GetStashGuid(stableId).ToString("D");
        }

        private static bool TryNormalizeGuid(string? value, out string guid)
        {
            if (Guid.TryParse(value, out Guid parsed))
            {
                guid = parsed.ToString("D");
                return true;
            }

            guid = string.Empty;
            return false;
        }

        private static bool TryResolveStorage(string guid)
        {
            try
            {
#if IL2CPPMELON
                return Il2Cpp.GUIDManager.GetObject<S1Storage.WorldStorageEntity>(new Il2CppSystem.Guid(guid)) != null;
#else
                return global::GUIDManager.GetObject<S1Storage.WorldStorageEntity>(new Guid(guid)) != null;
#endif
            }
            catch
            {
                return false;
            }
        }
    }
}
