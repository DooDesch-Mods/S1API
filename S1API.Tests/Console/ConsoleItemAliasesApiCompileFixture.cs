using S1API.Console;

namespace S1API.Tests.Console;

internal static class ConsoleItemAliasesApiCompileFixture
{
    internal static void RegisterAlias()
    {
        ConsoleItemAliases.Register(
            alias: "mdma",
            canonicalItemId: "example.mod:products/mdma");
    }
}
