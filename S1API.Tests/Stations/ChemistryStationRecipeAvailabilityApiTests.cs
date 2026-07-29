using S1API.Stations;

namespace S1API.Tests.Stations;

public sealed class ChemistryStationRecipeAvailabilityApiTests
{
    [Fact]
    public void AvailabilityApiIsAdditiveAndUsesStableSignatures()
    {
        Assert.Equal(
            typeof(ChemistryStationRecipeBuilder),
            typeof(ChemistryStationRecipeBuilder)
                .GetMethod(
                    nameof(ChemistryStationRecipeBuilder.WithInitialAvailability),
                    new[] { typeof(bool), typeof(bool) })!
                .ReturnType);
        Assert.Equal(
            typeof(void),
            typeof(ChemistryStationRecipe)
                .GetMethod(
                    nameof(ChemistryStationRecipe.SetAvailability),
                    new[] { typeof(bool), typeof(bool) })!
                .ReturnType);
        Assert.Equal(
            typeof(bool),
            typeof(ChemistryStationRecipe)
                .GetProperty(nameof(ChemistryStationRecipe.IsDiscovered))!
                .PropertyType);
        Assert.Equal(
            typeof(bool),
            typeof(ChemistryStationRecipe)
                .GetProperty(nameof(ChemistryStationRecipe.IsUnlocked))!
                .PropertyType);
    }
}
