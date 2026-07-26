namespace S1API.Internal.Entities.Suppliers
{
    /// <summary>
    /// Computes the dialogue indexes that the native Supplier.Start method appends.
    /// </summary>
    internal static class SupplierMeetingDialoguePolicy
    {
        internal static SupplierMeetingDialogueBindings ForNativeStart(
            int existingGreetingCount,
            int existingChoiceCount)
        {
            return new SupplierMeetingDialogueBindings(
                existingGreetingCount,
                existingChoiceCount);
        }

        internal static bool ShouldActivate(
            bool visible,
            bool isMeeting)
        {
            return visible && isMeeting;
        }
    }

    internal readonly struct SupplierMeetingDialogueBindings
    {
        internal SupplierMeetingDialogueBindings(
            int greetingOverrideIndex,
            int choiceIndex)
        {
            GreetingOverrideIndex = greetingOverrideIndex;
            ChoiceIndex = choiceIndex;
        }

        internal int GreetingOverrideIndex { get; }

        internal int ChoiceIndex { get; }
    }
}
