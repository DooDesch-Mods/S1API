using S1API.Internal.Console;

namespace S1API.Tests.Console;

public sealed class ConsoleItemAliasRegistryTests : IDisposable
{
    private readonly HashSet<string> _registeredItems =
        new(StringComparer.OrdinalIgnoreCase);

    public ConsoleItemAliasRegistryTests()
    {
        ConsoleItemAliasRegistry.ResetForTesting();
        _registeredItems.Add("example.mod:products/mdma");
        _registeredItems.Add("cash");
    }

    [Fact]
    public void RegistrationTrimsAndResolvesCaseInsensitively()
    {
        ConsoleItemAliasRegistry.Register(
            " MDMA ",
            " example.mod:products/mdma ",
            ItemExists);

        bool resolved = ConsoleItemAliasRegistry.TryResolveForGive(
            "mDmA",
            ItemExists,
            out string canonicalItemId);

        Assert.True(resolved);
        Assert.Equal("example.mod:products/mdma", canonicalItemId);
    }

    [Fact]
    public void RepeatingEquivalentRegistrationIsIdempotent()
    {
        ConsoleItemAliasRegistry.Register(
            "mdma",
            "example.mod:products/mdma",
            ItemExists);
        ConsoleItemAliasRegistry.Register(
            "MDMA",
            "EXAMPLE.MOD:PRODUCTS/MDMA",
            ItemExists);

        Assert.True(
            ConsoleItemAliasRegistry.TryResolveForGive(
                "mdma",
                ItemExists,
                out string canonicalItemId));
        Assert.Equal("example.mod:products/mdma", canonicalItemId);
    }

    [Fact]
    public void ConflictingAliasRegistrationFailsDeterministically()
    {
        _registeredItems.Add("other.mod:products/mdma");
        ConsoleItemAliasRegistry.Register(
            "mdma",
            "example.mod:products/mdma",
            ItemExists);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                ConsoleItemAliasRegistry.Register(
                    "MDMA",
                    "other.mod:products/mdma",
                    ItemExists));

        Assert.Contains(
            "example.mod:products/mdma",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "other.mod:products/mdma",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NativeItemIdCannotBeShadowed()
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                ConsoleItemAliasRegistry.Register(
                    "cash",
                    "example.mod:products/mdma",
                    ItemExists));

        Assert.Contains("cannot be shadowed", exception.Message);
    }

    [Fact]
    public void MissingCanonicalItemFailsBeforeRegistration()
    {
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                ConsoleItemAliasRegistry.Register(
                    "mdma",
                    "missing.mod:products/mdma",
                    ItemExists));

        Assert.Equal("canonicalItemId", exception.ParamName);
        Assert.False(
            ConsoleItemAliasRegistry.TryResolveForGive(
                "mdma",
                ItemExists,
                out _));
    }

    [Fact]
    public void NativeItemRegisteredLaterTakesPrecedence()
    {
        ConsoleItemAliasRegistry.Register(
            "mdma",
            "example.mod:products/mdma",
            ItemExists);
        _registeredItems.Add("mdma");

        Assert.False(
            ConsoleItemAliasRegistry.TryResolveForGive(
                "mdma",
                ItemExists,
                out string canonicalItemId));
        Assert.Equal(string.Empty, canonicalItemId);
    }

    [Fact]
    public void MissingTargetAtExecutionPreservesVanillaLookup()
    {
        ConsoleItemAliasRegistry.Register(
            "mdma",
            "example.mod:products/mdma",
            ItemExists);
        _registeredItems.Remove("example.mod:products/mdma");

        Assert.False(
            ConsoleItemAliasRegistry.TryResolveForGive(
                "mdma",
                ItemExists,
                out string canonicalItemId));
        Assert.Equal(string.Empty, canonicalItemId);
    }

    [Fact]
    public void UnknownAliasPreservesVanillaLookup()
    {
        Assert.False(
            ConsoleItemAliasRegistry.TryResolveForGive(
                "unknown",
                ItemExists,
                out string canonicalItemId));
        Assert.Equal(string.Empty, canonicalItemId);
    }

    [Fact]
    public void ResolvingAliasPreservesQuantityArgument()
    {
        ConsoleItemAliasRegistry.Register(
            "mdma",
            "example.mod:products/mdma",
            ItemExists);
        List<string> args = ["mdma", "5"];

        args[0] = ConsoleItemAliasRegistry.ResolveItemCodeForGive(
            args[0],
            ItemExists);

        Assert.Equal("example.mod:products/mdma", args[0]);
        Assert.Equal("5", args[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("mod:mdma")]
    [InlineData("two words")]
    [InlineData("mdma$")]
    public void InvalidAliasesAreRejected(string? alias)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ConsoleItemAliasRegistry.Register(
                alias!,
                "example.mod:products/mdma",
                ItemExists));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyCanonicalIdsAreRejected(string? canonicalItemId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ConsoleItemAliasRegistry.Register(
                "mdma",
                canonicalItemId!,
                ItemExists));
    }

    public void Dispose()
    {
        ConsoleItemAliasRegistry.ResetForTesting();
    }

    private bool ItemExists(string itemId) =>
        _registeredItems.Contains(itemId);
}
