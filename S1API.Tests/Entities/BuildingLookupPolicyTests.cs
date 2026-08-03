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

public sealed class CustomNpcDoorSelectionPolicyTests
{
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void DoorSelection_OnlyExitsAnAuthoritativeCustomNpcThatIsInside(
        bool isServer,
        bool isCustomNpc,
        bool isInsideBuilding,
        bool expected)
    {
        Assert.Equal(
            expected,
            global::S1API.Internal.Patches.NPCPatches.ShouldExitCustomNpcAfterDoorSelection(
                isServer,
                isCustomNpc,
                isInsideBuilding));
    }
}
