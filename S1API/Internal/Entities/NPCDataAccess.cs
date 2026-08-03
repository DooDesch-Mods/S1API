#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Employees = Il2CppScheduleOne.Employees;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1NPCFramework = Il2CppScheduleOne.NPCs.Framework;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1VoiceOver = Il2CppScheduleOne.VoiceOver;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Dialogue = ScheduleOne.Dialogue;
using S1Economy = ScheduleOne.Economy;
using S1Employees = ScheduleOne.Employees;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Messaging = ScheduleOne.Messaging;
using S1NPCFramework = ScheduleOne.NPCs.Framework;
using S1NPCs = ScheduleOne.NPCs;
using S1VoiceOver = ScheduleOne.VoiceOver;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using S1API.Entities.Dealer;
using S1API.Entities.Supplier;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Internal.Entities
{
    internal static class NPCDataAccess
    {
        private static readonly Logging.Log Logger = new Logging.Log("NPCDataAccess");
#if !IL2CPPMELON
        private static readonly FieldInfo NpcDataObjectField =
            typeof(S1NPCs.NPC).GetField("_npcData", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(S1NPCs.NPC).FullName, "_npcData");
        private static readonly FieldInfo CurrentNpcDataField =
            typeof(S1NPCs.NPC).GetField("<NPCData>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(S1NPCs.NPC).FullName, "<NPCData>k__BackingField");
#endif

        internal static void AssignNewData(
            S1NPCs.NPC npc,
            NpcRootRole rootRole,
            S1NPCs.NPC? sourceNpc = null)
        {
            if (npc == null)
                throw new ArgumentNullException(nameof(npc));

            S1NPCFramework.BaseNPCDataObject dataObject = rootRole switch
            {
                NpcRootRole.Dealer => ScriptableObject.CreateInstance<S1NPCFramework.DealerNPCDataObject>(),
                NpcRootRole.Supplier => ScriptableObject.CreateInstance<S1NPCFramework.SupplierNPCDataObject>(),
                _ => ScriptableObject.CreateInstance<S1NPCFramework.NPCDataObject>()
            };

            if (dataObject == null)
                throw new InvalidOperationException("Failed to create the beta NPC data object.");

            dataObject.hideFlags = HideFlags.DontUnloadUnusedAsset;
            dataObject.Initialize();
            S1NPCFramework.NPCData data = dataObject.GetOriginalData();
            PrepareData(data);
            if (rootRole == NpcRootRole.Supplier)
            {
                // Native supplier messaging presets do not allow their persistent
                // order conversations to be hidden from the Messages app.
                data.Messaging.ConversationCanBeHidden = false;
            }
            EnsureDialogueDatabase(data, sourceNpc);
            if (rootRole == NpcRootRole.Supplier)
                EnsureSupplierDialogueDatabase(data, required: false);
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

        internal static bool ApplyVoice(
            S1NPCs.NPC npc,
            S1VoiceOver.VODatabase database,
            float? pitch)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            return ApplyToData(npc, data =>
            {
                if (data.Voice == null)
                    throw new InvalidOperationException("The custom NPC data has no voice settings.");

                data.Voice.VoiceDatabase = database;
                if (pitch.HasValue)
                    data.Voice.VoicePitch = pitch.Value;
            });
        }

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

        internal static bool ApplySupplierDefaults(
            S1Economy.Supplier supplier,
            SupplierDataBuilder.SupplierConfigData data)
        {
            if (supplier == null || data == null)
                return false;

            S1NPCFramework.NPCData? originalData = GetOriginalData(supplier);
            if (originalData == null
                || !CrossType.Is(originalData, out S1NPCFramework.SupplierNPCData supplierData))
                return false;

            ApplySupplierDefaults(supplierData, data);

            S1NPCFramework.NPCData? currentData = GetCurrentData(supplier);
            if (currentData != null
                && CrossType.Is(currentData, out S1NPCFramework.SupplierNPCData currentSupplierData)
                && !ReferenceEquals(currentSupplierData, supplierData))
            {
                ApplySupplierDefaults(currentSupplierData, data);
            }

            return true;
        }

        internal static void PrepareForRuntime(S1NPCs.NPC npc)
        {
            S1NPCFramework.NPCData data = GetOriginalData(npc)
                ?? throw new InvalidOperationException("The beta NPC has no framework data before network spawn.");

            PrepareData(data);
            EnsureDialogueDatabase(data);

            if (CrossType.Is(data, out S1NPCFramework.SupplierNPCData _))
                EnsureSupplierDialogueDatabase(data, required: true);

            if (data is S1NPCFramework.DealerNPCData dealerData)
                PopulateDealerDialogueDefaults(dealerData);
        }

        private static void ApplySupplierDefaults(
            S1NPCFramework.SupplierNPCData supplierData,
            SupplierDataBuilder.SupplierConfigData data)
        {
            supplierData.MinimumDeaddropOrderLimit = data.MinimumDeaddropOrderLimit;
            supplierData.MaximumDeaddropOrderLimit = data.MaximumDeaddropOrderLimit;
            supplierData.SupplierRecommendMessage = data.SupplierRecommendMessage;
            supplierData.SupplierUnlockHint = data.SupplierUnlockHint;

#if IL2CPPMELON
            IReadOnlyList<S1ItemFramework.StorableItemDefinition> deliveryItems =
                data.ResolveDeliveryItems();
            var listings = new Il2CppReferenceArray<Il2CppScheduleOne.UI.Phone.PhoneShopInterface.Listing>(deliveryItems.Count);
            for (int i = 0; i < deliveryItems.Count; i++)
                listings[i] = new Il2CppScheduleOne.UI.Phone.PhoneShopInterface.Listing(deliveryItems[i]);
#else
            IReadOnlyList<S1ItemFramework.StorableItemDefinition> deliveryItems =
                data.ResolveDeliveryItems();
            var listings = new ScheduleOne.UI.Phone.PhoneShopInterface.Listing[deliveryItems.Count];
            for (int i = 0; i < deliveryItems.Count; i++)
                listings[i] = new ScheduleOne.UI.Phone.PhoneShopInterface.Listing(deliveryItems[i]);
#endif
            supplierData.DeliveryShopListings = listings;
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

            if (CrossType.Is(data, out S1NPCFramework.SupplierNPCData supplierData))
                supplierData.DeliveryShopListings ??=
                    new Il2CppReferenceArray<Il2CppScheduleOne.UI.Phone.PhoneShopInterface.Listing>(0);
#else
            data.Inventory.RandomInventoryItems ??= Array.Empty<S1NPCFramework.Inventory.WeightedItem>();
            data.Inventory.StartingInventoryItems ??= Array.Empty<ScheduleOne.ItemFramework.ItemDefinition>();
            data.Messaging.ConversationCategories ??= Array.Empty<ScheduleOne.Messaging.EConversationCategory>();

            if (CrossType.Is(data, out S1NPCFramework.SupplierNPCData supplierData))
                supplierData.DeliveryShopListings ??= Array.Empty<ScheduleOne.UI.Phone.PhoneShopInterface.Listing>();
#endif
        }

        private static void EnsureDialogueDatabase(
            S1NPCFramework.NPCData data,
            S1NPCs.NPC? sourceNpc = null)
        {
            if (data.Dialogue == null)
                throw new InvalidOperationException("The beta NPC data has no dialogue settings.");

            if (data.Dialogue.DialogueDatabase != null)
                return;

            S1NPCFramework.NPCData? sourceData = GetOriginalData(sourceNpc) ?? GetCurrentData(sourceNpc);
            bool sourceIsEmployee = sourceNpc is S1Employees.Employee;
            if (ShouldReuseSourceDialogueDatabase(sourceIsEmployee)
                && sourceData?.Dialogue?.DialogueDatabase != null)
            {
                data.Dialogue.DialogueDatabase = sourceData.Dialogue.DialogueDatabase;
            }

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

            if (sourceIsEmployee)
            {
                Logger.Debug(
                    $"[S1API][BaseEmployeeFallback][Dialogue] Rebased employee source dialogue " +
                    $"'{sourceData?.Dialogue?.DialogueDatabase?.name ?? "<null>"}' to " +
                    $"'{data.Dialogue.DialogueDatabase.name}' for the replacement NPC data.");
            }
        }

        /// <summary>
        /// Determines whether a source NPC's dialogue database may be inherited by a rebuilt custom NPC.
        /// Employee databases contain employee-only greeting and transfer content, so the BaseEmployee
        /// fallback must instead resolve the native default database for the replacement NPC role.
        /// </summary>
        internal static bool ShouldReuseSourceDialogueDatabase(bool sourceIsEmployee) => !sourceIsEmployee;

        private static void EnsureSupplierDialogueDatabase(
            S1NPCFramework.NPCData data,
            bool required)
        {
            var dialogue = data.Dialogue;
            if (dialogue == null)
            {
                if (required)
                    throw new InvalidOperationException("The beta supplier NPC data has no dialogue settings.");

                return;
            }

            S1Dialogue.DialogueDatabase? current = dialogue.DialogueDatabase;
            if (current != null
                && current.name.StartsWith("S1API_SupplierDialogue_", StringComparison.Ordinal)
                && HasRequiredSupplierDialogue(current))
            {
                return;
            }

            S1Dialogue.DialogueDatabase? donor = Resources
                .FindObjectsOfTypeAll<S1Dialogue.DialogueDatabase>()
                .Where(HasRequiredSupplierDialogue)
                .OrderBy(database => database.name, StringComparer.Ordinal)
                .FirstOrDefault();

            if (donor == null)
            {
                if (required)
                {
                    throw new InvalidOperationException(
                        "No loaded supplier dialogue database contains the native supplier meeting entries.");
                }

                return;
            }

            S1Dialogue.DialogueDatabase clone = UnityEngine.Object.Instantiate(donor);
            clone.name = "S1API_SupplierDialogue_" + donor.name;
            clone.hideFlags = HideFlags.DontUnloadUnusedAsset;
            ApplyGenericSupplierDialogue(clone);
            dialogue.DialogueDatabase = clone;
        }

        private static bool HasRequiredSupplierDialogue(S1Dialogue.DialogueDatabase database)
        {
            if (database?.GenericEntries == null)
                return false;

            var requiredKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "supplier_unlocked",
                "supplier_meet_confirm",
                "supplier_meeting_greeting",
                "meeting_order_complete",
                "supplier_meetings_unlocked",
                "supplier_deliveries_unlocked"
            };

            foreach (S1Dialogue.Entry entry in database.GenericEntries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                    requiredKeys.Remove(entry.Key);
            }

            return requiredKeys.Count == 0;
        }

        private static void ApplyGenericSupplierDialogue(S1Dialogue.DialogueDatabase database)
        {
            SetSupplierDialogueLines(
                database,
                "supplier_unlocked",
                "I've heard you're looking for supplies.",
                "Send me a message when you'd like to place an order. You can pay off the balance later.");
            SetSupplierDialogueLines(
                database,
                "supplier_meet_confirm",
                "Agreed. I'll be <LOCATION> for the next 6 hours.");
            SetSupplierDialogueLines(
                database,
                "supplier_meeting_greeting",
                "Ready to look over the supplies?");
            SetSupplierDialogueLines(
                database,
                "meeting_order_complete",
                "Good doing business with you.");
            SetSupplierDialogueLines(
                database,
                "supplier_meetings_unlocked",
                "You've proven reliable, so we can now arrange in-person meetings for larger orders.",
                "Send me a message when you'd like to meet.");
            SetSupplierDialogueLines(
                database,
                "supplier_deliveries_unlocked",
                "I can now deliver supplies directly to your properties. Use the deliveries app to place an order.");
        }

        private static void SetSupplierDialogueLines(
            S1Dialogue.DialogueDatabase database,
            string key,
            params string[] values)
        {
            if (database?.GenericEntries == null)
                return;

            foreach (S1Dialogue.Entry entry in database.GenericEntries)
            {
                if (!string.Equals(entry.Key, key, StringComparison.Ordinal) || entry.Chains == null)
                    continue;

                for (int i = 0; i < entry.Chains.Length; i++)
                {
                    S1Dialogue.DialogueChain? chain = entry.Chains[i];
                    if (chain == null)
                        continue;

#if IL2CPPMELON
                    var lines = new Il2CppStringArray(values.Length);
                    for (int lineIndex = 0; lineIndex < values.Length; lineIndex++)
                        lines[lineIndex] = values[lineIndex];
                    chain.Lines = lines;
#else
                    chain.Lines = values.ToArray();
#endif
                }

                return;
            }
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
