namespace S1API.Products
{
    /// <summary>
    /// Identifies one of the game-supported mixer-map families.
    /// </summary>
    /// <remarks>
    /// This is a map-selection value, not a logical product-kind identity and it never creates a
    /// synthetic native drug-type value.
    /// </remarks>
    public enum ProductMixingMap
    {
        Marijuana,
        Methamphetamine,
        Cocaine,
        Shrooms
    }
}
