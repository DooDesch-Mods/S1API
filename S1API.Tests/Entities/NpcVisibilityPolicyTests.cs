using S1API.Entities;

namespace S1API.Tests.Entities;

public sealed class NpcVisibilityPolicyTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void ResolveSpawnVisibility_PreservesNativeSupplierVisibility(
        bool isPhysical,
        bool isSupplier,
        bool isSupplierMeeting,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPC.ResolveSpawnVisibility(
                isPhysical,
                isSupplier,
                isSupplierMeeting));
    }
}
