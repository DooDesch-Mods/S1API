using System;
using UnityEngine;

namespace S1API.Map
{
    /// <summary>
    /// Builds standalone points of interest for the phone map.
    /// </summary>
    public sealed class MapPOIBuilder
    {
        internal string Id { get; }
        internal string Label { get; private set; }
        internal Vector3 Position { get; private set; }
        internal Transform? Target { get; private set; }
        internal Sprite? Icon { get; private set; }
        internal MapPOITextVisibility TextVisibility { get; private set; } = MapPOITextVisibility.Always;
        internal bool RotateWithTarget { get; private set; }
        internal bool Visible { get; private set; } = true;

        /// <summary>
        /// Creates a builder for a uniquely identified map marker.
        /// </summary>
        /// <param name="id">
        /// A stable, mod-namespaced identifier such as <c>my-mod:supplier-stash</c>.
        /// </param>
        public MapPOIBuilder(string id)
        {
            Id = MapPOIManager.NormalizeId(id, nameof(id));
            Label = Id;
        }

        /// <summary>Sets the label displayed beside the marker.</summary>
        public MapPOIBuilder WithLabel(string label)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            return this;
        }

        /// <summary>
        /// Places the marker at a fixed world position.
        /// </summary>
        /// <remarks>This clears any target previously supplied with <see cref="WithTarget"/>.</remarks>
        public MapPOIBuilder WithPosition(Vector3 position)
        {
            Position = position;
            Target = null;
            return this;
        }

        /// <summary>
        /// Binds the marker to a moving target.
        /// </summary>
        public MapPOIBuilder WithTarget(Transform target)
        {
            Target = target != null ? target : throw new ArgumentNullException(nameof(target));
            Position = target.position;
            return this;
        }

        /// <summary>
        /// Sets an optional custom icon. When omitted, the native marker appearance is retained.
        /// </summary>
        public MapPOIBuilder WithIcon(Sprite? icon)
        {
            Icon = icon;
            return this;
        }

        /// <summary>Controls when the marker label is displayed.</summary>
        public MapPOIBuilder WithTextVisibility(MapPOITextVisibility visibility)
        {
            if (!Enum.IsDefined(typeof(MapPOITextVisibility), visibility))
            {
                throw new ArgumentOutOfRangeException(nameof(visibility));
            }

            TextVisibility = visibility;
            return this;
        }

        /// <summary>
        /// Controls whether the icon rotates with the marker transform's world-space heading.
        /// </summary>
        public MapPOIBuilder WithRotation(bool rotateWithTarget = true)
        {
            RotateWithTarget = rotateWithTarget;
            return this;
        }

        /// <summary>Sets the marker's initial visibility.</summary>
        public MapPOIBuilder WithVisibility(bool visible)
        {
            Visible = visible;
            return this;
        }

        /// <summary>
        /// Registers the marker and starts its deferred native initialization.
        /// </summary>
        /// <returns>A managed handle that can update or remove the marker.</returns>
        public MapPOI Build() => MapPOIManager.Create(this);
    }
}
