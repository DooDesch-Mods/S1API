#if IL2CPPMELON
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1Economy = Il2CppScheduleOne.Economy;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Schedules = Il2CppScheduleOne.NPCs.Schedules;
#elif MONOMELON
using S1Dialogue = ScheduleOne.Dialogue;
using S1Economy = ScheduleOne.Economy;
using S1NPCs = ScheduleOne.NPCs;
using S1Schedules = ScheduleOne.NPCs.Schedules;
#endif

using System.Linq;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Internal.Entities.Suppliers
{
    /// <summary>
    /// Owns the reserved schedule action used by the native supplier meeting flow.
    /// </summary>
    internal static class SupplierMeetingRuntime
    {
        private const string MeetingActionName = "S1API_SupplierMeetingAction";

        internal static void EnsurePrefab(GameObject prefabRoot)
        {
            S1NPCs.NPCScheduleManager manager =
                prefabRoot.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (manager == null)
            {
                var managerObject = new GameObject("NPCSchedule");
                managerObject.transform.SetParent(prefabRoot.transform, false);
                manager = managerObject.AddComponent<S1NPCs.NPCScheduleManager>();
            }

            // Some fallback NPC prefabs do not carry a schedule manager. When we add one,
            // reconnect the native behaviour reference that Supplier.Start reads directly.
            S1NPCs.NPC? npc = prefabRoot.GetComponent<S1NPCs.NPC>();
            if (npc?.Behaviour != null)
                npc.Behaviour.ScheduleManager = manager;

            S1Schedules.NPCEvent_LocationDialogue? action = manager
                .GetComponentsInChildren<S1Schedules.NPCEvent_LocationDialogue>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.name == MeetingActionName);
            if (action == null)
            {
                var actionObject = new GameObject(MeetingActionName);
                actionObject.transform.SetParent(manager.transform, false);
                action = actionObject.AddComponent<S1Schedules.NPCEvent_LocationDialogue>();
            }

            action.StartTime = 501;
            action.Duration = 1439;
            action.EndTime = 500;
            action.Destination = null;
            action.FaceDestinationDir = true;
            action.DestinationThreshold = 1f;
            action.WarpIfSkipped = false;
            action.GreetingOverrideToEnable = 0;
            action.ChoiceToEnable = 0;
            action.DialogueOverride = null;
            action.transform.SetSiblingIndex(0);
            // Keep the reserved meeting action unavailable to NPCSchedule's inactive-action pool.
            // Native Supplier.Start selects this action, then disables it until a meeting begins.
            action.gameObject.SetActive(true);
        }

        internal static bool EnsureSelected(S1Economy.Supplier supplier)
        {
            S1NPCs.NPCScheduleManager? manager = supplier.Behaviour?.ScheduleManager;
            if (manager == null)
            {
                manager = supplier.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
                if (manager != null && supplier.Behaviour != null)
                    supplier.Behaviour.ScheduleManager = manager;
            }

            if (manager == null)
                return false;

            if (manager.ActionList == null || manager.ActionList.Count == 0)
                manager.InitializeActions();

            S1Schedules.NPCEvent_LocationDialogue? reserved = manager
                .GetComponentsInChildren<S1Schedules.NPCEvent_LocationDialogue>(true)
                .FirstOrDefault(action => action != null && action.gameObject.name == MeetingActionName);
            if (reserved == null || manager.ActionList == null)
                return false;

            int reservedIndex = manager.ActionList.IndexOf(reserved);
            if (reservedIndex < 0)
            {
                manager.InitializeActions();
                reservedIndex = manager.ActionList.IndexOf(reserved);
            }

            int firstLocationIndex = -1;
            for (int i = 0; i < manager.ActionList.Count; i++)
            {
                if (CrossType.Is(manager.ActionList[i], out S1Schedules.NPCEvent_LocationDialogue _))
                {
                    firstLocationIndex = i;
                    break;
                }
            }

            if (reservedIndex >= 0 && firstLocationIndex >= 0 && reservedIndex != firstLocationIndex)
            {
                manager.ActionList.Remove(reserved);
                manager.ActionList.Insert(firstLocationIndex, reserved);
            }

            for (int i = 0; i < manager.ActionList.Count; i++)
            {
                if (CrossType.Is(
                    manager.ActionList[i],
                    out S1Schedules.NPCEvent_LocationDialogue first))
                {
                    return first == reserved;
                }
            }

            return false;
        }

        internal static bool PrepareNativeStartDialogue(S1Economy.Supplier supplier)
        {
            S1Schedules.NPCEvent_LocationDialogue? reserved =
                FindReservedAction(supplier);
            S1Dialogue.DialogueController? controller =
                supplier.DialogueHandler
                    ?.GetComponent<S1Dialogue.DialogueController>();
            if (reserved == null
                || controller?.GreetingOverrides == null
                || controller.Choices == null)
            {
                return false;
            }

            SupplierMeetingDialogueBindings bindings =
                SupplierMeetingDialoguePolicy.ForNativeStart(
                    controller.GreetingOverrides.Count,
                    controller.Choices.Count);

            // Content prefabs such as BaseEmployee can contribute active dialogue
            // entries before they are converted to suppliers. Native Supplier.Start
            // appends its meeting entries, so preserve the inherited data but leave it
            // inactive while the supplier meeting action owns the interaction.
            for (int i = 0; i < controller.GreetingOverrides.Count; i++)
                controller.GreetingOverrides[i].ShouldShow = false;

            for (int i = 0; i < controller.Choices.Count; i++)
                controller.Choices[i].Enabled = false;

            reserved.GreetingOverrideToEnable =
                bindings.GreetingOverrideIndex;
            reserved.ChoiceToEnable = bindings.ChoiceIndex;
            return true;
        }

        internal static bool SetDialogueActive(
            S1Economy.Supplier supplier,
            bool active)
        {
            S1Schedules.NPCEvent_LocationDialogue? reserved =
                FindReservedAction(supplier);
            S1Dialogue.DialogueController? controller =
                supplier.DialogueHandler
                    ?.GetComponent<S1Dialogue.DialogueController>();
            if (reserved == null
                || controller?.GreetingOverrides == null
                || controller.Choices == null
                || reserved.GreetingOverrideToEnable < 0
                || reserved.GreetingOverrideToEnable
                    >= controller.GreetingOverrides.Count
                || reserved.ChoiceToEnable < 0
                || reserved.ChoiceToEnable >= controller.Choices.Count)
            {
                return false;
            }

            controller.GreetingOverrides[
                reserved.GreetingOverrideToEnable].ShouldShow = active;
            controller.Choices[
                reserved.ChoiceToEnable].Enabled = active;
            return true;
        }

        private static S1Schedules.NPCEvent_LocationDialogue?
            FindReservedAction(S1Economy.Supplier supplier)
        {
            S1NPCs.NPCScheduleManager? manager =
                supplier.Behaviour?.ScheduleManager
                ?? supplier.GetComponentInChildren<S1NPCs.NPCScheduleManager>(
                    true);
            return manager
                ?.GetComponentsInChildren<S1Schedules.NPCEvent_LocationDialogue>(
                    true)
                .FirstOrDefault(
                    action => action != null
                              && action.gameObject.name == MeetingActionName);
        }
    }
}
