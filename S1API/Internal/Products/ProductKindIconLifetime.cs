using System;
using UnityEngine;

namespace S1API.Internal.Products
{
    internal static class ProductKindIconLifetime
    {
        private static Func<Sprite, bool> _unityNullEvaluator = IsUnityNull;

        internal static bool IsNullOrDestroyed(Sprite? icon)
        {
            if (ReferenceEquals(icon, null))
                return true;

            return _unityNullEvaluator(icon);
        }

        internal static void SetUnityNullEvaluatorForTesting(
            Func<Sprite, bool>? evaluator)
        {
            _unityNullEvaluator = evaluator ?? IsUnityNull;
        }

        private static bool IsUnityNull(Sprite icon) => icon == null;
    }
}
