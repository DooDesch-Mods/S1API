using S1API.Entities;

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
}
