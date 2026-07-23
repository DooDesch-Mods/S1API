using S1API.Items;
using S1API.Items.Ingredient;
using S1API.Products;
using ProductProperty = S1API.Properties.Property;

namespace S1API.CompatibilityCompile
{
    internal static class PublicSyntaxFixture
    {
        internal static void CompileExistingProductAndIngredientSyntax()
        {
            MixIngredientDefinitionBuilder ingredient = MixIngredientItemCreator
                .CreateBuilder()
                .WithBasicInfo(
                    "compat_ingredient",
                    "Compatibility Ingredient",
                    "Compile-only fixture",
                    ItemCategory.Ingredient)
                .WithEffect(ProductProperty.Calming);

            ProductDefinition[] discovered = ProductManager.DiscoveredProducts;
            if (discovered.Length > 0)
            {
                ProductDefinition wrapped = ProductDefinitionWrapper.Wrap(discovered[0]);
                if (wrapped is WeedDefinition weed)
                    _ = weed.GetProperties();
            }

            _ = ingredient;
        }

        internal static WeedDefinition CompileNewWeedBuilderSyntax()
        {
            return WeedItemCreator
                .CreateBuilder("compat.example:calm-kush")
                .WithName("Calm Kush")
                .WithProperties(ProductProperty.Calming, ProductProperty.Munchies)
                .Build();
        }
    }
}
