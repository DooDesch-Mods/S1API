using S1API.Entities;
using System.Reflection;

namespace S1API.Tests.Entities;

public sealed class NPCRelationshipEventTests
{
    [Theory]
    [InlineData("OnUnlocked", "onUnlocked")]
    [InlineData("OnRelationshipChange", "onRelationshipChange")]
    public void ResolvesCanonicalNativeRelationshipEventMembers(
        string canonicalName,
        string legacyName)
    {
        var member =
            NPCRelationship.ResolveNativeEventMember(canonicalName, legacyName);

        Assert.NotNull(member);
        Assert.Equal(canonicalName, member.Name);
    }

    [Fact]
    public void CanonicalPropertyTakesPrecedenceOverLegacyField()
    {
        MemberInfo? member = NPCRelationship.ResolveNativeEventMember(
            typeof(CanonicalPropertyAndLegacyFieldFixture),
            "OnUnlocked",
            "onUnlocked");

        PropertyInfo property = Assert.IsAssignableFrom<PropertyInfo>(member);
        Assert.Equal("OnUnlocked", property.Name);
    }

    [Fact]
    public void LegacyFieldIsUsedWhenCanonicalMemberDoesNotExist()
    {
        MemberInfo? member = NPCRelationship.ResolveNativeEventMember(
            typeof(LegacyFieldOnlyFixture),
            "OnUnlocked",
            "onUnlocked");

        FieldInfo field = Assert.IsAssignableFrom<FieldInfo>(member);
        Assert.Equal("onUnlocked", field.Name);
    }

    private sealed class CanonicalPropertyAndLegacyFieldFixture
    {
        public Action? onUnlocked = null;

        public Action? OnUnlocked { get; set; }
    }

    private sealed class LegacyFieldOnlyFixture
    {
        public Action? onUnlocked = null;
    }
}
