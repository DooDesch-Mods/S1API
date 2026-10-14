using System.Reflection;
using S1API.Trash;
using UnityEngine;

#if (MONOMELON || MONOBEPINEX)
using S1Recycler = ScheduleOne.ObjectScripts.Recycler;
#else
using S1Recycler = Il2CppScheduleOne.ObjectScripts.Recycler;
#endif

namespace S1API.Tests.Trash;

public sealed class RecyclerContractTests
{
    [Theory]
    [InlineData(RecyclerState.HatchClosed, S1Recycler.EState.HatchClosed, 0)]
    [InlineData(RecyclerState.HatchOpen, S1Recycler.EState.HatchOpen, 1)]
    [InlineData(RecyclerState.Processing, S1Recycler.EState.Processing, 2)]
    public void ManagedStatePreservesNativeValues(RecyclerState managed, S1Recycler.EState native, int value)
    {
        Assert.Equal(value, (int)managed);
        Assert.Equal(value, (int)native);
    }

    [Fact]
    public void RecyclerExposesOnlyReadOnlyWorldState()
    {
        Type type = typeof(Recycler);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));

        PropertyInfo[] properties = type.GetProperties();
        Assert.Equal(4, properties.Length);
        Assert.All(properties, property => Assert.Null(property.SetMethod));
        Assert.Equal(typeof(RecyclerState), type.GetProperty(nameof(Recycler.State))!.PropertyType);
        Assert.Equal(typeof(bool), type.GetProperty(nameof(Recycler.IsHatchOpen))!.PropertyType);
        Assert.Equal(typeof(Vector3), type.GetProperty(nameof(Recycler.Position))!.PropertyType);
        Assert.Equal(typeof(GameObject), type.GetProperty(nameof(Recycler.GameObject))!.PropertyType);
        Assert.Empty(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName));
    }

    [Fact]
    public void RegistryExposesSnapshotDiscoveryAndManagedSignals()
    {
        Type type = typeof(RecyclerRegistry);
        MethodInfo method = Assert.Single(type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => !method.IsSpecialName));
        Assert.Equal(nameof(RecyclerRegistry.GetAll), method.Name);
        Assert.Empty(method.GetParameters());
        Assert.Equal(typeof(IReadOnlyList<Recycler>), method.ReturnType);

        EventInfo[] events = type.GetEvents();
        Assert.Equal(2, events.Length);
        Assert.All(events, signal => Assert.Equal(typeof(Action<Recycler>), signal.EventHandlerType));
        Assert.Contains(events, signal => signal.Name == nameof(RecyclerRegistry.ProcessingStarted));
        Assert.Contains(events, signal => signal.Name == nameof(RecyclerRegistry.ProcessingStopped));
    }

    [Fact]
    public void ProcessingSignalsPreserveWrapperIdentityAndDoNotDeduplicate()
    {
        Recycler recycler = TestObjectFactory.CreateUninitialized<Recycler>();
        var starts = new List<Recycler>();
        var stops = new List<Recycler>();
        Action<Recycler> started = starts.Add;
        Action<Recycler> stopped = stops.Add;
        RecyclerRegistry.ProcessingStarted += started;
        RecyclerRegistry.ProcessingStopped += stopped;
        try
        {
            RecyclerRegistry.NotifyProcessingStarted(recycler);
            RecyclerRegistry.NotifyProcessingStarted(recycler);
            RecyclerRegistry.NotifyProcessingStopped(recycler);
            Assert.Equal(2, starts.Count);
            Assert.All(starts, observed => Assert.Same(recycler, observed));
            Assert.Same(recycler, Assert.Single(stops));
        }
        finally
        {
            RecyclerRegistry.ProcessingStarted -= started;
            RecyclerRegistry.ProcessingStopped -= stopped;
        }

        RecyclerRegistry.NotifyProcessingStarted(recycler);
        RecyclerRegistry.NotifyProcessingStopped(recycler);
        Assert.Equal(2, starts.Count);
        Assert.Single(stops);
    }
}
