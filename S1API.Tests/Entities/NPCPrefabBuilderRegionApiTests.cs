using System.Reflection;
using S1API.Entities;
using S1API.Map;

namespace S1API.Tests.Entities;

public sealed class NPCPrefabBuilderRegionApiTests
{
    [Fact]
    public void WithRegion_IsPublicFluentApiUsingMapRegion()
    {
        var method = typeof(NPCPrefabBuilder).GetMethod(
            nameof(NPCPrefabBuilder.WithRegion),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(Region)],
            modifiers: null);

        Assert.NotNull(method);
        Assert.Equal(typeof(NPCPrefabBuilder), method.ReturnType);
    }
}
