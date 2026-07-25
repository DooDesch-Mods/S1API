#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using S1API.Entities;
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
                ProductConsumptionContext context)
            {
                Profile = profile;
                Context = context;
            }

            internal ProductConsumptionProfile Profile { get; }

            internal ProductConsumptionContext Context { get; }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object? left, object? right) =>
                ReferenceEquals(left, right);

            public int GetHashCode(object value) =>
                RuntimeHelpers.GetHashCode(value);
        }

        private static readonly Log Logger = new Log("ProductConsumptionProfileDispatcher");
        private static readonly object Gate = new object();
        private static readonly Dictionary<object, ActiveProfile> ActiveProfiles =
            new Dictionary<object, ActiveProfile>(new ReferenceComparer());

        internal static void DispatchPlayer(
            S1Product.ProductItemInstance productInstance,
            object nativePlayer,
            Player player,
            bool isClear)
        {
            Dispatch(
                productInstance,
                nativePlayer,
                player,
                null,
                player.Name,
                isClear);
        }

        internal static void DispatchNpc(
            S1Product.ProductItemInstance productInstance,
            object nativeNpc,
            NPC? npc,
            string targetId,
            bool isClear)
        {
            Dispatch(
                productInstance,
                nativeNpc,
                null,
                npc,
                targetId,
                isClear);
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
                ActiveProfiles.Clear();
        }

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

        internal static bool HasRegisteredProfile(
            S1Product.ProductItemInstance productInstance)
        {
            if (!TryCreateContext(productInstance, null, null, string.Empty, out ProductConsumptionContext? context) ||
                context == null)
            {
                return false;
            }

            return ProductConsumptionProfileRegistrationRegistry.TryResolve(
                context.ProductId,
                context.ProductKind.Id,
                out _);
        }

        private static void Dispatch(
            S1Product.ProductItemInstance productInstance,
            object nativeTarget,
            Player? player,
            NPC? npc,
            string targetId,
            bool isClear)
        {
            if (productInstance == null || nativeTarget == null)
                return;

            if (!TryCreateContext(
                    productInstance,
                    player,
                    npc,
                    targetId,
                    out ProductConsumptionContext? context))
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

            Apply(nativeTarget, context, player != null);
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
            bool isPlayer)
        {
            if (!ProductConsumptionProfileRegistrationRegistry.TryResolve(
                    context.ProductId,
                    context.ProductKind.Id,
                    out ProductConsumptionProfileRegistration? registration) ||
                registration == null)
            {
                return;
            }

            ApplyResolved(nativeTarget, registration.Profile, context, isPlayer);
        }

        private static void ApplyResolved(
            object nativeTarget,
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            bool isPlayer)
        {
            ActiveProfile? previous;
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

                ActiveProfiles[nativeTarget] = new ActiveProfile(profile, context);
            }

            if (previous != null)
                InvokeClear(previous.Profile, previous.Context, isPlayer);

            InvokeApply(profile, context, isPlayer);
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
            }

            InvokeClear(active.Profile, active.Context, isPlayer);
        }

        private static void InvokeApply(
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            bool isPlayer)
        {
            Action<ProductConsumptionContext>? callback = isPlayer
                ? profile.OnPlayerApply
                : profile.OnNpcApply;
            Invoke(callback, profile, context, isPlayer ? "player apply" : "NPC apply");
        }

        private static void InvokeClear(
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            bool isPlayer)
        {
            Action<ProductConsumptionContext>? callback = isPlayer
                ? profile.OnPlayerClear
                : profile.OnNpcClear;
            Invoke(callback, profile, context, isPlayer ? "player clear" : "NPC clear");
        }

        private static void Invoke(
            Action<ProductConsumptionContext>? callback,
            ProductConsumptionProfile profile,
            ProductConsumptionContext context,
            string lifecycle)
        {
            if (callback == null)
                return;

            try
            {
                callback(context);
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
            }
        }
    }
}
