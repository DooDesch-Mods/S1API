using S1API.Products;

var tests = new (string Name, Action Run)[]
{
    ("normalizes a stable namespaced ID", () =>
        Equal("example.mod:calm-kush", WeedDefinitionBuilderContract.NormalizeId(" example.mod:calm-kush "))),
    ("rejects an unnamespaced ID", () =>
        Throws<ArgumentException>(() => WeedDefinitionBuilderContract.NormalizeId("calm-kush"))),
    ("rejects empty namespace segments", () =>
        Throws<ArgumentException>(() => WeedDefinitionBuilderContract.NormalizeId("example.mod:"))),
    ("rejects extra namespace separators", () =>
        Throws<ArgumentException>(() => WeedDefinitionBuilderContract.NormalizeId("example:mod:calm-kush"))),
    ("rejects unstable ID characters", () =>
        Throws<ArgumentException>(() => WeedDefinitionBuilderContract.NormalizeId("example.mod:calm kush"))),
    ("trims a display name", () =>
        Equal("Calm Kush", WeedDefinitionBuilderContract.NormalizeName(" Calm Kush "))),
    ("rejects an empty display name", () =>
        Throws<ArgumentException>(() => WeedDefinitionBuilderContract.NormalizeName(" "))),
    ("accepts one resolved property", () =>
        WeedDefinitionBuilderContract.ValidatePropertyCounts(1, 1)),
    ("accepts the native property limit", () =>
        WeedDefinitionBuilderContract.ValidatePropertyCounts(
            WeedDefinitionBuilderContract.MaximumPropertyCount,
            WeedDefinitionBuilderContract.MaximumPropertyCount)),
    ("rejects a product without properties", () =>
        Throws<InvalidOperationException>(() =>
            WeedDefinitionBuilderContract.ValidatePropertyCounts(0, 0))),
    ("rejects too many properties", () =>
        Throws<InvalidOperationException>(() =>
            WeedDefinitionBuilderContract.ValidatePropertyCounts(
                WeedDefinitionBuilderContract.MaximumPropertyCount + 1,
                WeedDefinitionBuilderContract.MaximumPropertyCount + 1))),
    ("rejects unresolved or duplicate properties", () =>
        Throws<InvalidOperationException>(() =>
            WeedDefinitionBuilderContract.ValidatePropertyCounts(2, 1)))
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{test.Name}: {exception.Message}");
        Console.Error.WriteLine($"FAIL {test.Name}: {exception}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} contract test(s) failed.");
    return 1;
}

Console.WriteLine($"PASS {tests.Length} weed builder contract tests");
return 0;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
}

static void Throws<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
