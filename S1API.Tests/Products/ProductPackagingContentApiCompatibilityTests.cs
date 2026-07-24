using S1API.Products;
using UnityEngine;

namespace S1API.Tests.Products;

public sealed class ProductPackagingContentApiCompatibilityTests
{
    [Fact]
    public void PackagingContentApiIsAdditiveAndRuntimeAgnostic()
    {
        Assert.True(typeof(ProductPackagingContentProfile).IsSealed);
        Assert.True(typeof(ProductPackagingContentProfileBuilder).IsSealed);
        Assert.True(typeof(ProductPackagingContentProfileRegistry).IsAbstract);
        Assert.True(typeof(ProductPackagingContentProfileRegistry).IsSealed);

        System.Reflection.MethodInfo withContent =
            typeof(ProductPackagingContentProfileBuilder).GetMethod(
                nameof(ProductPackagingContentProfileBuilder.WithContent),
                new[] { typeof(Func<GameObject>) })!;
        System.Reflection.MethodInfo addPlacement =
            typeof(ProductPackagingContentProfileBuilder).GetMethod(
                nameof(ProductPackagingContentProfileBuilder.AddPlacement),
                new[] { typeof(ProductPresentationTransform) })!;
        System.Reflection.MethodInfo addPlacements =
            typeof(ProductPackagingContentProfileBuilder).GetMethod(
                nameof(ProductPackagingContentProfileBuilder.AddPlacements),
                new[] { typeof(ProductPresentationTransform[]) })!;
        System.Reflection.MethodInfo register =
            typeof(ProductPackagingContentProfileRegistry).GetMethod(
                nameof(ProductPackagingContentProfileRegistry.Register),
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(string),
                    typeof(ProductPackagingContentProfile)
                })!;

        Assert.NotNull(withContent);
        Assert.NotNull(addPlacement);
        Assert.NotNull(addPlacements);
        Assert.NotNull(register);
        Assert.Equal(typeof(ProductPackagingContentProfileBuilder), withContent.ReturnType);
        Assert.Equal(typeof(ProductPackagingContentProfileBuilder), addPlacement.ReturnType);
        Assert.Equal(typeof(ProductPackagingContentProfileBuilder), addPlacements.ReturnType);
        Assert.Equal(typeof(ProductPackagingContentProfile), register.ReturnType);
    }
}
