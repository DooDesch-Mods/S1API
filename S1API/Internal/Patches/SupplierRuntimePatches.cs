#if IL2CPPMELON
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif MONOMELON
using S1Datas = ScheduleOne.Persistence.Datas;
using S1Economy = ScheduleOne.Economy;
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Entities;
using S1API.Internal.Utils;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Adapts native supplier lifecycle calls to the separated supplier runtime services.
    /// </summary>
    [HarmonyPatch]
    internal static class SupplierRuntimePatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("SupplierRuntimePatches");

        [HarmonyPatch(typeof(S1Economy.Supplier), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void SupplierAwakePrefix(S1Economy.Supplier __instance)
        {
            if (!NPCPatches.IsS1ApiCustomNpcComponent(__instance))
                return;

            try
            {
                __instance.GetComponent<NPCPrefabIdentity>()?.ApplyCriticalIdentityBeforeAwake(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to apply custom supplier identity before Awake: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(S1Economy.Supplier), "Start")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SupplierStartPrefix(S1Economy.Supplier __instance)
        {
            if (!NPCPatches.IsS1ApiCustomNpcComponent(__instance))
                return true;

            try
            {
                NPC? wrapper = NPCPatches.FindWrapperForS1Npc(__instance);
                var defaults = wrapper != null
                    ? NPC.BuildSupplierDefaultsForType(wrapper.GetType())
                    : null;
                if (SupplierRuntimeCoordinator.EnsureReady(__instance, __instance.ID, defaults))
                    return true;

                Logger.Error(
                    $"Custom supplier '{__instance.ID}' is missing required runtime infrastructure; native Supplier.Start was skipped.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to prepare custom supplier infrastructure before Start: {ex.Message}");
            }

            return false;
        }

        [HarmonyPatch(
            typeof(S1Economy.Supplier),
            nameof(S1Economy.Supplier.Load),
            new[] { typeof(S1Datas.DynamicSaveData), typeof(S1Datas.NPCData) })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SupplierLoadPrefix(
            S1Economy.Supplier __instance,
            S1Datas.DynamicSaveData dynamicData)
        {
            NPC? apiNpc = NPCPatches.FindWrapperForS1Npc(__instance);
            if (apiNpc == null || !apiNpc.IsCustomNPC)
                return true;

            if (!dynamicData.TryExtractBaseData<S1Datas.SupplierData>(out var data) || data == null)
            {
                Logger.Warning($"Custom supplier '{__instance.ID}' has no SupplierData in its save entry.");
                return false;
            }

            ReflectionUtils.TrySetFieldOrProperty(__instance, "_minsSinceMeetingStart", data.timeSinceMeetingStart);
            ReflectionUtils.TrySetFieldOrProperty(__instance, "_minsSinceLastMeetingEnd", data.timeSinceLastMeetingEnd);
            ReflectionUtils.TrySetFieldOrProperty(__instance, "MinsUntilDeaddropReady", data.minsUntilDeadDropReady);
            ReflectionUtils.TrySetFieldOrProperty(__instance, "_repaymentReminderSent", data.debtReminderSent);
            if (data.deaddropItems != null)
                ReflectionUtils.TrySetFieldOrProperty(__instance, "_deaddropItems", data.deaddropItems);

            bool preparing = data.minsUntilDeadDropReady > 0;
            if (__instance.IsServerInitialized)
            {
                __instance.sync___set_value__debt(data.debt, true);
                __instance.sync___set_value__deadDropPreparing(preparing, true);
            }
            else
            {
                // Load can run before Awake for custom NPCs. Seed fields used to construct FishNet SyncVars.
                __instance._debt = data.debt;
                __instance._deadDropPreparing = preparing;
            }

            return false;
        }

        [HarmonyPatch(typeof(S1Economy.SupplierStash), "Start")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SupplierStashStartPrefix(S1Economy.SupplierStash __instance)
        {
            return !SupplierRuntimeCoordinator.TryInitializeGeneratedStash(__instance);
        }

        [HarmonyPatch(typeof(S1Loaders.NPCLoader), nameof(S1Loaders.NPCLoader.Load))]
        [HarmonyPostfix]
        private static void NpcLoaderLoadPostfix(S1Datas.DynamicSaveData saveData)
        {
            if (!NPCPatches.IsInMainScene() || saveData == null)
                return;

            S1Datas.NPCData? baseData = saveData.ExtractBaseData<S1Datas.NPCData>();
            if (baseData == null || string.IsNullOrEmpty(baseData.ID))
                return;

            S1NPCs.NPC? nativeNpc = NPCPatches.FindBaseNpcById(baseData.ID);
            if (nativeNpc != null && CrossType.Is(nativeNpc, out S1Economy.Supplier supplier))
                SupplierRuntimeCoordinator.ReconcileDeliveryUnlock(supplier);
        }

        [HarmonyPatch(typeof(S1NPCs.NPC), "OnDestroy")]
        [HarmonyPostfix]
        private static void NpcOnDestroyPostfix(S1NPCs.NPC __instance)
        {
            if (CrossType.Is(__instance, out S1Economy.Supplier supplier))
                SupplierRuntimeCoordinator.CleanupSupplier(supplier);
        }
    }
}
