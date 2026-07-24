using System;
using S1API.Products;
using Xunit;

namespace S1API.Tests.Products
{
    public sealed class ProductMixingProfileTests
    {
        [Fact]
        public void ProfileRegistrationRetainsKindAndInvokesDeterministicFactory()
        {
            ProductKind kind = CreateKind();
            Func<ProductMixingOutput, ProductMixingOutputDefinition> factory = input =>
                new ProductMixingOutputDefinition(input.MixName + " Output", input.SourceKind, input.SourcePrice + 10f);

            ProductMixingProfile profile = new ProductMixingProfileBuilder(kind)
                .WithMixerMap(ProductMixingMap.Marijuana)
                .WithOutputFactory(factory)
                .Build();

            Assert.Same(profile, ProductMixingProfiles.Get(kind));
            Assert.Equal(ProductMixingMap.Marijuana, profile.MixerMap);
            ProductMixingOutputDefinition output = profile.OutputFactory(
                new ProductMixingOutput("example:output", "Named Mix", "example:source", kind, 20f));
            Assert.Equal("Named Mix Output", output.Name);
            Assert.Same(kind, output.ProductKind);
            Assert.Equal(30f, output.Price);
        }

        [Fact]
        public void MissingOutputFactoryFailsBeforeRegistration()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => new ProductMixingProfileBuilder(CreateKind()).Build());

            Assert.Contains("WithOutputFactory", exception.Message);
        }

        [Fact]
        public void OutputDefinitionRejectsInvalidPriceAndMissingKind()
        {
            ProductKind kind = CreateKind();

            Assert.Throws<ArgumentNullException>(
                () => new ProductMixingOutputDefinition("Output", null!, 5f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ProductMixingOutputDefinition("Output", kind, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ProductMixingOutputDefinition("Output", kind, 1000f));
        }

        [Fact]
        public void MismatchedMapCannotEnterAnUnsupportedNativeFamily()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => new ProductMixingProfileBuilder(CreateKind())
                    .WithMixerMap(ProductMixingMap.Cocaine)
                    .WithOutputFactory(input =>
                        new ProductMixingOutputDefinition(input.MixName, input.SourceKind, input.SourcePrice))
                    .Build());

            Assert.Contains("must match", exception.Message);
        }

        private static ProductKind CreateKind()
        {
            return new ProductKindBuilder("mixingtests:" + Guid.NewGuid().ToString("N"))
                .WithCompatibilityDrugType(DrugType.Marijuana)
                .Build();
        }
    }
}
