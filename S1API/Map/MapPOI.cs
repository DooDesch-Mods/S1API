using System;
using S1API.Internal.Map;
using UnityEngine;

namespace S1API.Map
{
    /// <summary>
    /// A managed handle for a standalone point of interest on the phone map.
    /// </summary>
    public sealed class MapPOI : IDisposable
    {
        private readonly MapPOIRuntime _runtime;
        private Vector3 _position;
        private Transform? _target;
        private bool _removed;

        /// <summary>The stable identifier used to register this marker.</summary>
        public string Id { get; }

        /// <summary>The marker's current display label.</summary>
        public string Label { get; private set; }

        /// <summary>The marker's custom icon, or <see langword="null"/> for the native appearance.</summary>
        public Sprite? Icon { get; private set; }

        /// <summary>Controls when the marker label is displayed.</summary>
        public MapPOITextVisibility TextVisibility { get; private set; }

        /// <summary>Whether the marker icon rotates with its target heading.</summary>
        public bool RotateWithTarget { get; private set; }

        /// <summary>The requested marker visibility.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Whether the native POI component has been created.
        /// </summary>
        public bool IsReady => !_removed && _runtime.IsReady;

        /// <summary>
        /// Whether the phone map has created this marker's UI.
        /// </summary>
        public bool IsUIReady => !_removed && _runtime.IsUIReady;

        /// <summary>Whether this handle has been removed or invalidated by a scene change.</summary>
        public bool IsRemoved => _removed;

        /// <summary>The moving target currently followed by the marker, if any.</summary>
        public Transform? Target => _target != null ? _target : null;

        /// <summary>The marker's current world position.</summary>
        public Vector3 Position => Target != null ? Target.position : _position;

        /// <summary>The live map UI transform, or <see langword="null"/> until it is available.</summary>
        public RectTransform? UI => _runtime.UI;

        internal bool IsRegistryValid => !_removed && _runtime.CanContinue;

        internal MapPOI(MapPOIBuilder builder)
        {
            Id = builder.Id;
            Label = builder.Label;
            _position = builder.Position;
            _target = builder.Target;
            Icon = builder.Icon;
            TextVisibility = builder.TextVisibility;
            RotateWithTarget = builder.RotateWithTarget;
            IsVisible = builder.Visible;
            _runtime = new MapPOIRuntime(this);
        }

        /// <summary>Updates the marker label.</summary>
        public MapPOI SetLabel(string label)
        {
            ThrowIfRemoved();
            Label = label ?? throw new ArgumentNullException(nameof(label));
            _runtime.ApplyLabel();
            return this;
        }

        /// <summary>Updates or clears the marker's custom icon.</summary>
        public MapPOI SetIcon(Sprite? icon)
        {
            ThrowIfRemoved();
            Icon = icon;
            _runtime.ApplyIcon();
            return this;
        }

        /// <summary>Moves the marker to a fixed world position and clears its moving target.</summary>
        public MapPOI SetPosition(Vector3 position)
        {
            ThrowIfRemoved();
            _target = null;
            _position = position;
            _runtime.ApplyLocation();
            return this;
        }

        /// <summary>Binds the marker to a moving target.</summary>
        public MapPOI SetTarget(Transform target)
        {
            ThrowIfRemoved();
            _target = target != null ? target : throw new ArgumentNullException(nameof(target));
            _position = target.position;
            _runtime.ApplyLocation();
            return this;
        }

        /// <summary>Updates when the marker label is displayed.</summary>
        public MapPOI SetTextVisibility(MapPOITextVisibility visibility)
        {
            ThrowIfRemoved();
            if (!Enum.IsDefined(typeof(MapPOITextVisibility), visibility))
            {
                throw new ArgumentOutOfRangeException(nameof(visibility));
            }

            TextVisibility = visibility;
            _runtime.ApplyTextVisibility();
            return this;
        }

        /// <summary>Updates whether the marker icon rotates with its target heading.</summary>
        public MapPOI SetRotation(bool rotateWithTarget)
        {
            ThrowIfRemoved();
            RotateWithTarget = rotateWithTarget;
            _runtime.ApplyRotation();
            return this;
        }

        /// <summary>Shows or hides the marker.</summary>
        public MapPOI SetVisible(bool visible)
        {
            ThrowIfRemoved();
            IsVisible = visible;
            _runtime.ApplyVisibility();
            return this;
        }

        /// <summary>
        /// Focuses the phone map on this marker.
        /// </summary>
        /// <param name="openMap">Whether to open the phone map before focusing it.</param>
        /// <returns><see langword="true"/> when the live marker UI was available and focused.</returns>
        public bool Focus(bool openMap = true)
        {
            ThrowIfRemoved();
            return _runtime.Focus(openMap);
        }

        /// <summary>Removes the marker and releases its native objects.</summary>
        /// <returns><see langword="true"/> on the first removal; otherwise <see langword="false"/>.</returns>
        public bool Remove()
        {
            if (_removed)
            {
                return false;
            }

            _removed = true;
            _runtime.Dispose(destroyRuntimeObject: true);
            MapPOIManager.Unregister(this);
            return true;
        }

        /// <inheritdoc/>
        public void Dispose() => Remove();

        internal void StartInitialization() => _runtime.Start();

        internal void InvalidateForSceneChange()
        {
            if (_removed)
            {
                return;
            }

            _removed = true;
            _runtime.Dispose(destroyRuntimeObject: false);
        }

        private void ThrowIfRemoved()
        {
            if (_removed)
            {
                throw new ObjectDisposedException(nameof(MapPOI), $"Map POI '{Id}' has been removed.");
            }
        }
    }
}
