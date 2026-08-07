namespace S1API.Tests.Entities;

public sealed class BuildingLookupPolicyTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public void TypedBuildingLookup_DefersUntilTheMapIsReady(
        bool isMenuScene,
        bool isMainSceneReady,
        bool expected)
    {
        Assert.Equal(
            expected,
            global::S1API.Map.Building.ShouldDeferTypedLookup(isMenuScene, isMainSceneReady));
    }
}

public sealed class CustomNpcResidenceSummonPolicyTests
{
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void SummonCompletion_OnlyExitsAnAuthoritativeCustomNpcThatIsInside(
        bool isServer,
        bool isCustomNpc,
        bool isInsideBuilding,
        bool expected)
    {
        Assert.Equal(
            expected,
            global::S1API.Internal.Patches.NPCPatches.ShouldExitCustomNpcAfterSummon(
                isServer,
                isCustomNpc,
                isInsideBuilding));
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void ResidenceReentry_IsSuppressedOnlyDuringAnAuthoritativeCustomNpcSummon(
        bool isServer,
        bool isCustomNpc,
        bool isSummonBehaviourEnabled,
        bool expected)
    {
        Assert.Equal(
            expected,
            global::S1API.Internal.Patches.NPCPatches.ShouldSuppressResidenceReentry(
                isServer,
                isCustomNpc,
                isSummonBehaviourEnabled));
    }

    [Fact]
    public void SummonLogicLookup_UsesGeneratedNamePrefixAndNativeSignature()
    {
        var method = global::S1API.Internal.Patches.NPCPatches.FindSummonLogicMethod(
            typeof(SummonLogicFixture));

        Assert.NotNull(method);
        Assert.Equal(nameof(SummonLogicFixture.RpcLogic___Summon_123), method!.Name);
    }

    private sealed class SummonLogicFixture
    {
        public void RpcLogic___Summon_123(string buildingGuid, int doorIndex, float duration) { }

        public void RpcLogic___Summon_456(string buildingGuid, int doorIndex) { }
    }
}
