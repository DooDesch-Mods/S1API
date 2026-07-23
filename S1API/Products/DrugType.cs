#if (IL2CPPMELON)
using NativeDrugType = Il2CppScheduleOne.Product.EDrugType;
#elif MONOMELON
using NativeDrugType = ScheduleOne.Product.EDrugType;
#endif

namespace S1API.Products
{
    /// <summary>
    /// API-safe product type enum. Mirrors base game drug types.
    /// </summary>
    /// <remarks>
    /// The presence of a value only mirrors the native enum. In particular, <see cref="MDMA"/> and
    /// <see cref="Heroin"/> do not imply that every native product system supports those types.
    /// </remarks>
    public enum DrugType
    {
        Marijuana = 0,
        Methamphetamine = 1,
        Cocaine = 2,
        MDMA = 3,
        Shrooms = 4,
        Heroin = 5
    }

    /// <summary>
    /// INTERNAL: Converts between native and API-safe drug type values.
    /// </summary>
    internal static class DrugTypeExtensions
    {
        /// <summary>
        /// Converts a native drug type to its API-safe equivalent.
        /// </summary>
        /// <param name="drugType">The native drug type.</param>
        /// <returns>The API-safe drug type.</returns>
        internal static DrugType ToAPI(this NativeDrugType drugType)
        {
            return drugType switch
            {
                NativeDrugType.Marijuana => DrugType.Marijuana,
                NativeDrugType.Methamphetamine => DrugType.Methamphetamine,
                NativeDrugType.Cocaine => DrugType.Cocaine,
                NativeDrugType.MDMA => DrugType.MDMA,
                NativeDrugType.Shrooms => DrugType.Shrooms,
                NativeDrugType.Heroin => DrugType.Heroin,
                _ => (DrugType)(int)drugType
            };
        }

        /// <summary>
        /// Converts an API-safe drug type to its native equivalent.
        /// </summary>
        /// <param name="drugType">The API-safe drug type.</param>
        /// <returns>The native drug type.</returns>
        internal static NativeDrugType ToInternal(this DrugType drugType)
        {
            return drugType switch
            {
                DrugType.Marijuana => NativeDrugType.Marijuana,
                DrugType.Methamphetamine => NativeDrugType.Methamphetamine,
                DrugType.Cocaine => NativeDrugType.Cocaine,
                DrugType.MDMA => NativeDrugType.MDMA,
                DrugType.Shrooms => NativeDrugType.Shrooms,
                DrugType.Heroin => NativeDrugType.Heroin,
                _ => (NativeDrugType)(int)drugType
            };
        }
    }
}


