#if (IL2CPPMELON)
using NativeDrugType = Il2CppScheduleOne.Product.EDrugType;
using NativeDrugTypeContainer = Il2CppScheduleOne.Product.DrugTypeContainer;
#elif MONOMELON
using NativeDrugType = ScheduleOne.Product.EDrugType;
using NativeDrugTypeContainer = ScheduleOne.Product.DrugTypeContainer;
#endif

using S1API.Items;
using S1API.Products;
using S1API.Rendering;

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

    [Fact]
    public void ProductPresentationProfileApiIsAdditiveAndRuntimeAgnostic()
    {
        Assert.True(typeof(ProductPresentationProfile).IsSealed);
        Assert.True(typeof(ProductPresentationProfileBuilder).IsSealed);
        Assert.True(typeof(ProductPresentationTransform).IsSealed);
        Assert.True(typeof(ProductPresentationProfileRegistry).IsAbstract);
        Assert.True(typeof(ProductPresentationProfileRegistry).IsSealed);
        Assert.Equal(
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6
            },
            Enum.GetValues<ProductPresentationContext>()
                .Select(value => (int)value));

        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithLooseVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithStoredVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithHeldVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithStationVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(
                ProductPresentationProfileBuilder
                    .WithFunctionalProductVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithIcon),
            typeof(Func<UnityEngine.Sprite>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithConsumptionPrefab),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithLooseVisual),
            typeof(Func<UnityEngine.GameObject>),
            typeof(ProductPresentationTransform));

        System.Reflection.ConstructorInfo? presentationTransform =
            typeof(ProductPresentationTransform).GetConstructor(
                new[]
                {
                    typeof(UnityEngine.Vector3),
                    typeof(UnityEngine.Vector3),
                    typeof(UnityEngine.Vector3)
                });
        Assert.NotNull(presentationTransform);
        Assert.NotNull(
            typeof(ProductPresentationTransform).GetProperty(
                nameof(ProductPresentationTransform.LocalPosition)));
        Assert.NotNull(
            typeof(ProductPresentationTransform).GetProperty(
                nameof(ProductPresentationTransform.LocalEulerAngles)));
        Assert.NotNull(
            typeof(ProductPresentationTransform).GetProperty(
                nameof(ProductPresentationTransform.LocalScale)));

        System.Reflection.MethodInfo generatedIcon =
            typeof(ProductPresentationProfileBuilder).GetMethod(
                nameof(
                    ProductPresentationProfileBuilder
                        .WithGeneratedIconFromLooseVisual),
                new[] { typeof(int) })!;
        Assert.NotNull(generatedIcon);
        AssertSingleOptionalParameter(generatedIcon, "size", 512);
        Assert.NotNull(
            typeof(ProductPresentationProfileBuilder).GetMethod(
                nameof(
                    ProductPresentationProfileBuilder
                        .WithGeneratedIconFromLooseVisual),
                new[]
                {
                    typeof(int),
                    typeof(bool),
                    typeof(float)
                }));

        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIcon),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool)
                }));
        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIcon),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool),
                    typeof(bool),
                    typeof(float)
                }));
        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIconSprite),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool)
                }));
        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIconSprite),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool),
                    typeof(bool),
                    typeof(float)
                }));

        System.Reflection.MethodInfo registerProduct =
            typeof(ProductPresentationProfileRegistry).GetMethod(
                nameof(ProductPresentationProfileRegistry.RegisterForProduct),
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(ProductPresentationProfile)
                })!;
        System.Reflection.MethodInfo registerKind =
            typeof(ProductPresentationProfileRegistry).GetMethod(
                nameof(
                    ProductPresentationProfileRegistry.RegisterForProductKind),
                new[]
                {
                    typeof(string),
                    typeof(ProductKind),
                    typeof(ProductPresentationProfile)
                })!;
        Assert.NotNull(registerProduct);
        Assert.NotNull(registerKind);
        Assert.Equal(typeof(ProductPresentationProfile), registerProduct.ReturnType);
        Assert.Equal(typeof(ProductPresentationProfile), registerKind.ReturnType);
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

    private static void AssertBuilderMethod(
        string name,
        params Type[] parameterTypes)
    {
        System.Reflection.MethodInfo? method =
            typeof(ProductPresentationProfileBuilder).GetMethod(
                name,
                parameterTypes);
        Assert.NotNull(method);
        Assert.Equal(typeof(ProductPresentationProfileBuilder), method.ReturnType);
    }
}
