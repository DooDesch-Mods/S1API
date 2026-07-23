using S1API.Stations;

namespace S1API.Tests.Stations;

public sealed class ChemistryStationRecipeIdTests
{
    [Fact]
    public void ResolvePreservesLegacyDerivedIdWhenExplicitIdIsOmitted()
    {
        string recipeId = ChemistryStationRecipeId.Resolve(
            hasExplicitId: false,
            explicitId: "ignored:explicit-id",
            productQuantity: 5,
            productItemId: "mymod_product");

        Assert.Equal("5xmymod_product", recipeId);
    }

    [Fact]
    public void ResolveAllowsDistinctExplicitIdsForTheSameProductAndQuantity()
    {
        string firstId = ChemistryStationRecipeId.Resolve(
            hasExplicitId: true,
            explicitId: "my-mod:standard-route",
            productQuantity: 2,
            productItemId: "mymod_coolant");
        string secondId = ChemistryStationRecipeId.Resolve(
            hasExplicitId: true,
            explicitId: "my-mod:reclaimed-route",
            productQuantity: 2,
            productItemId: "mymod_coolant");

        Assert.Equal("my-mod:standard-route", firstId);
        Assert.Equal("my-mod:reclaimed-route", secondId);
        Assert.NotEqual(firstId, secondId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("missing-namespace")]
    [InlineData(":missing-namespace")]
    [InlineData("missing-recipe:")]
    [InlineData("too:many:segments")]
    [InlineData("my-mod:contains whitespace")]
    [InlineData("my-mod:contains/slash")]
    [InlineData("my-mod:non-ascii-\u00E9")]
    [InlineData("---:___")]
    public void ResolveRejectsInvalidExplicitIds(string? explicitId)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ChemistryStationRecipeId.Resolve(
                hasExplicitId: true,
                explicitId,
                productQuantity: 1,
                productItemId: "mymod_product"));

        Assert.Contains("Chemistry Station recipe ID", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ComparerTreatsCaseVariantsAsAFirstRegistrationWinsCollision()
    {
        var registrations = new Dictionary<string, string>(ChemistryStationRecipeId.Comparer);

        bool firstRegistered = registrations.TryAdd("My-Mod:Alternate-Route", "first");
        bool secondRegistered = registrations.TryAdd("my-mod:alternate-route", "second");

        Assert.True(firstRegistered);
        Assert.False(secondRegistered);
        Assert.Equal("first", registrations["MY-MOD:ALTERNATE-ROUTE"]);
    }

    [Fact]
    public void ResolveTrimsButOtherwisePreservesExplicitId()
    {
        string recipeId = ChemistryStationRecipeId.Resolve(
            hasExplicitId: true,
            explicitId: "  My.Mod:Alternate_Route-2  ",
            productQuantity: 1,
            productItemId: "mymod_product");

        Assert.Equal("My.Mod:Alternate_Route-2", recipeId);
    }

    [Fact]
    public void PreExistingBuilderSyntaxRemainsSourceCompatible()
    {
        static ChemistryStationRecipe BuildWithLegacySyntax(string productId, string ingredientId) =>
            new ChemistryStationRecipeBuilder()
                .WithTitle("Legacy Recipe")
                .WithCookTimeMinutes(10)
                .WithCalculationMethod(QualityCalculationMethod.Additive)
                .WithTemperature(250f, 25f)
                .WithProduct(productId, quantity: 5)
                .WithIngredient(ingredientId, quantity: 1)
                .Build();

        Func<string, string, ChemistryStationRecipe> legacyBuilderSyntax = BuildWithLegacySyntax;

        Assert.NotNull(legacyBuilderSyntax);
    }
}
