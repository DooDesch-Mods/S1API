using System.Reflection;
using S1API.Items.Buildable;
using UnityEngine;

namespace S1API.Tests.Items;

public sealed class FurnitureApiCompatibilityTests
{
    [Fact]
    public void FurnitureBuilderExposesRuntimeAgnosticFluentSurface()
    {
        MethodInfo? createBuilder = typeof(FurnitureCreator).GetMethod(
            nameof(FurnitureCreator.CreateBuilder),
            Type.EmptyTypes);

        Assert.NotNull(createBuilder);
        Assert.Equal(typeof(FurnitureDefinitionBuilder), createBuilder!.ReturnType);
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithBasicInfo), typeof(string), typeof(string), typeof(string));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithModel), typeof(GameObject));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithPlacement), typeof(FurniturePlacementMode));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithFootprint), typeof(int), typeof(int));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithSurfacePlacement), typeof(FurnitureSurfaceType), typeof(bool));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithBuildSound), typeof(BuildSoundType));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithPricing), typeof(float), typeof(float));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithStackLimit), typeof(int));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithIcon), typeof(Sprite));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithGeneratedIcon), typeof(int));

        MethodInfo? build = typeof(FurnitureDefinitionBuilder).GetMethod(
            nameof(FurnitureDefinitionBuilder.Build),
            Type.EmptyTypes);
        Assert.NotNull(build);
        Assert.Equal(typeof(BuildableItemDefinition), build!.ReturnType);
    }

    [Fact]
    public void PlacementEnumsExposeOnlySupportedNativeFamilies()
    {
        Assert.Equal(
            new[] { FurniturePlacementMode.Grid, FurniturePlacementMode.Surface },
            Enum.GetValues<FurniturePlacementMode>());
        Assert.Equal(
            FurnitureSurfaceType.Wall | FurnitureSurfaceType.Roof,
            FurnitureSurfaceType.All);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void FootprintRejectsNonPositiveDimensions(int width, int depth)
    {
        FurnitureDefinitionBuilder builder = FurnitureCreator.CreateBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithFootprint(width, depth));
    }

    [Theory]
    [InlineData(FurnitureSurfaceType.None)]
    [InlineData((FurnitureSurfaceType)8)]
    public void SurfacePlacementRejectsEmptyOrUnknownFlags(FurnitureSurfaceType surfaceTypes)
    {
        FurnitureDefinitionBuilder builder = FurnitureCreator.CreateBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithSurfacePlacement(surfaceTypes));
    }

    private static void AssertFluent(string name, params Type[] parameterTypes)
    {
        MethodInfo? method = typeof(FurnitureDefinitionBuilder).GetMethod(name, parameterTypes);
        Assert.NotNull(method);
        Assert.Equal(typeof(FurnitureDefinitionBuilder), method!.ReturnType);
    }
}
