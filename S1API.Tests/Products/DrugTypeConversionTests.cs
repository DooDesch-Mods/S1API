#if (IL2CPPMELON)
using NativeDrugType = Il2CppScheduleOne.Product.EDrugType;
#elif MONOMELON
using NativeDrugType = ScheduleOne.Product.EDrugType;
#endif

using S1API.Products;

namespace S1API.Tests.Products;

public sealed class DrugTypeConversionTests
{
    public static IEnumerable<object[]> KnownMappings
    {
        get
        {
            yield return new object[] { DrugType.Marijuana, NativeDrugType.Marijuana };
            yield return new object[] { DrugType.Methamphetamine, NativeDrugType.Methamphetamine };
            yield return new object[] { DrugType.Cocaine, NativeDrugType.Cocaine };
            yield return new object[] { DrugType.MDMA, NativeDrugType.MDMA };
            yield return new object[] { DrugType.Shrooms, NativeDrugType.Shrooms };
            yield return new object[] { DrugType.Heroin, NativeDrugType.Heroin };
        }
    }

    [Theory]
    [MemberData(nameof(KnownMappings))]
    public void KnownValuesRoundTrip(DrugType apiValue, NativeDrugType nativeValue)
    {
        Assert.Equal(apiValue, nativeValue.ToAPI());
        Assert.Equal(nativeValue, apiValue.ToInternal());
    }

    [Fact]
    public void ApiAndNativeEnumsExposeTheSameKnownValues()
    {
        var apiValues = Enum.GetValues<DrugType>();
        var nativeValues = Enum.GetValues<NativeDrugType>();

        Assert.Equal(apiValues.Length, nativeValues.Length);
        Assert.Equal(apiValues.Select(value => (int)value), nativeValues.Select(value => (int)value));
        Assert.Equal(apiValues.Select(value => value.ToString()), nativeValues.Select(value => value.ToString()));
    }

    [Fact]
    public void UnknownNumericValuesRemainRoundTrippable()
    {
        const int unknownValue = 42;
        var apiValue = (DrugType)unknownValue;
        var nativeValue = (NativeDrugType)unknownValue;

        Assert.Equal(apiValue, nativeValue.ToAPI());
        Assert.Equal(nativeValue, apiValue.ToInternal());
    }
}
