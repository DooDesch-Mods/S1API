using S1API.Entities.Supplier;
using System.Reflection;

namespace S1API.Tests.Entities;

public sealed class SupplierPersistentIdentityTests
{
    [Fact]
    public void PersistentIdIsPublicFluentApi()
    {
        MethodInfo? method = typeof(SupplierDataBuilder).GetMethod(
            nameof(SupplierDataBuilder.WithPersistentId),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

        Assert.NotNull(method);
        Assert.Equal(typeof(SupplierDataBuilder), method.ReturnType);
    }

    [Fact]
    public void PersistentIdIsOptInAndPreservesTheConfiguredValue()
    {
        var defaults = new SupplierDataBuilder().BuildInternal();
        var builder = new SupplierDataBuilder();

        SupplierDataBuilder result = builder.WithPersistentId(
            " ifbars.moredrugs:npcs/disco-davey ");

        Assert.Same(builder, result);
        Assert.Null(defaults.PersistentId);
        Assert.Equal(
            "ifbars.moredrugs:npcs/disco-davey",
            builder.BuildInternal().PersistentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PersistentIdRejectsEmptyValues(string persistentId)
    {
        var builder = new SupplierDataBuilder();

        Assert.Throws<ArgumentException>(() =>
            builder.WithPersistentId(persistentId));
    }
}
