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

public sealed class ProductApiCompatibilityTests
{
    [Fact]
    public void ExistingPublicMemberSignaturesRemainAvailable()
    {
#pragma warning disable CS0618
        var nativeDrugType = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.DrugType));
        var nativeDrugTypes = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.DrugTypes));
#pragma warning restore CS0618
        var wrapperMethod = typeof(ProductDefinitionWrapper).GetMethod(
            nameof(ProductDefinitionWrapper.Wrap),
            new[] { typeof(ProductDefinition) });
        var legacyLookup = typeof(ItemManager).GetMethod(
            nameof(ItemManager.GetItemDefinition),
            new[] { typeof(string) });

        Assert.NotNull(nativeDrugType);
        Assert.Equal(typeof(NativeDrugType), nativeDrugType.PropertyType);
        AssertObsoleteCompatibilityShim(nativeDrugType, "Use PrimaryDrugType instead.");
        Assert.NotNull(nativeDrugTypes);
#if IL2CPPMELON
        Assert.Equal(typeof(IReadOnlyList<NativeDrugTypeContainer>), nativeDrugTypes.PropertyType);
#else
        Assert.Equal(typeof(List<NativeDrugTypeContainer>), nativeDrugTypes.PropertyType);
#endif
        AssertObsoleteCompatibilityShim(nativeDrugTypes, "Use DrugTypeValues instead.");
        Assert.NotNull(wrapperMethod);
        Assert.Equal(typeof(ProductDefinition), wrapperMethod.ReturnType);
        Assert.NotNull(legacyLookup);
        Assert.Equal(typeof(ItemDefinition), legacyLookup.ReturnType);
    }

    [Fact]
    public void NewRuntimeAgnosticMembersAreAdditive()
    {
        var primaryDrugType = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.PrimaryDrugType));
        var drugTypeValues = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.DrugTypeValues));

        Assert.NotNull(primaryDrugType);
        Assert.Equal(typeof(DrugType), primaryDrugType.PropertyType);
        Assert.NotNull(drugTypeValues);
        Assert.Equal(typeof(IReadOnlyList<DrugType>), drugTypeValues.PropertyType);
    }

    private static void AssertObsoleteCompatibilityShim(
        System.Reflection.MemberInfo member,
        string expectedMessage)
    {
        var obsolete = member.GetCustomAttributes(typeof(ObsoleteAttribute), false)
            .Cast<ObsoleteAttribute>()
            .Single();

        Assert.Equal(expectedMessage, obsolete.Message);
        Assert.False(obsolete.IsError);
    }
}
