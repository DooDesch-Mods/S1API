using S1API.Stations;

namespace S1API.Tests.Stations;

/// <summary>
/// Compile-only coverage for Chemistry Station builder syntax that predates explicit recipe IDs.
/// </summary>
internal static class ChemistryStationRecipeApiCompileFixture
{
    internal static ChemistryStationRecipe CompileLegacyBuilderSyntax(
        string productId,
        string ingredientId)
    {
        return new ChemistryStationRecipeBuilder()
            .WithTitle("Legacy Recipe")
            .WithCookTimeMinutes(10)
            .WithCalculationMethod(QualityCalculationMethod.Additive)
            .WithTemperature(250f, 25f)
            .WithProduct(productId, quantity: 5)
            .WithIngredient(ingredientId, quantity: 1)
            .Build();
    }
}
