using S1API.Internal.Console;
using S1API.Items;

namespace S1API.Console
{
    /// <summary>
    /// Registers short, local aliases for item IDs accepted by the native
    /// <c>give</c> console command.
    /// </summary>
    /// <remarks>
    /// Aliases are console conveniences only. Given items retain their
    /// canonical IDs, and aliases are not included in saves, recipes,
    /// multiplayer manifests, compatibility hashes, or network payloads.
    /// </remarks>
    public static class ConsoleItemAliases
    {
        /// <summary>
        /// Registers a short alias for an existing canonical item ID.
        /// </summary>
        /// <param name="alias">
        /// The non-namespaced command token, such as <c>mdma</c>.
        /// </param>
        /// <param name="canonicalItemId">
        /// The existing canonical item ID, such as
        /// <c>example.mod:products/mdma</c>.
        /// </param>
        /// <remarks>
        /// Call this after the canonical item has been registered. Native item
        /// IDs always take precedence over aliases. Repeating the same mapping
        /// is idempotent; attempting to reuse an alias for another item throws.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when either argument is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when an argument is empty, the alias is not a safe command
        /// token, or the canonical item is not registered.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when the alias is already a native item ID or is registered
        /// for another canonical item.
        /// </exception>
        public static void Register(string alias, string canonicalItemId)
        {
            ConsoleItemAliasRegistry.Register(
                alias,
                canonicalItemId,
                ItemManager.IsItemRegistered);
        }
    }
}
