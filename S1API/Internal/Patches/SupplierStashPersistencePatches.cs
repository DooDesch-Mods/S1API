#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1Storage = Il2CppScheduleOne.Storage;
#elif MONOMELON || MONOBEPINEX || IL2CPPBEPINEX
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1Storage = ScheduleOne.Storage;
#endif

using HarmonyLib;
using S1API.Internal.Entities.Suppliers;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Bridges native world-storage loading to delayed custom supplier stash creation.
    /// </summary>
    [HarmonyPatch]
    internal static class SupplierStashPersistencePatches
    {
        [HarmonyPatch(typeof(S1Loaders.StorageLoader), nameof(S1Loaders.StorageLoader.Load))]
        [HarmonyPrefix]
        private static void StorageLoaderLoadPrefix(S1Loaders.StorageLoader __instance, string __0)
        {
            SupplierStashPersistenceRuntime.CaptureFromLoader(__instance, __0);
        }

        [HarmonyPatch(
            typeof(S1Storage.WorldStorageEntity),
            nameof(S1Storage.WorldStorageEntity.Load),
            new[] { typeof(S1Datas.WorldStorageEntityData) })]
        [HarmonyPostfix]
        private static void WorldStorageEntityLoadPostfix(S1Datas.WorldStorageEntityData __0)
        {
            SupplierStashPersistenceRuntime.NotifyLoadSucceeded(__0);
        }
    }
}
