#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal static class AvatarAccessoryDiagnostics
    {
        private const int NativeAccessorySlotCount = 9;
        private static readonly Log Logger = new Log("AvatarAccessoryDiagnostics");
        private static readonly HashSet<string> WarnedInvalidResources =
            new HashSet<string>(StringComparer.Ordinal);

        internal static void Validate(
            S1AvatarFramework.Avatar avatar,
            S1AvatarFramework.AvatarSettings settings)
        {
            try
            {
                ValidateCore(avatar, settings);
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"[S1API][AvatarAccessoryValidation] Could not validate accessory settings for " +
                    $"avatar '{DescribeAvatar(avatar)}': {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void ValidateCore(
            S1AvatarFramework.Avatar avatar,
            S1AvatarFramework.AvatarSettings settings)
        {
            if (settings?.AccessorySettings == null)
                return;

            int count = Math.Min(settings.AccessorySettings.Count, NativeAccessorySlotCount);
            for (int index = 0; index < count; index++)
            {
                var accessorySetting = settings.AccessorySettings[index];
                string? path = accessorySetting?.path;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                // Match the native ApplyAccessorySettings lookup exactly. This also honors assets
                // registered through RuntimeResourceRegistry/AccessoryFactory at runtime.
                UnityEngine.Object? resource = Resources.Load(path);
                if (resource == null)
                {
                    WarnInvalidAccessoryOnce(
                        avatar,
                        index,
                        path,
                        "Resources.Load returned null at runtime");
                    continue;
                }

                GameObject? accessoryObject = resource as GameObject;
                if (accessoryObject == null)
                {
                    WarnInvalidAccessoryOnce(
                        avatar,
                        index,
                        path,
                        $"Resources.Load returned {resource.GetType().FullName}, not a GameObject");
                    continue;
                }

                if (accessoryObject.GetComponent<S1AvatarFramework.Accessory>() == null)
                {
                    WarnInvalidAccessoryOnce(
                        avatar,
                        index,
                        path,
                        $"the loaded GameObject '{accessoryObject.name}' has no AvatarFramework.Accessory component");
                }
            }
        }

        private static void WarnInvalidAccessoryOnce(
            S1AvatarFramework.Avatar avatar,
            int index,
            string path,
            string reason)
        {
            string avatarDescription = DescribeAvatar(avatar);
            string warningKey = avatarDescription + "|" + index + "|" + path + "|" + reason;
            lock (WarnedInvalidResources)
            {
                if (!WarnedInvalidResources.Add(warningKey))
                    return;
            }

            Logger.Warning(
                $"[S1API][AvatarAccessoryValidation] Avatar '{avatarDescription}' has an invalid accessory " +
                $"at index {index}: path='{path}'; {reason}. Correct or remove the accessory path in " +
                "WithAppearanceDefaults/AddAccessory, or register the custom accessory with " +
                "AccessoryFactory before the appearance is applied. S1API left the settings unchanged; " +
                "the native ApplyAccessorySettings call may throw.");
        }

        private static string DescribeAvatar(S1AvatarFramework.Avatar? avatar)
        {
            if (avatar == null)
                return "<null-avatar>";

            try
            {
                var npc = avatar.GetComponentInParent<S1NPCs.NPC>(true);
                if (npc != null)
                {
                    string id = string.IsNullOrWhiteSpace(npc.ID) ? "<unknown-id>" : npc.ID;
                    string? name = ReflectionUtils.TryGetFieldOrProperty(npc, "fullName") as string;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        string? firstName = ReflectionUtils.TryGetFieldOrProperty(npc, "FirstName") as string;
                        string? lastName = ReflectionUtils.TryGetFieldOrProperty(npc, "LastName") as string;
                        name = $"{firstName} {lastName}".Trim();
                    }
                    if (string.IsNullOrWhiteSpace(name))
                        name = npc.name;
                    return $"{name} (ID={id})";
                }
            }
            catch
            {
                // Fall back to the avatar object name below.
            }

            return avatar.gameObject?.name ?? avatar.name ?? "<unknown-avatar>";
        }
    }
}
