using System.Reflection;
using S1API.Console;

namespace S1API.Tests.Console;

public sealed class ConsoleItemAliasesApiCompatibilityTests
{
    [Fact]
    public void RegisterRetainsTheDocumentedPublicShape()
    {
        Type type = typeof(ConsoleItemAliases);
        Assert.True(type.IsPublic);
        Assert.True(type.IsAbstract);
        Assert.True(type.IsSealed);

        MethodInfo register = type.GetMethod(
            nameof(ConsoleItemAliases.Register),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!;

        Assert.NotNull(register);
        Assert.Equal(typeof(void), register.ReturnType);
        ParameterInfo[] parameters = register.GetParameters();
        Assert.Equal("alias", parameters[0].Name);
        Assert.Equal("canonicalItemId", parameters[1].Name);
    }
}
