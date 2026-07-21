#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1Economy = Il2CppScheduleOne.Economy;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1NPCFramework = Il2CppScheduleOne.NPCs.Framework;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif MONOMELON || MONOBEPINEX || IL2CPPBEPINEX
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Dialogue = ScheduleOne.Dialogue;
using S1Economy = ScheduleOne.Economy;
using S1Messaging = ScheduleOne.Messaging;
using S1NPCFramework = ScheduleOne.NPCs.Framework;
using S1NPCs = ScheduleOne.NPCs;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using S1API.Entities.Dealer;
using UnityEngine;

namespace S1API.Internal.Entities
{
    internal static class NPCDataAccess
    {
#if !IL2CPPMELON
        private static readonly FieldInfo NpcDataObjectField =
            typeof(S1NPCs.NPC).GetField("_npcData", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(S1NPCs.NPC).FullName, "_npcData");
        private static readonly FieldInfo CurrentNpcDataField =
            typeof(S1NPCs.NPC).GetField("<NPCData>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(S1NPCs.NPC).FullName, "<NPCData>k__BackingField");
#endif

        internal static void AssignNewData(S1NPCs.NPC npc, bool useDealerData)
        {
            if (npc == null)
                throw new ArgumentNullException(nameof(npc));

            S1NPCFramework.BaseNPCDataObject dataObject = useDealerData
                ? ScriptableObject.CreateInstance<S1NPCFramework.DealerNPCDataObject>()
                : ScriptableObject.CreateInstance<S1NPCFramework.NPCDataObject>();

            if (dataObject == null)
                throw new InvalidOperationException("Failed to create the beta NPC data object.");

            dataObject.hideFlags = HideFlags.DontUnloadUnusedAsset;
            dataObject.Initialize();
            S1NPCFramework.NPCData data = dataObject.GetOriginalData();
            PrepareData(data);
            SetDataObject(npc, dataObject);
            SetCurrentData(npc, data);
        }

        internal static void InitializeCurrentDataForConstruction(S1NPCs.NPC npc)
        {
            if (npc == null)
                throw new ArgumentNullException(nameof(npc));

            S1NPCFramework.BaseNPCDataObject dataObject = GetDataObject(npc)
                ?? throw new InvalidOperationException("The custom NPC prefab has no framework data object.");
            S1NPCFramework.NPCData data = dataObject.GetRuntimeData()
                ?? throw new InvalidOperationException("The custom NPC data object returned no runtime data.");
            PrepareData(data);
            SetCurrentData(npc, data);
        }

        internal static bool ApplyIdentity(
            S1NPCs.NPC npc,
            string? id,
            string? firstName,
            string? lastName)
        {
            return ApplyToData(npc, data =>
            {
                S1NPCFramework.BasicInfo basicInfo = data.BasicInfo;
                if (!string.IsNullOrWhiteSpace(id))
                    basicInfo.ID = id;
                if (!string.IsNullOrWhiteSpace(firstName))
                    basicInfo.FirstName = firstName;

                basicInfo.HasLastName = !string.IsNullOrWhiteSpace(lastName);
                basicInfo.LastName = basicInfo.HasLastName ? lastName : string.Empty;
            });
        }

        internal static string GetId(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.BasicInfo?.ID ?? string.Empty;

        internal static string GetFirstName(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.BasicInfo?.FirstName ?? string.Empty;

        internal static string GetLastName(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.BasicInfo?.LastName ?? string.Empty;

        internal static bool ApplyFirstName(S1NPCs.NPC npc, string firstName) =>
            ApplyToData(npc, data => data.BasicInfo.FirstName = firstName ?? string.Empty);

        internal static bool ApplyLastName(S1NPCs.NPC npc, string lastName) =>
            ApplyToData(npc, data =>
            {
                data.BasicInfo.HasLastName = !string.IsNullOrWhiteSpace(lastName);
                data.BasicInfo.LastName = data.BasicInfo.HasLastName ? lastName : string.Empty;
            });

        internal static bool ApplyId(S1NPCs.NPC npc, string id) =>
            ApplyToData(npc, data => data.BasicInfo.ID = id ?? string.Empty);

        internal static bool ApplyIcon(S1NPCs.NPC npc, Sprite? icon)
        {
            return ApplyToData(npc, data => data.Appearance.Mugshot = icon);
        }

        internal static Sprite? GetIcon(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.Appearance?.Mugshot;

        internal static bool GetConversationCanBeHidden(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.Messaging?.ConversationCanBeHidden ?? false;

        internal static bool ApplyConversationCanBeHidden(S1NPCs.NPC npc, bool value) =>
            ApplyToData(npc, data => data.Messaging.ConversationCanBeHidden = value);

        internal static IReadOnlyList<S1Messaging.EConversationCategory> GetConversationCategories(
            S1NPCs.NPC npc)
        {
            var categories = GetCurrentData(npc)?.Messaging?.ConversationCategories;
            if (categories == null)
                return Array.Empty<S1Messaging.EConversationCategory>();

            var result = new List<S1Messaging.EConversationCategory>(categories.Length);
            foreach (S1Messaging.EConversationCategory category in categories)
                result.Add(category);
            return result;
        }

        internal static bool ApplyConversationCategories(
            S1NPCs.NPC npc,
            IEnumerable<S1Messaging.EConversationCategory> categories)
        {
            var values = categories?.ToArray() ?? Array.Empty<S1Messaging.EConversationCategory>();
            return ApplyToData(npc, data =>
            {
#if IL2CPPMELON
                var array = new Il2CppStructArray<S1Messaging.EConversationCategory>(values.Length);
                for (int i = 0; i < values.Length; i++)
                    array[i] = values[i];
                data.Messaging.ConversationCategories = array;
#else
                data.Messaging.ConversationCategories = values;
#endif
            });
        }

        internal static bool ApplyAppearance(S1NPCs.NPC npc, S1AvatarFramework.AvatarSettings? settings)
        {
            if (settings == null)
                return false;

            S1NPCFramework.Appearance? appearance = GetOriginalData(npc)?.Appearance;
            if (appearance == null)
                return false;

            appearance.AvatarSettings = settings;
            return true;
        }

        internal static bool ApplyDealerDefaults(
            S1Economy.Dealer dealer,
            DealerDataBuilder.DealerConfigData data)
        {
            if (dealer == null || data == null)
                return false;

            if (GetOriginalData(dealer) is not S1NPCFramework.DealerNPCData dealerData)
                return false;

            dealerData.SigningFee = data.SigningFee;
            dealerData.SalesCutPercentage = data.Cut;
            dealerData.DealerType = (S1Economy.EDealerType)(int)data.DealerType;
            if (!string.IsNullOrWhiteSpace(data.HomeName))
                dealerData.HomeName = data.HomeName;
            return true;
        }

        internal static void PrepareForRuntime(S1NPCs.NPC npc)
        {
            S1NPCFramework.NPCData data = GetOriginalData(npc)
                ?? throw new InvalidOperationException("The beta NPC has no framework data before network spawn.");

            PrepareData(data);
            if (data.Dialogue.DialogueDatabase == null)
            {
                S1Dialogue.DialogueManager manager =
                    S1DevUtilities.Singleton<S1Dialogue.DialogueManager>.Instance;
                if (manager != null)
                    data.Dialogue.DialogueDatabase = manager.DefaultDatabase;
            }

            if (data.Dialogue.DialogueDatabase == null)
            {
                S1Dialogue.DialogueDatabase[] databases =
                    Resources.FindObjectsOfTypeAll<S1Dialogue.DialogueDatabase>();
                if (databases.Length > 0)
                    data.Dialogue.DialogueDatabase = databases[0];
            }

            if (data.Dialogue.DialogueDatabase == null)
                throw new InvalidOperationException("No 0.4.6 dialogue database is loaded for the custom NPC.");

            if (data is S1NPCFramework.DealerNPCData dealerData)
                PopulateDealerDialogueDefaults(dealerData);
        }

        private static S1NPCFramework.NPCData? GetOriginalData(S1NPCs.NPC? npc)
        {
            return npc == null ? null : GetDataObject(npc)?.GetOriginalData();
        }

        private static S1NPCFramework.NPCData? GetCurrentData(S1NPCs.NPC? npc)
        {
            if (npc == null)
                return null;

            return npc.NPCData ?? GetOriginalData(npc);
        }

        private static bool ApplyToData(S1NPCs.NPC? npc, Action<S1NPCFramework.NPCData> apply)
        {
            if (npc == null)
                return false;

            S1NPCFramework.NPCData? originalData = GetOriginalData(npc);
            S1NPCFramework.NPCData? currentData = npc.NPCData;
            if (originalData == null && currentData == null)
                return false;

            if (originalData != null)
                apply(originalData);
            if (currentData != null && !ReferenceEquals(currentData, originalData))
                apply(currentData);
            return true;
        }

        internal static S1NPCFramework.BaseNPCDataObject? GetDataObject(S1NPCs.NPC npc)
        {
#if IL2CPPMELON
            return npc._npcData;
#else
            return NpcDataObjectField.GetValue(npc) as S1NPCFramework.BaseNPCDataObject;
#endif
        }

        private static void SetDataObject(
            S1NPCs.NPC npc,
            S1NPCFramework.BaseNPCDataObject dataObject)
        {
#if IL2CPPMELON
            npc._npcData = dataObject;
#else
            NpcDataObjectField.SetValue(npc, dataObject);
#endif
        }

        private static void SetCurrentData(S1NPCs.NPC npc, S1NPCFramework.NPCData data)
        {
#if IL2CPPMELON
            npc.NPCData = data;
#else
            CurrentNpcDataField.SetValue(npc, data);
#endif
        }

        private static void PrepareData(S1NPCFramework.NPCData data)
        {
            if (data == null)
                throw new InvalidOperationException("The beta NPC data object returned no data.");

#if IL2CPPMELON
            data.Inventory.RandomInventoryItems ??=
                new Il2CppReferenceArray<S1NPCFramework.Inventory.WeightedItem>(0);
            data.Inventory.StartingInventoryItems ??=
                new Il2CppReferenceArray<S1ItemFramework.ItemDefinition>(0);
            data.Messaging.ConversationCategories ??=
                new Il2CppStructArray<S1Messaging.EConversationCategory>(0);
#else
            data.Inventory.RandomInventoryItems ??= Array.Empty<S1NPCFramework.Inventory.WeightedItem>();
            data.Inventory.StartingInventoryItems ??= Array.Empty<ScheduleOne.ItemFramework.ItemDefinition>();
            data.Messaging.ConversationCategories ??= Array.Empty<ScheduleOne.Messaging.EConversationCategory>();
#endif
        }

        private static void PopulateDealerDialogueDefaults(S1NPCFramework.DealerNPCData dealerData)
        {
            if (dealerData.RecruitDialogue != null
                && dealerData.CollectCashDialogue != null
                && dealerData.AssignCustomersDialogue != null)
            {
                return;
            }

            S1NPCFramework.DealerNPCDataObject[] donors =
                Resources.FindObjectsOfTypeAll<S1NPCFramework.DealerNPCDataObject>();
            foreach (S1NPCFramework.DealerNPCDataObject donor in donors)
            {
                if (donor == null || donor.GetOriginalData() is not S1NPCFramework.DealerNPCData source)
                    continue;

                dealerData.RecruitDialogue ??= source.RecruitDialogue;
                dealerData.CollectCashDialogue ??= source.CollectCashDialogue;
                dealerData.AssignCustomersDialogue ??= source.AssignCustomersDialogue;

                if (dealerData.RecruitDialogue != null
                    && dealerData.CollectCashDialogue != null
                    && dealerData.AssignCustomersDialogue != null)
                {
                    return;
                }
            }
        }
    }
}
