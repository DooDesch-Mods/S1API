using S1API.Items.Storable;
using UnityEngine;

namespace S1API.Tests.Trash;

public sealed class TrashApiCompatibilityTests
{
    [Fact]
    public void TrashManagerExposesPrefabRegistration()
    {
        var method = typeof(global::S1API.Trash.TrashManager).GetMethod(
            nameof(global::S1API.Trash.TrashManager.RegisterTrashPrefab),
            new[] { typeof(string), typeof(GameObject), typeof(bool) });

        Assert.NotNull(method);
        Assert.Equal(typeof(GameObject), method!.ReturnType);
    }

    [Fact]
    public void StorableBuilderExposesDefaultAndExplicitTrashIds()
    {
        Assert.NotNull(
            typeof(StorableItemDefinitionBuilder).GetMethod(
                nameof(StorableItemDefinitionBuilder.WithTrashPrefab),
                new[] { typeof(GameObject), typeof(bool) }));
        Assert.NotNull(
            typeof(StorableItemDefinitionBuilder).GetMethod(
                nameof(StorableItemDefinitionBuilder.WithTrashPrefab),
                new[] { typeof(string), typeof(GameObject), typeof(bool) }));
    }
}
