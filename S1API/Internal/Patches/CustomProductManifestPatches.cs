#if IL2CPPMELON
using S1Connection = Il2CppFishNet.Connection.NetworkConnection;
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1Player = Il2CppScheduleOne.PlayerScripts.Player;
#elif MONOMELON
using S1Connection = FishNet.Connection.NetworkConnection;
using S1Persistence = ScheduleOne.Persistence;
using S1Player = ScheduleOne.PlayerScripts.Player;
#endif

using HarmonyLib;
using S1API.Internal.Products;

namespace S1API.Internal.Patches
{
    /// <summary>INTERNAL: Starts and stops custom-product manifest handshakes with native loading.</summary>
    [HarmonyPatch]
    internal static class CustomProductManifestLoadPatches
    {
        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.StartGame))]
        [HarmonyPrefix]
        private static void StartGamePrefix()
        {
            CustomProductManifestRuntime.BeginHostSession();
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.LoadAsClient))]
        [HarmonyPrefix]
        private static void LoadAsClientPrefix()
        {
            CustomProductManifestRuntime.BeginClientSession();
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.ExitToMenu))]
        [HarmonyPrefix]
        private static void ExitToMenuPrefix()
        {
            CustomProductManifestRuntime.EndHostSession();
            CustomProductManifestRuntime.EndClientSession();
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.Update))]
        [HarmonyPostfix]
        private static void UpdatePostfix()
        {
            CustomProductManifestRuntime.Tick();
        }
    }

    /// <summary>INTERNAL: Prevents client inventory deserialization before manifest validation.</summary>
    [HarmonyPatch]
    internal static class CustomProductManifestPlayerPatches
    {
        [HarmonyPatch(typeof(S1Player), nameof(S1Player.RequestPlayerData))]
        [HarmonyPrefix]
        private static bool RequestPlayerDataPrefix(S1Player __instance, string playerCode)
        {
            return CustomProductManifestRuntime.AuthorizeClientPlayerDataRequest(
                () => __instance.RequestPlayerData(playerCode));
        }

        [HarmonyPatch(typeof(S1Player), nameof(S1Player.ReceivePlayerData))]
        [HarmonyPrefix]
        private static bool ReceivePlayerDataPrefix(object __instance, object[] __args)
        {
            if (__args.Length == 0 || !(__args[0] is S1Connection connection))
                return true;

            return CustomProductManifestRuntime.AuthorizeHostPlayerData(
                __instance,
                connection,
                __args);
        }
    }
}
