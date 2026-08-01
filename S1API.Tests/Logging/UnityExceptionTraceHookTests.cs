using S1API.Internal.Diagnostics;

namespace S1API.Tests.Logging;

public sealed class UnityExceptionTraceHookTests
{
    [Theory]
    [InlineData("ScheduleOne.Weather.EnvironmentManager.GetWeatherProfileFromPosition (UnityEngine.Vector3 position)")]
    [InlineData("ScheduleOne.NPCs.NPCMovement+<FaceDirection_Process>d__170.MoveNext ()")]
    [InlineData("ScheduleOne.Configuration.ConfigurationServiceNetworker.OnDestroy ()")]
    [InlineData("ScheduleOne.UI.PauseMenu.OnDestroy ()")]
    public void KnownNativeStackFramesReturnBaseGameAdvisory(string stackTrace)
    {
        string? advisory = UnityExceptionTraceHook.GetKnownBaseGameNullReferenceAdvisory(stackTrace);

        Assert.NotNull(advisory);
        Assert.Contains("native/base game", advisory, StringComparison.Ordinal);
        Assert.Contains("not caused by a mod", advisory, StringComparison.Ordinal);
    }

    [Fact]
    public void OtherStackFramesDoNotReturnBaseGameAdvisory()
    {
        string? advisory = UnityExceptionTraceHook.GetKnownBaseGameNullReferenceAdvisory(
            "ExampleMod.Widget.Update ()");

        Assert.Null(advisory);
    }
}
