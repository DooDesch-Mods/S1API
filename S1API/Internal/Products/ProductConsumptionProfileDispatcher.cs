#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using S1API.Entities;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Dispatches intrinsic custom-product consumption behavior.</summary>
    internal static class ProductConsumptionProfileDispatcher
    {
        private sealed class ActiveProfile
        {
            internal ActiveProfile(
                ProductConsumptionProfile profile,
                ProductConsumptionContext context,
                bool isPlayer)
            {
                Profile = profile;
                Context = context;
                IsPlayer = isPlayer;
            }

            internal ProductConsumptionProfile Profile { get; }

            internal ProductConsumptionContext Context { get; }

            internal bool IsPlayer { get; }

            internal bool IsActive { get; set; } = true;
        }

        private static readonly Log Logger = new Log("ProductConsumptionProfileDispatcher");
        private static readonly object Gate = new object();
        private static ConditionalWeakTable<object, ActiveProfile> ActiveProfiles = new();
        private static readonly List<WeakReference<ActiveProfile>> ActiveProfileReferences = new();
        private static bool _lifecycleHooksRegistered;

        internal static void DispatchPlayer(
            S1Product.ProductItemInstance productInstance,
            object nativePlayer,
            Player player,
            bool isClear,
            ProductConsumptionContext? resolvedContext = null,
            ProductConsumptionProfileRegistration? resolvedRegistration = null)
        {
            Dispatch(
                productInstance,
                nativePlayer,
                player,
                null,
                GetPlayerTargetId(player.S1Player),
                isClear,
                resolvedContext,
                resolvedRegistration);
        }

        internal static void DispatchNpc(
            S1Product.ProductItemInstance productInstance,
            object nativeNpc,
            NPC? npc,
            string targetId,
            bool isClear,
            ProductConsumptionContext? resolvedContext = null,
            ProductConsumptionProfileRegistration? resolvedRegistration = null)
        {
            Dispatch(
                productInstance,
                nativeNpc,
                null,
                npc,
                targetId,
                isClear,
                resolvedContext,
                resolvedRegistration);
        }

        internal static void ResetForTesting()
        {
            ResetActiveProfiles();
        }

        internal static string GetPlayerTargetIdForTesting(string playerCode) =>
            GetPlayerTargetId(playerCode);

        internal static void DispatchForTesting(
            object nativeTarget,
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            bool isPlayer,
            bool isClear,
            bool isLocalPlayer = true)
        {
            if (isPlayer && !isLocalPlayer)
                return;

            if (isClear)
                Clear(nativeTarget, context, isPlayer);
            else
                ApplyResolved(nativeTarget, profile, context, isPlayer);
        }

        internal static bool TryResolveRegisteredContext(
            S1Product.ProductItemInstance productInstance,
            Player? player,
            NPC? npc,
            string targetId,
            out ProductConsumptionContext? context,
            out ProductConsumptionProfileRegistration? registration)
        {
            registration = null;
            if (!TryCreateContext(productInstance, player, npc, targetId, out context) || context == null)
            {
                return false;
            }

            return ProductConsumptionProfileRegistrationRegistry.TryResolve(
                context.ProductId,
                context.ProductKind.Id,
                out registration);
        }

        private static void Dispatch(
            S1Product.ProductItemInstance productInstance,
            object nativeTarget,
            Player? player,
            NPC? npc,
            string targetId,
            bool isClear,
            ProductConsumptionContext? resolvedContext,
            ProductConsumptionProfileRegistration? resolvedRegistration)
        {
            if (productInstance == null || nativeTarget == null)
                return;

            ProductConsumptionContext? context = resolvedContext;
            if (context == null && !TryCreateContext(
                productInstance,
                player,
                npc,
                targetId,
                out context))
            {
                return;
            }

            if (context == null)
                return;

            if (isClear)
            {
                Clear(nativeTarget, context, player != null);
                return;
            }

            Apply(nativeTarget, context, player != null, resolvedRegistration);
        }

        private static bool TryCreateContext(
            S1Product.ProductItemInstance productInstance,
            Player? player,
            NPC? npc,
            string targetId,
            out ProductConsumptionContext? context)
        {
            context = null;
            try
            {
                var product = new ProductInstance(productInstance);
                if (!(product.Definition is CustomProductDefinition definition))
                    return false;

                context = new ProductConsumptionContext(
                    product,
                    definition,
                    player,
                    npc,
                    targetId ?? string.Empty);
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "Could not resolve a custom product consumption context: " +
                    exception.GetType().Name);
                return false;
            }
        }

        private static void Apply(
            object nativeTarget,
            ProductConsumptionContext context,
            bool isPlayer,
            ProductConsumptionProfileRegistration? resolvedRegistration)
        {
            ProductConsumptionProfileRegistration? registration = resolvedRegistration;
            if (registration == null &&
                !ProductConsumptionProfileRegistrationRegistry.TryResolve(
                    context.ProductId,
                    context.ProductKind.Id,
                    out registration))
            {
                return;
            }

            if (registration == null)
                return;

            ApplyResolved(nativeTarget, registration.Profile, context, isPlayer);
        }

        private static void ApplyResolved(
            object nativeTarget,
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            bool isPlayer)
        {
            EnsureLifecycleHooks();
            ActiveProfile? previous;
            var active = new ActiveProfile(profile, context, isPlayer);
            lock (Gate)
            {
                if (ActiveProfiles.TryGetValue(nativeTarget, out previous) &&
                    string.Equals(
                        previous.Context.ProductId,
                        context.ProductId,
                        StringComparison.OrdinalIgnoreCase) &&
                    ReferenceEquals(previous.Profile, profile))
                {
                    return;
                }

                if (previous != null)
                {
                    previous.IsActive = false;
                    ActiveProfiles.Remove(nativeTarget);
                }

                ActiveProfiles.Add(nativeTarget, active);
                ActiveProfileReferences.Add(new WeakReference<ActiveProfile>(active));
            }

            if (previous != null)
                InvokeClear(previous.Profile, previous.Context, previous.IsPlayer);

            if (!InvokeApply(profile, context, isPlayer))
            {
                RemoveActiveProfile(nativeTarget, active);
                InvokeClear(profile, context, isPlayer);
            }
        }

        private static void Clear(
            object nativeTarget,
            ProductConsumptionContext context,
            bool isPlayer)
        {
            ActiveProfile? active;
            lock (Gate)
            {
                if (!ActiveProfiles.TryGetValue(nativeTarget, out active) ||
                    !string.Equals(
                        active.Context.ProductId,
                        context.ProductId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                ActiveProfiles.Remove(nativeTarget);
                active.IsActive = false;
            }

            InvokeClear(active.Profile, active.Context, active.IsPlayer);
        }

        private static bool InvokeApply(
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            bool isPlayer)
        {
            Action<ProductConsumptionContext>? callback = isPlayer
                ? profile.OnPlayerApply
                : profile.OnNpcApply;
            return Invoke(callback, profile, context, isPlayer ? "player apply" : "NPC apply");
        }

        private static bool InvokeClear(
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            bool isPlayer)
        {
            Action<ProductConsumptionContext>? callback = isPlayer
                ? profile.OnPlayerClear
                : profile.OnNpcClear;
            return Invoke(callback, profile, context, isPlayer ? "player clear" : "NPC clear");
        }

        private static bool Invoke(
            Action<ProductConsumptionContext>? callback,
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            string lifecycle)
        {
            if (callback == null)
                return true;

            try
            {
                callback(context);
                return true;
            }
            catch (Exception exception)
            {
                try
                {
                    Logger.Error(
                        "Consumption profile '" + profile.ProviderId + "' " + lifecycle +
                        " callback failed for product '" + context.ProductId + "': " +
                        exception.GetType().Name);
                }
                catch
                {
                }

                return false;
            }
        }

        private static string GetPlayerTargetId(S1PlayerScripts.Player player) =>
            GetPlayerTargetId(player?.PlayerCode);

        private static string GetPlayerTargetId(string? playerCode) =>
            playerCode ?? string.Empty;

        private static void EnsureLifecycleHooks()
        {
            lock (Gate)
            {
                if (_lifecycleHooksRegistered)
                    return;

                GameLifecycle.OnPreLoad += ResetActiveProfiles;
                GameLifecycle.OnPreSceneChange += ResetActiveProfiles;
                Player.PlayerDespawned += OnPlayerDespawned;
                _lifecycleHooksRegistered = true;
            }
        }

        private static void OnPlayerDespawned(Player player)
        {
            if (player == null)
                return;

            RemoveActiveProfile(player.S1Player);
        }

        private static void RemoveActiveProfile(object nativeTarget, ActiveProfile expected)
        {
            lock (Gate)
            {
                if (ActiveProfiles.TryGetValue(nativeTarget, out ActiveProfile? current) &&
                    ReferenceEquals(current, expected))
                {
                    current.IsActive = false;
                    ActiveProfiles.Remove(nativeTarget);
                }
            }
        }

        private static void RemoveActiveProfile(object nativeTarget)
        {
            ActiveProfile? active;
            lock (Gate)
            {
                if (!ActiveProfiles.TryGetValue(nativeTarget, out active))
                    return;

                active.IsActive = false;
                ActiveProfiles.Remove(nativeTarget);
            }

            InvokeClear(active.Profile, active.Context, active.IsPlayer);
        }

        private static void ResetActiveProfiles()
        {
            var activeProfiles = new List<ActiveProfile>();
            lock (Gate)
            {
                for (var index = ActiveProfileReferences.Count - 1; index >= 0; index--)
                {
                    if (!ActiveProfileReferences[index].TryGetTarget(out ActiveProfile? active))
                    {
                        ActiveProfileReferences.RemoveAt(index);
                        continue;
                    }

                    if (active.IsActive)
                    {
                        active.IsActive = false;
                        activeProfiles.Add(active);
                    }
                }

                ActiveProfiles = new ConditionalWeakTable<object, ActiveProfile>();
                ActiveProfileReferences.Clear();
            }

            foreach (ActiveProfile active in activeProfiles)
                InvokeClear(active.Profile, active.Context, active.IsPlayer);
        }
    }
}
