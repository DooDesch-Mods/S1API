namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Applies one resolved presentation profile to a retained custom product.
    /// </summary>
    internal interface ICustomProductPresentationRuntime
    {
        void Apply(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfileRegistration? profile);
    }
}
