#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using S1StationFramework = Il2CppScheduleOne.StationFramework;
using S1UIStations = Il2CppScheduleOne.UI.Stations;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
using S1StationFramework = ScheduleOne.StationFramework;
using S1UIStations = ScheduleOne.UI.Stations;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Logging;
using S1API.Stations;
using UnityEngine;
using UnityEngine.Events;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Harmony patches to inject S1API-registered Chemistry Station recipes into the UI.
    /// </summary>
    [HarmonyPatch]
    internal static class ChemistryStationPatches
    {
        private static readonly Log Logger = new Log("ChemistryStationPatches");
        private static readonly ConditionalWeakTable<object, CanvasInjectionState> CanvasStateTable =
            new ConditionalWeakTable<object, CanvasInjectionState>();
        private static readonly MethodInfo? SetSelectedRecipeMethod =
            AccessTools.Method(
                typeof(S1UIStations.ChemistryStationInterface),
                "SetSelectedRecipe");

        private static bool _loggedRecipeEntriesMissing;
        private static bool _loggedSetSelectedRecipeMissing;

        private sealed class CanvasInjectionState
        {
            public readonly HashSet<string> LoggedRecipeConflicts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> LoggedEntryConflicts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<int> ClickBoundEntryIds = new HashSet<int>();
        }

        private static CanvasInjectionState GetCanvasState(object canvas)
        {
            return CanvasStateTable.GetValue(canvas, _ => new CanvasInjectionState());
        }

        private static bool TryGetRecipeEntriesList(
            S1UIStations.ChemistryStationInterface canvas,
#if IL2CPPMELON
            out Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry>? entries
#else
            out List<S1UIStations.StationRecipeEntry>? entries
#endif
        )
        {
            entries = null;
            if (canvas == null)
                return false;

            try
            {
                entries =
#if IL2CPPMELON
                    canvas.recipeEntries;
#else
                    ReflectionUtils.TryGetFieldOrProperty(canvas, "recipeEntries")
                        as List<S1UIStations.StationRecipeEntry>;
#endif
            }
            catch
            {
                entries = null;
            }

            return entries != null;
        }

        [HarmonyPatch(typeof(S1UIStations.ChemistryStationInterface), "Awake")]
        [HarmonyPrefix]
        private static void ChemistryStationAwakePrefix(
            S1UIStations.ChemistryStationInterface __instance)
        {
            AwakePrefix(__instance);
        }

        [HarmonyPatch(typeof(S1UIStations.ChemistryStationInterface), "Open")]
        [HarmonyPrefix]
        private static void ChemistryStationOpenPrefix(
            S1UIStations.ChemistryStationInterface __instance,
            S1ObjectScripts.ChemistryStation __0)
        {
            OpenPrefix(__instance, __0);
        }

        private static void AwakePrefix(S1UIStations.ChemistryStationInterface __instance)
        {
            try
            {
                InjectRegisteredRecipes(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] ChemistryStationCanvas.Awake inject failed: {ex.Message}");
            }
        }

        private static void OpenPrefix(
            S1UIStations.ChemistryStationInterface __instance,
            S1ObjectScripts.ChemistryStation __0)
        {
            try
            {
                // Late registrations: ensure Recipes and recipeEntries are in sync before StationSlotsChanged runs.
                InjectRegisteredRecipes(__instance);
                EnsureRecipeEntries(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] ChemistryStationCanvas.Open sync failed: {ex.Message}");
            }
        }

        private static void InjectRegisteredRecipes(
            S1UIStations.ChemistryStationInterface canvas)
        {
            if (canvas == null)
                return;

            var registered = ChemistryStationRecipes.GetAllNative();
            if (registered.Count == 0)
                return;

            var recipes = canvas.Recipes;
            if (recipes == null)
                return;

            var state = GetCanvasState(canvas);
            for (int i = 0; i < registered.Count; i++)
            {
                var custom = registered[i];
                if (custom == null)
                    continue;

                string? id;
                try { id = custom.RecipeID; } catch { id = null; }
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var existing = FindRecipeById(recipes, id);
                if (existing != null)
                {
                    if (!ReferenceEquals(existing, custom) && state.LoggedRecipeConflicts.Add(id))
                    {
                        Logger.Warning($"[S1API] Chemistry Station already has a recipe with ID '{id}' (different instance). Skipping S1API injection for this ID.");
                    }
                    continue;
                }

                recipes.Add(custom);
            }
        }

        private static void EnsureRecipeEntries(
            S1UIStations.ChemistryStationInterface canvas)
        {
            if (canvas == null)
                return;

            if (!TryGetRecipeEntriesList(canvas, out var entries) || entries == null)
            {
                if (!_loggedRecipeEntriesMissing)
                {
                    _loggedRecipeEntriesMissing = true;
                    Logger.Warning("[S1API] ChemistryStationCanvas recipeEntries could not be resolved. Late recipe UI sync will be skipped.");
                }

                return;
            }

            var registered = ChemistryStationRecipes.GetAllNative();
            var state = GetCanvasState(canvas);
            for (int i = 0; i < registered.Count; i++)
            {
                var recipe = registered[i];
                if (recipe == null)
                    continue;

                string? id;
                try { id = recipe.RecipeID; } catch { id = null; }
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var existingEntry = FindEntryByRecipeId(entries, id);
                if (existingEntry != null)
                {
                    var existingRecipe = existingEntry.Recipe;
                    if (!ReferenceEquals(existingRecipe, recipe) && state.LoggedEntryConflicts.Add(id))
                    {
                        Logger.Warning($"[S1API] Chemistry Station UI already has an entry for recipe ID '{id}' (different recipe). Skipping entry injection for this ID.");
                    }
                    continue;
                }

                try
                {
                    var recipeEntryPrefab = canvas.RecipeEntryPrefab;
                    var recipeContainer = canvas.RecipeContainer;
                    if (recipeEntryPrefab == null || recipeContainer == null)
                        return;

                    var entry = UnityEngine.Object.Instantiate(recipeEntryPrefab, recipeContainer);
                    if (entry == null)
                        continue;

                    entry.AssignRecipe(recipe);
                    entries.Add(entry);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to create StationRecipeEntry for '{id}': {ex.Message}");
                }
            }

            EnsureRecipeEntryClickHandlers(canvas, entries, state);
        }

        private static void EnsureRecipeEntryClickHandlers(
            object canvas,
#if IL2CPPMELON
            Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry> entries,
#else
            List<S1UIStations.StationRecipeEntry> entries,
#endif
            CanvasInjectionState state)
        {
            if (SetSelectedRecipeMethod == null)
            {
                if (!_loggedSetSelectedRecipeMissing)
                {
                    _loggedSetSelectedRecipeMissing = true;
                    Logger.Warning(
                        "[S1API] Chemistry station selection method could not be resolved. Recipe click binding will be skipped.");
                }

                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                S1UIStations.StationRecipeEntry entry = entries[i];
                if (entry == null || entry.Button == null)
                    continue;

                int instanceId = entry.GetInstanceID();
                if (!state.ClickBoundEntryIds.Add(instanceId))
                    continue;

                entry.Button.onClick.AddListener(
                    (UnityAction)(() => SelectRecipeFromClick(canvas, entry)));
            }
        }

        private static void SelectRecipeFromClick(
            object canvas,
            S1UIStations.StationRecipeEntry entry)
        {
            try
            {
                SetSelectedRecipeMethod?.Invoke(canvas, new object[] { entry });
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"[S1API] Chemistry Station recipe click selection failed: {ex.Message}");
            }
        }

        private static S1StationFramework.StationRecipe? FindRecipeById(
#if IL2CPPMELON
            Il2CppSystem.Collections.Generic.List<S1StationFramework.StationRecipe> recipes,
#else
            List<S1StationFramework.StationRecipe> recipes,
#endif
            string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return null;

            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                if (r == null)
                    continue;

                try
                {
                    if (string.Equals(r.RecipeID, recipeId, StringComparison.OrdinalIgnoreCase))
                        return r;
                }
                catch
                {
                    // ignore and continue
                }
            }

            return null;
        }

        private static S1UIStations.StationRecipeEntry? FindEntryByRecipeId(
#if IL2CPPMELON
            Il2CppSystem.Collections.Generic.List<S1UIStations.StationRecipeEntry> entries,
#else
            List<S1UIStations.StationRecipeEntry> entries,
#endif
            string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return null;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null)
                    continue;

                try
                {
                    var r = e.Recipe;
                    if (r == null)
                        continue;

                    if (string.Equals(r.RecipeID, recipeId, StringComparison.OrdinalIgnoreCase))
                        return e;
                }
                catch
                {
                    // ignore and continue
                }
            }

            return null;
        }

        [HarmonyPatch(typeof(S1StationFramework.StationRecipe), "get_RecipeID")]
        [HarmonyPrefix]
        private static bool UseRegisteredRecipeId(
            S1StationFramework.StationRecipe __instance,
            ref string __result)
        {
            if (!ChemistryStationRecipes.TryGetRegisteredId(__instance, out var recipeId))
                return true;

            __result = recipeId;
            return false;
        }

        [HarmonyPatch(typeof(S1StationFramework.StationRecipe), "CalculateQuality")]
        [HarmonyPrefix]
        private static bool UseCustomCalcMethods(S1StationFramework.StationRecipe __instance,
            ref S1ItemFramework.EQuality __result)
        {
            // Exit early out of patch if instance or recipeID is null
            if (__instance == null) return true;
            string? instanceRecipeId;
            try { instanceRecipeId = __instance.RecipeID; } catch { instanceRecipeId = null; }
            if (string.IsNullOrWhiteSpace(instanceRecipeId)) return true;
            var currentAddedRecipe =
                ChemistryStationRecipes.GetAll().FirstOrDefault(r =>
                {
                    try { return string.Equals(r.RecipeID, instanceRecipeId, StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                });
            // If this recipe is not one of ours, exit early from patch
            if (currentAddedRecipe == null) return true;
            // Use default quality calculation for non-absolute methods
            if (currentAddedRecipe.QualityCalculationMethod != QualityCalculationMethod.Absolute) return true;
            var product = currentAddedRecipe.Product.ItemId;
            var itemDefinition = ItemManager.GetDefinition(product);
            if (itemDefinition is not global::S1API.Items.Quality.QualityItemDefinition qualityItemDefinition)
            {
                Logger.Warning($"[S1API] Absolute quality calculation method specified for recipe '{currentAddedRecipe.RecipeID}' but product '{product}' is not a quality item. Falling back to default calculation.");
                return true;
            }

            __result = (S1ItemFramework.EQuality)qualityItemDefinition.DefaultQuality;
            return false;
        }
    }
}
