using S1API.Entities;
using S1API.Internal.Patches;

namespace S1API.Tests.Entities;

public sealed class ContactsAppRoleTests
{
    [Fact]
    public void IsContactRole_IncludesCustomersDealersAndSuppliers()
    {
        NPC.RegisterCustomerType(typeof(CustomerContact));
        NPC.RegisterDealerType(typeof(DealerContact));
        NPC.RegisterSupplierType(typeof(SupplierContact));

        Assert.True(ContactsAppPatches.IsContactRole(typeof(CustomerContact)));
        Assert.True(ContactsAppPatches.IsContactRole(typeof(DealerContact)));
        Assert.True(ContactsAppPatches.IsContactRole(typeof(SupplierContact)));
        Assert.False(ContactsAppPatches.IsContactRole(typeof(NonContact)));
    }

    [Fact]
    public void GetRoleIndicators_DistinguishesDealerSupplierAndCustomer()
    {
        NPC.RegisterCustomerType(typeof(CustomerIndicatorContact));
        NPC.RegisterDealerType(typeof(DealerIndicatorContact));
        NPC.RegisterSupplierType(typeof(SupplierIndicatorContact));

        Assert.Equal(
            (IsDealer: false, IsSupplier: false),
            ContactsAppPatches.GetRoleIndicators(typeof(CustomerIndicatorContact)));
        Assert.Equal(
            (IsDealer: true, IsSupplier: false),
            ContactsAppPatches.GetRoleIndicators(typeof(DealerIndicatorContact)));
        Assert.Equal(
            (IsDealer: false, IsSupplier: true),
            ContactsAppPatches.GetRoleIndicators(typeof(SupplierIndicatorContact)));
    }

    [Fact]
    public void ApplyRoleIndicators_ActivatesExpectedNamedIndicators()
    {
        NPC.RegisterCustomerType(typeof(CustomerActivationContact));
        NPC.RegisterDealerType(typeof(DealerActivationContact));
        NPC.RegisterSupplierType(typeof(SupplierActivationContact));

        Assert.Equal(
            new[]
            {
                ("DealerIndicator", false),
                ("SupplierIndicator", false),
            },
            CaptureIndicatorStates(typeof(CustomerActivationContact)));
        Assert.Equal(
            new[]
            {
                ("DealerIndicator", true),
                ("SupplierIndicator", false),
            },
            CaptureIndicatorStates(typeof(DealerActivationContact)));
        Assert.Equal(
            new[]
            {
                ("DealerIndicator", false),
                ("SupplierIndicator", true),
            },
            CaptureIndicatorStates(typeof(SupplierActivationContact)));
    }

    private static (string Name, bool IsActive)[] CaptureIndicatorStates(
        Type npcType)
    {
        var states = new List<(string Name, bool IsActive)>();
        ContactsAppPatches.ApplyRoleIndicators(
            npcType,
            (name, isActive) => states.Add((name, isActive)));
        return states.ToArray();
    }

    private sealed class CustomerContact
    {
    }

    private sealed class DealerContact
    {
    }

    private sealed class SupplierContact
    {
    }

    private sealed class NonContact
    {
    }

    private sealed class CustomerIndicatorContact
    {
    }

    private sealed class DealerIndicatorContact
    {
    }

    private sealed class SupplierIndicatorContact
    {
    }

    private sealed class CustomerActivationContact
    {
    }

    private sealed class DealerActivationContact
    {
    }

    private sealed class SupplierActivationContact
    {
    }
}
