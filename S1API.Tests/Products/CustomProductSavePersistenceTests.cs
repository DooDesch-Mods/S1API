using System.Reflection;
using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductSavePersistenceTests
{
    [Fact]
    public void DescriptorAcceptsVanillaRepresentationTemplateId()
    {
        CustomProductSaveDescriptorData data = CreateDescriptor("cocaine");
        MethodInfo validate = typeof(CustomProductSavePersistence).GetMethod(
            "TryValidate",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments = { data, null };

        bool valid = (bool)validate.Invoke(null, arguments)!;

        Assert.True(valid);
        Assert.NotNull(arguments[1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DescriptorRejectsEmptyRepresentationTemplateId(string templateId)
    {
        CustomProductSaveDescriptorData data = CreateDescriptor(templateId);
        MethodInfo validate = typeof(CustomProductSavePersistence).GetMethod(
            "TryValidate",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments = { data, null };

        bool valid = (bool)validate.Invoke(null, arguments)!;

        Assert.False(valid);
        Assert.Null(arguments[1]);
    }

    private static CustomProductSaveDescriptorData CreateDescriptor(
        string representationTemplateId)
    {
        return new CustomProductSaveDescriptorData
        {
            ProductId = "example.mod:products/tablet",
            OwnerId = "example.mod",
            ProductName = "Tablet",
            Description = "A custom tablet.",
            InitialPrice = 100f,
            LegalStatus = 1,
            BaseAddictiveness = 0.25f,
            DefaultQuality = 3,
            ProductKindId = "example.mod:tablet",
            CompatibilityDrugType = 2,
            RepresentationTemplateId = representationTemplateId,
            PlayerEffectDurationSeconds = 120,
            NpcEffectDurationSeconds = 180,
            PropertyIds = new[] { "energizing" },
            PackagingIds = new[] { "baggie", "jar" },
            ProviderId = "example.mod:products",
            ProviderVersion = 1,
            ProviderData = "tablet",
        };
    }
}
