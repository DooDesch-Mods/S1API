#if IL2CPPMELON
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
using S1Equipping = Il2CppScheduleOne.Equipping;
using S1Player = Il2CppScheduleOne.PlayerScripts.Player;
#elif MONOMELON
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
using S1Equipping = ScheduleOne.Equipping;
using S1Player = ScheduleOne.PlayerScripts.Player;
#endif

using HarmonyLib;
using S1API.Internal.Utils;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Activates avatar presentation configured through EquippableBuilder for
    /// non-viewmodel equippables, including native buildable-item equippables.
    /// </summary>
    [HarmonyPatch]
    internal static class EquippableAvatarPatches
    {
        [HarmonyPatch(
            typeof(S1Equipping.Equippable),
            nameof(S1Equipping.Equippable.Equip))]
        [HarmonyPostfix]
        private static void Equip_Postfix(S1Equipping.Equippable __instance)
        {
            if (!TryGetNonViewmodelAvatar(__instance, out var avatar))
                return;

            S1Player.Local?.SendEquippable_Networked(avatar.AssetPath);
        }

        [HarmonyPatch(
            typeof(S1Equipping.Equippable),
            nameof(S1Equipping.Equippable.Unequip))]
        [HarmonyPrefix]
        private static void Unequip_Prefix(S1Equipping.Equippable __instance)
        {
            if (!TryGetNonViewmodelAvatar(__instance, out _))
                return;

            S1Player.Local?.SendEquippable_Networked(string.Empty);
        }

        private static bool TryGetNonViewmodelAvatar(
            S1Equipping.Equippable equippable,
            out S1AvatarEquipping.AvatarEquippable avatar)
        {
            avatar = null!;
            if (equippable == null ||
                CrossType.Is(
                    equippable,
                    out S1Equipping.Equippable_Viewmodel _))
            {
                return false;
            }

            avatar =
                equippable.GetComponentInChildren<
                    S1AvatarEquipping.AvatarEquippable>(true);
            return avatar != null &&
                   !string.IsNullOrWhiteSpace(avatar.AssetPath);
        }
    }
}
