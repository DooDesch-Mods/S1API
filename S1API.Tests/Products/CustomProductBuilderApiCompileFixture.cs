using S1API.Items;
using S1API.Products;
using S1API.Properties.Interfaces;

namespace S1API.Tests.Products;

internal static class CustomProductBuilderApiCompileFixture
{
    internal static CustomProductDefinition CompileRepresentativeCaller(
        ProductKind productKind,
        ProductDefinition representationTemplate,
        PackagingDefinition packaging,
        PropertyBase property)
    {
        return CustomProductItemCreator
            .CreateBuilder("examplemod:focus-tablet", productKind)
            .WithName("Focus Tablet")
            .WithDescription("A same-mod generic product.")
            .WithProductPrice(175f)
            .WithProperties(property)
            .WithLegalStatus(LegalStatus.Illegal)
            .WithBaseAddictiveness(0.2f)
            .WithDefaultQuality(Quality.Premium)
            .WithValidPackaging(packaging)
            .WithRepresentationsFrom(representationTemplate)
            .WithEffectDurations(120, 180)
            .Build();
    }

    internal static void CompileInstanceAndOptInOperations(
        CustomProductDefinition definition,
        PackagingDefinition packaging)
    {
        _ = definition.ProductKind;
        _ = definition.DefaultQuality;
        _ = definition.BaseAddictiveness;
        _ = definition.PlayerEffectDurationSeconds;
        _ = definition.NpcEffectDurationSeconds;
        _ = definition.ValidPackaging;
        _ = definition.SupportsPackaging(packaging);
        _ = definition.CreateInstance();
        _ = definition.CreateInstance(2, Quality.Heavenly);
        _ = definition.CreatePackagedInstance(1, packaging);
        _ = definition.CreatePackagedInstance(
            1,
            packaging,
            Quality.Standard);
        definition.Discover();
        definition.Discover(listForSale: true);
        definition.SetListed();
        definition.SetListed(listed: false);
    }
}
