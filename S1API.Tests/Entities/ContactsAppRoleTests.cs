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

    private sealed class CustomerContact;

    private sealed class DealerContact;

    private sealed class SupplierContact;

    private sealed class NonContact;

    private sealed class CustomerIndicatorContact;

    private sealed class DealerIndicatorContact;

    private sealed class SupplierIndicatorContact;
}
