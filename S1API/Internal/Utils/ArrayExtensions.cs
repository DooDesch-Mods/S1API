#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

using System;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Extensions for Arrays.
    /// This class is intended for internal API use only. Mod developers should use <c>S1API.Utils.ArrayExtensions</c> instead.
    /// </summary>
    internal static class ArrayExtensions
    {
        /// <summary>
        /// Add's an item to an existing array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        internal static T[] AddItemToArray<T>(this T[]? array, T item)
        {
            array ??= Array.Empty<T>();

            Array.Resize(ref array, array.Length + 1);
            array[^1] = item;

            return array;
        }

#if IL2CPPMELON
        /// <summary>
        /// Add's an item to an existing <see cref="Il2CppReferenceArray{T}"/> instance.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="itemsToAdd"></param>
        /// <typeparam name="T"></typeparam>
        internal static Il2CppReferenceArray<T> AddItemToArray<T>(this Il2CppReferenceArray<T>? array, params T[]? itemsToAdd) where T : Il2CppSystem.Object
        {
            var originalLength = array?.Length ?? 0;
            var additionalLength = itemsToAdd?.Length ?? 0;
            var newLength = originalLength + additionalLength;
            var source = array;
            var additions = itemsToAdd;

            var result = new Il2CppReferenceArray<T>(newLength);
            if (source != null)
            {
                for (var i = 0; i < originalLength; i++)
                    result[i] = source[i]!;
            }

            if (additions != null)
            {
                for (var i = 0; i < additionalLength; i++)
                    result[originalLength + i] = additions[i]!;
            }

            return result;
        }
#endif
    }
}
