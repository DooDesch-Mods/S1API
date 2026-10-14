namespace S1API.Trash
{
    /// <summary>
    /// Describes the native world recycler's state.
    /// </summary>
    public enum RecyclerState
    {
        /// <summary>The recycler is waiting for its hatch to be opened.</summary>
        HatchClosed = 0,

        /// <summary>The hatch is open for trash insertion.</summary>
        HatchOpen = 1,

        /// <summary>The recycler is processing or waiting for cash collection.</summary>
        Processing = 2
    }
}
