using S1API.Internal.Entities.Suppliers;

namespace S1API.Tests.Entities;

public sealed class SupplierMeetingDialoguePolicyTests
{
    [Fact]
    public void EmptyNativeSupplierUsesFirstDialogueEntries()
    {
        SupplierMeetingDialogueBindings bindings =
            SupplierMeetingDialoguePolicy.ForNativeStart(0, 0);

        Assert.Equal(0, bindings.GreetingOverrideIndex);
        Assert.Equal(0, bindings.ChoiceIndex);
    }

    [Fact]
    public void ConvertedEmployeeTargetsEntriesAppendedBySupplierStart()
    {
        SupplierMeetingDialogueBindings bindings =
            SupplierMeetingDialoguePolicy.ForNativeStart(1, 0);

        Assert.Equal(1, bindings.GreetingOverrideIndex);
        Assert.Equal(0, bindings.ChoiceIndex);
    }

    [Fact]
    public void ExistingCustomEntriesRemainAheadOfSupplierEntries()
    {
        SupplierMeetingDialogueBindings bindings =
            SupplierMeetingDialoguePolicy.ForNativeStart(3, 2);

        Assert.Equal(3, bindings.GreetingOverrideIndex);
        Assert.Equal(2, bindings.ChoiceIndex);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void DialogueActivatesOnlyForVisibleMeetingSupplier(
        bool visible,
        bool isMeeting,
        bool expected)
    {
        Assert.Equal(
            expected,
            SupplierMeetingDialoguePolicy.ShouldActivate(
                visible,
                isMeeting));
    }
}
