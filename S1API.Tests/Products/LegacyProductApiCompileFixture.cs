#if (IL2CPPMELON)
using NativeDrugType = Il2CppScheduleOne.Product.EDrugType;
using NativeDrugTypeContainer = Il2CppScheduleOne.Product.DrugTypeContainer;
#elif MONOMELON
using NativeDrugType = ScheduleOne.Product.EDrugType;
using NativeDrugTypeContainer = ScheduleOne.Product.DrugTypeContainer;
#endif

using S1API.Items;
using S1API.Products;

namespace S1API.Tests.Products;

/// <summary>
/// Compile-only coverage for the product API syntax that predates the runtime-agnostic accessors.
/// </summary>
internal static class LegacyProductApiCompileFixture
{
    internal static void CompileLegacyProductAccess(
        ProductDefinition definition,
        ProductInstance instance)
    {
#pragma warning disable CS0618
        ItemDefinition itemDefinition = definition;
        NativeDrugType nativePrimaryType = definition.DrugType;
#if IL2CPPMELON
        IReadOnlyList<NativeDrugTypeContainer> nativeTypes = definition.DrugTypes;
#else
        List<NativeDrugTypeContainer> nativeTypes = definition.DrugTypes;
#endif
        ProductDefinition wrapped = ProductDefinitionWrapper.Wrap(definition);
        ProductDefinition instanceDefinition = instance.Definition;

        ItemDefinition? legacyLookup = ItemManager.GetItemDefinition(definition.ID);
#pragma warning restore CS0618

        _ = itemDefinition;
        _ = nativePrimaryType;
        _ = nativeTypes;
        _ = wrapped;
        _ = instanceDefinition;
        _ = legacyLookup;
    }
}
