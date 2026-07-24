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
        var createInstance = typeof(ProductDefinition).GetMethod(
            nameof(ProductDefinition.CreateInstance),
            new[] { typeof(int) });
        var createPackagedInstance = typeof(ProductDefinition).GetMethod(
            nameof(ProductDefinition.CreatePackagedInstance),
            new[] { typeof(int), typeof(PackagingDefinition) });

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
        Assert.NotNull(createInstance);
        Assert.Equal(typeof(ItemInstance), createInstance.ReturnType);
        Assert.True(createInstance.IsVirtual);
        Assert.False(createInstance.IsAbstract);
        AssertSingleOptionalParameter(createInstance, "quantity", 1);
        Assert.NotNull(createPackagedInstance);
        Assert.Equal(typeof(ProductInstance), createPackagedInstance.ReturnType);
        Assert.False(createPackagedInstance.IsVirtual);
        Assert.Equal(
            new[] { "quantity", "packaging" },
            createPackagedInstance.GetParameters().Select(parameter => parameter.Name));

        Assert.Equal(
            typeof(bool),
            typeof(ProductInstance)
                .GetProperty(nameof(ProductInstance.IsPackaged))!
                .PropertyType);
        Assert.Equal(
            typeof(PackagingDefinition),
            typeof(ProductInstance)
                .GetProperty(nameof(ProductInstance.AppliedPackaging))!
                .PropertyType);
        Assert.Equal(
            typeof(Quality),
            typeof(ProductInstance)
                .GetProperty(nameof(ProductInstance.Quality))!
                .PropertyType);
        Assert.Equal(
            typeof(ProductDefinition),
            typeof(ProductInstance)
                .GetProperty(
                    nameof(ProductInstance.Definition),
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.DeclaredOnly)!
                .PropertyType);
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

    [Fact]
    public void GenericCustomProductApiIsAdditiveAndKeepsOptInDefaults()
    {
        var createBuilder = typeof(CustomProductItemCreator).GetMethod(
            nameof(CustomProductItemCreator.CreateBuilder),
            new[] { typeof(string), typeof(ProductKind) });
        var constructor = typeof(CustomProductDefinitionBuilder).GetConstructor(
            new[] { typeof(string), typeof(ProductKind) });
        var createDefault = typeof(CustomProductDefinition).GetMethod(
            nameof(CustomProductDefinition.CreateInstance),
            new[] { typeof(int) });
        var discover = typeof(CustomProductDefinition).GetMethod(
            nameof(CustomProductDefinition.Discover),
            new[] { typeof(bool) });
        var setListed = typeof(CustomProductDefinition).GetMethod(
            nameof(CustomProductDefinition.SetListed),
            new[] { typeof(bool) });

        Assert.NotNull(createBuilder);
        Assert.Equal(
            typeof(CustomProductDefinitionBuilder),
            createBuilder.ReturnType);
        Assert.NotNull(constructor);
        Assert.True(typeof(CustomProductDefinition).IsSealed);
        Assert.True(
            typeof(ProductDefinition).IsAssignableFrom(
                typeof(CustomProductDefinition)));
        Assert.NotNull(createDefault);
        Assert.Equal(typeof(ItemInstance), createDefault.ReturnType);
        Assert.True(createDefault.IsVirtual);
        AssertSingleOptionalParameter(createDefault, "quantity", 1);
        Assert.NotNull(discover);
        AssertSingleOptionalParameter(discover, "listForSale", false);
        Assert.NotNull(setListed);
        AssertSingleOptionalParameter(setListed, "listed", true);
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

    private static void AssertSingleOptionalParameter(
        System.Reflection.MethodInfo method,
        string expectedName,
        object expectedDefault)
    {
        System.Reflection.ParameterInfo parameter =
            Assert.Single(method.GetParameters());
        Assert.Equal(expectedName, parameter.Name);
        Assert.True(parameter.IsOptional);
        Assert.Equal(expectedDefault, parameter.DefaultValue);
    }
}
