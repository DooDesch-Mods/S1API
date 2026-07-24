#if (IL2CPPMELON)
using S1UI = Il2CppScheduleOne.UI;
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
#elif MONOMELON
using S1UI = ScheduleOne.UI;
using S1Persistence = ScheduleOne.Persistence;
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using HarmonyLib;
using MelonLoader;
using S1API.Entities;
using S1API.Internal.Products;
using S1API.Internal.Utils;
using S1API.Logging;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches the LoadingScreen to delay closing until queued rendered icons complete.
    /// This ensures players see the loading screen while S1API portraits and product icons generate.
    /// </summary>
    [HarmonyPatch]
    internal static class LoadingScreenPatches
    {
        private static readonly Log Logger = new Log("LoadingScreenPatches");
        private static bool _isWaitingForRenderedIcons;
        private static bool _waitForMugshots;
        private static bool _hasCustomNpcTypes = false;
        private static bool _allowGameClose;

        /// <summary>
        /// Patch GetLoadStatusText to describe the rendered-icon work still pending.
        /// </summary>
        [HarmonyPatch(typeof(S1Persistence.LoadManager), "GetLoadStatusText")]
        [HarmonyPostfix]
        private static void GetLoadStatusText_Postfix(ref string __result)
        {
            if (!_isWaitingForRenderedIcons)
                return;

            if (HasOutstandingProductIconWork(
                    CustomProductPresentationRuntime.GeneratedIconWorkComplete,
                    ProductPackagingContentRuntime.GeneratedIconWorkComplete))
                __result = "Generating Product Icons...";
            else if (_waitForMugshots &&
                     !NPCAppearance.MugshotsProcessingComplete)
                __result = "Generating NPC Mugshots...";
        }

        /// <summary>
        /// Target the Close method on LoadingScreen
        /// </summary>
        [HarmonyPatch(typeof(S1UI.LoadingScreen), "Close")]
        [HarmonyPrefix]
        private static bool Close_Prefix(S1UI.LoadingScreen __instance)
        {
            if (_allowGameClose)
                return true;

            if (!IsGameLoading())
                return true;

            bool waitForMugshots =
                ShouldWaitForMugshots() &&
                !NPCAppearance.MugshotsProcessingComplete;
            bool waitForProductIcons =
                HasOutstandingProductIconWork(
                    CustomProductPresentationRuntime.GeneratedIconWorkComplete,
                    ProductPackagingContentRuntime.GeneratedIconWorkComplete);
            if (!waitForMugshots && !waitForProductIcons)
                return true;

            if (_isWaitingForRenderedIcons)
                return false;

            _isWaitingForRenderedIcons = true;
            _waitForMugshots = waitForMugshots;
            MelonCoroutines.Start(
                WaitForRenderedIconsThenClose(
                    __instance,
                    waitForMugshots));

            return false;
        }

        private static bool HasCustomNpcInstances() =>
            NPC.All.Any(npc => npc != null && npc.IsCustomNPC);

        private static bool ShouldWaitForMugshots()
        {
            if (S1APIPreferences.EnableMugshotLoadingScreen?.Value == false ||
                !_hasCustomNpcTypes)
            {
                return false;
            }

            return HasCustomNpcInstances() || HasOutstandingMugshotWork();
        }

        internal static bool HasOutstandingProductIconWork(
            bool looseIconWorkComplete,
            bool packagingIconWorkComplete) =>
            !looseIconWorkComplete || !packagingIconWorkComplete;

        /// <summary>
        /// Check if we're currently in the final phase of game loading where NPC mugshots should complete.
        /// This is true when LoadStatus is None but IsLoading is still true (the moment before Close is called).
        /// Returns false when exiting to menu or in other contexts where mugshots aren't relevant.
        /// </summary>
        private static bool IsGameLoading()
        {
            try
            {
                var loadManager = S1DevUtilities.Singleton<S1Persistence.LoadManager>.Instance;
                if (loadManager == null)
                    return false;

                // We're in the game loading close phase when:
                // - IsLoading is true
                // - LoadStatus is None (set just before Close is called in LoadManager)
                // This happens at the very end of StartGame/LoadAsClient before LoadingScreen.Close()
                if (!loadManager.IsLoading)
                    return false;
                
                string sceneName = SceneManager.GetActiveScene().name;
                if (!string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase))
                    return false;
                
                return loadManager.LoadStatus == S1Persistence.LoadManager.ELoadStatus.None ||
                       loadManager.LoadStatus == S1Persistence.LoadManager.ELoadStatus.LoadingData;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true only if mugshot generation has actually started and still has queued or active work.
        /// </summary>
        private static bool HasOutstandingMugshotWork()
        {
            try
            {
                bool hasQueuedMugshots = TryReadStaticBool("_hasQueuedMugshots");
                bool isProcessingMugshots = TryReadStaticBool("_isProcessingMugshots");
                int queueCount = GetMugshotQueueCount();

                if (!hasQueuedMugshots)
                    return false;

                return isProcessingMugshots || queueCount > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadStaticBool(string memberName)
        {
                object? value = ReflectionUtils.TryGetStaticFieldOrProperty(typeof(NPCAppearance), memberName);
            if (value is bool boolValue)
                return boolValue;

            return value != null && bool.TryParse(value.ToString(), out bool parsed) && parsed;
        }

        /// <summary>
        /// Waits for product-icon and optional mugshot work before closing the loading screen.
        /// </summary>
        private static IEnumerator WaitForRenderedIconsThenClose(
            S1UI.LoadingScreen loadingScreen,
            bool waitForMugshots)
        {
            const float START_TIMEOUT = 5f;
            const float TIMEOUT = 90f;
            float startTimer = 0f;
            float timer = 0f;

            while (waitForMugshots &&
                   !HasOutstandingMugshotWork() &&
                   !NPCAppearance.MugshotsProcessingComplete &&
                   startTimer < START_TIMEOUT)
            {
                yield return new WaitForSeconds(0.1f);
                startTimer += 0.1f;
            }

            if (waitForMugshots &&
                !HasOutstandingMugshotWork() &&
                !NPCAppearance.MugshotsProcessingComplete)
            {
                waitForMugshots = false;
                _waitForMugshots = false;
            }

            while ((HasOutstandingProductIconWork(
                        CustomProductPresentationRuntime.GeneratedIconWorkComplete,
                        ProductPackagingContentRuntime.GeneratedIconWorkComplete) ||
                    (waitForMugshots &&
                     !NPCAppearance.MugshotsProcessingComplete)) &&
                   timer < TIMEOUT)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            _isWaitingForRenderedIcons = false;
            _waitForMugshots = false;
            
            if (timer >= TIMEOUT)
            {
                int remaining = GetMugshotQueueCount();
                Logger.Warning(
                    $"Rendered icon generation timeout reached after {TIMEOUT}s. " +
                    $"{remaining} NPCs may have incomplete portraits; " +
                    $"product icons complete: " +
                    $"{!HasOutstandingProductIconWork(
                        CustomProductPresentationRuntime.GeneratedIconWorkComplete,
                        ProductPackagingContentRuntime.GeneratedIconWorkComplete)}.");
            }
            
            CloseLoadingScreenDirectly(loadingScreen);
        }

        /// <summary>
        /// Gets the current mugshot queue count via reflection (internal member)
        /// </summary>
        private static int GetMugshotQueueCount()
        {
            var queue = ReflectionUtils.TryGetStaticFieldOrProperty(typeof(NPCAppearance), "_mugshotQueue");
            if (queue is System.Collections.IEnumerable enumerable)
            {
                int count = 0;
                foreach (var _ in enumerable)
                    count++;
                return count;
            }
            
            return -1;
        }

        /// <summary>
        /// Closes the loading screen through the game's implementation while bypassing this prefix.
        /// The beta implementation also removes its state from SceneState, so reproducing only
        /// the visual fade leaves all player input blocked after loading.
        /// </summary>
        private static void CloseLoadingScreenDirectly(S1UI.LoadingScreen loadingScreen)
        {
            try
            {
                _allowGameClose = true;
                loadingScreen.Close();
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error closing loading screen: {ex.Message}");
                ReflectionUtils.TrySetFieldOrProperty(loadingScreen, "IsOpen", false);
                if (loadingScreen.Canvas != null)
                    loadingScreen.Canvas.enabled = false;
            }
            finally
            {
                _allowGameClose = false;
            }
        }

        /// <summary>
        /// Called when the scene changes to reset our state
        /// </summary>
        internal static void ResetState()
        {
            _isWaitingForRenderedIcons = false;
            _waitForMugshots = false;
            _allowGameClose = false;
            _hasCustomNpcTypes = ReflectionUtils.GetDerivedClasses<NPC>()
                .Any(t => t != null && !t.IsAbstract && t.Assembly != typeof(NPC).Assembly);
        }
    }
}
