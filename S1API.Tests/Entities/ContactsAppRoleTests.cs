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

    private sealed class CustomerContact;

    private sealed class DealerContact;

    private sealed class SupplierContact;

    private sealed class NonContact;
}
