using S1API.Internal.Abstraction;
using S1API.Saveables;

namespace S1API.Tests.Saveables;

public sealed class SaveableAutoRegistryTests
{
    [Fact]
    public void DiscoveryRegistersOnlyDirectConcreteSaveablesOutsideS1Api()
    {
        SaveableAutoRegistry.ClearCache();
        try
        {
            Type[] registered = SaveableAutoRegistry.GetRegisteredSaveables()
                .Select(saveable => saveable.GetType())
                .ToArray();

            Assert.Contains(typeof(DirectSaveableShape), registered);
            Assert.DoesNotContain(typeof(IndirectSaveableShape), registered);
            Assert.DoesNotContain(typeof(AbstractSaveableShape), registered);
            Assert.DoesNotContain(registered, type => type.Assembly == typeof(Saveable).Assembly);
        }
        finally
        {
            SaveableAutoRegistry.ClearCache();
        }
    }

    [Fact]
    public void DiscoveryReturnsTheSameInstanceOnRepeatedCalls()
    {
        SaveableAutoRegistry.ClearCache();
        try
        {
            Saveable first = SaveableAutoRegistry.GetRegisteredSaveables().OfType<DirectSaveableShape>().Single();
            Saveable second = SaveableAutoRegistry.GetRegisteredSaveables().OfType<DirectSaveableShape>().Single();

            Assert.Same(first, second);
        }
        finally
        {
            SaveableAutoRegistry.ClearCache();
        }
    }

    private sealed class DirectSaveableShape : Saveable
    {
    }

    private abstract class AbstractSaveableShape : Saveable
    {
    }

    private sealed class IndirectSaveableShape : AbstractSaveableShape
    {
    }
}
