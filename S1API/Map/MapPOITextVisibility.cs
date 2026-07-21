namespace S1API.Map
{
    /// <summary>
    /// Controls when a standalone phone-map POI displays its label.
    /// </summary>
    public enum MapPOITextVisibility
    {
        /// <summary>The label is never shown.</summary>
        Off = 0,

        /// <summary>The label is always shown while the marker is visible.</summary>
        Always = 1,

        /// <summary>The label is shown while the marker is hovered or selected.</summary>
        OnHover = 2
    }
}
