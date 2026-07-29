using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>Describes how incompatible custom-product content is handled in multiplayer.</summary>
    public enum CustomProductMultiplayerPolicy
    {
        /// <summary>
        /// Reject the joining client before its inventory or product state is deserialized.
        /// </summary>
        Reject = 0
    }

    /// <summary>
    /// Provides additive diagnostics for custom-product multiplayer compatibility.
    /// </summary>
    /// <remarks>
    /// S1API transmits only a bounded scalar manifest and never transfers assets, delegates,
    /// arbitrary types, paths, or save files. A client whose locally registered definitions do not
    /// match the host is rejected before its player inventory is deserialized.
    /// </remarks>
    public static class CustomProductMultiplayer
    {
        /// <summary>Gets the explicit policy used for missing or incompatible custom content.</summary>
        public static CustomProductMultiplayerPolicy MissingContentPolicy =>
            CustomProductMultiplayerPolicy.Reject;

        /// <summary>
        /// Gets the deterministic compatibility hash for custom products registered in this process.
        /// </summary>
        /// <remarks>
        /// The value is intended for diagnostics only. It contains no assets, paths, raw provider
        /// data, or save content. The method throws when the current registrations cannot be
        /// represented by the bounded protocol.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// A registered custom product cannot be represented by the bounded compatibility protocol.
        /// </exception>
        public static string GetCompatibilityManifestHash() =>
            CustomProductManifestRuntime.GetLocalCompatibilityHash();
    }
}
