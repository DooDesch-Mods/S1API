#if (MONOMELON || MONOBEPINEX)
using S1Recycler = ScheduleOne.ObjectScripts.Recycler;
#else
using S1Recycler = Il2CppScheduleOne.ObjectScripts.Recycler;
#endif

using HarmonyLib;
using S1API.Trash;

namespace S1API.Internal.Patches
{
    /// <summary>INTERNAL: Registers native recyclers and releases their managed event listeners.</summary>
    [HarmonyPatch]
    internal static class RecyclerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(S1Recycler), nameof(S1Recycler.Start))]
        private static void Started(S1Recycler __instance) =>
            RecyclerRegistry.Wrap(__instance);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(S1Recycler), "OnDestroy")]
        private static void Destroying(S1Recycler __instance) =>
            RecyclerRegistry.Remove(__instance);
    }
}
