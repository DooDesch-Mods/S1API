#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

using System;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Configures the four native color channels used by a weed product.
    /// </summary>
    public sealed class WeedAppearanceSettings
    {
        /// <summary>
        /// Creates weed appearance settings.
        /// </summary>
        /// <param name="mainColor">The primary bud color.</param>
        /// <param name="secondaryColor">The secondary bud color.</param>
        /// <param name="leafColor">The leaf color.</param>
        /// <param name="stemColor">The stem color.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when any color is fully clear. The native creator treats a clear channel as an
        /// uninitialized appearance and replaces all supplied colors with generated colors.
        /// </exception>
        public WeedAppearanceSettings(
            Color32 mainColor,
            Color32 secondaryColor,
            Color32 leafColor,
            Color32 stemColor)
        {
            ValidateColor(mainColor, nameof(mainColor));
            ValidateColor(secondaryColor, nameof(secondaryColor));
            ValidateColor(leafColor, nameof(leafColor));
            ValidateColor(stemColor, nameof(stemColor));

            MainColor = mainColor;
            SecondaryColor = secondaryColor;
            LeafColor = leafColor;
            StemColor = stemColor;
        }

        /// <summary>
        /// Gets the primary bud color.
        /// </summary>
        public Color32 MainColor { get; }

        /// <summary>
        /// Gets the secondary bud color.
        /// </summary>
        public Color32 SecondaryColor { get; }

        /// <summary>
        /// Gets the leaf color.
        /// </summary>
        public Color32 LeafColor { get; }

        /// <summary>
        /// Gets the stem color.
        /// </summary>
        public Color32 StemColor { get; }

        /// <summary>
        /// INTERNAL: Converts the runtime-agnostic settings into the active game's native value.
        /// </summary>
        internal S1Product.WeedAppearanceSettings ToNative()
        {
            return new S1Product.WeedAppearanceSettings(
                MainColor,
                SecondaryColor,
                LeafColor,
                StemColor);
        }

        private static void ValidateColor(Color32 color, string parameterName)
        {
            if (color.r == 0 && color.g == 0 && color.b == 0 && color.a == 0)
            {
                throw new ArgumentException(
                    "Weed appearance colors must not be Color.clear.",
                    parameterName);
            }
        }
    }
}
