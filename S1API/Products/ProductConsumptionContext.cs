using System.Collections.Generic;
using S1API.Entities;
using S1API.Properties.Interfaces;

namespace S1API.Products
{
    /// <summary>
    /// Describes one intrinsic consumption lifecycle transition for a registered custom product.
    /// </summary>
    /// <remarks>
    /// Player callbacks are invoked only for the local player. NPC callbacks can receive an
    /// unavailable <see cref="NPC"/> wrapper for game-owned NPCs; use <see cref="TargetId"/>
    /// when behavior only needs the stable native target identifier.
    /// </remarks>
    public sealed class ProductConsumptionContext
    {
        private readonly string _productId;
        private readonly ProductKind _productKind;
        private readonly Quality _quality;
        private readonly IReadOnlyList<PropertyBase> _properties;

        internal ProductConsumptionContext(
            ProductInstance product,
            CustomProductDefinition definition,
            Player? player,
            NPC? npc,
            string targetId)
        {
            Product = product;
            Definition = definition;
            _productId = definition.ID;
            _productKind = definition.ProductKind;
            _quality = product.Quality;
            _properties = product.Properties;
            Player = player;
            NPC = npc;
            TargetId = targetId;
        }

        internal ProductConsumptionContext(
            string productId,
            ProductKind productKind,
            Player? player,
            NPC? npc,
            string targetId)
        {
            Product = null!;
            Definition = null!;
            _productId = productId;
            _productKind = productKind;
            _quality = Quality.Standard;
            _properties = System.Array.Empty<PropertyBase>();
            Player = player;
            NPC = npc;
            TargetId = targetId;
        }

        /// <summary>
        /// Gets the consumed product instance.
        /// </summary>
        public ProductInstance Product { get; }

        /// <summary>
        /// Gets the registered custom product definition.
        /// </summary>
        public CustomProductDefinition Definition { get; }

        /// <summary>
        /// Gets the stable product identifier.
        /// </summary>
        public string ProductId =>
            _productId;

        /// <summary>
        /// Gets the logical product kind retained by generated mixed products.
        /// </summary>
        public ProductKind ProductKind =>
            _productKind;

        /// <summary>
        /// Gets the consumed product quality.
        /// </summary>
        public Quality Quality =>
            _quality;

        /// <summary>
        /// Gets the ordinary product properties applied alongside this profile.
        /// </summary>
        public IReadOnlyList<PropertyBase> Properties =>
            _properties;

        /// <summary>
        /// Gets the local player for player callbacks, or <see langword="null"/> for NPC callbacks.
        /// </summary>
        public Player? Player { get; }

        /// <summary>
        /// Gets the S1API NPC wrapper when one is available, or <see langword="null"/> for a
        /// game-owned NPC without an S1API wrapper.
        /// </summary>
        public NPC? NPC { get; }

        /// <summary>
        /// Gets the stable native target identifier: a player code for players or the NPC ID for NPCs.
        /// </summary>
        public string TargetId { get; }

        /// <summary>
        /// Gets whether this transition targets the local player.
        /// </summary>
        public bool IsLocalPlayer =>
            Player != null;
    }
}
