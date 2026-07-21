using System;
using System.Collections.Generic;

namespace S1API.Map
{
    /// <summary>
    /// Tracks standalone phone-map POIs by stable identifier.
    /// </summary>
    public static class MapPOIManager
    {
        private static readonly Dictionary<string, MapPOI> Markers =
            new Dictionary<string, MapPOI>(StringComparer.Ordinal);

        /// <summary>Returns a snapshot of the currently registered markers.</summary>
        public static IReadOnlyCollection<MapPOI> All
        {
            get
            {
                PruneInvalidMarkers();
                return new List<MapPOI>(Markers.Values).AsReadOnly();
            }
        }

        /// <summary>Gets a marker by ID, or <see langword="null"/> when it is not registered.</summary>
        public static MapPOI? Get(string id)
        {
            TryGet(id, out MapPOI? marker);
            return marker;
        }

        /// <summary>Attempts to get a marker by ID.</summary>
        public static bool TryGet(string id, out MapPOI? marker)
        {
            string normalizedId = NormalizeId(id, nameof(id));
            if (!Markers.TryGetValue(normalizedId, out marker))
            {
                return false;
            }

            if (marker.IsRegistryValid)
            {
                return true;
            }

            Markers.Remove(normalizedId);
            marker = null;
            return false;
        }

        /// <summary>Removes a marker by ID.</summary>
        /// <returns><see langword="true"/> when a registered marker was removed.</returns>
        public static bool Remove(string id)
        {
            return TryGet(id, out MapPOI? marker) && marker!.Remove();
        }

        /// <summary>Removes every currently registered standalone marker.</summary>
        public static void RemoveAll()
        {
            var snapshot = new List<MapPOI>(Markers.Values);
            foreach (MapPOI marker in snapshot)
            {
                marker.Remove();
            }

            Markers.Clear();
        }

        internal static MapPOI Create(MapPOIBuilder builder)
        {
            PruneInvalidMarkers();
            if (Markers.ContainsKey(builder.Id))
            {
                throw new InvalidOperationException($"A map POI with ID '{builder.Id}' is already registered.");
            }

            var marker = new MapPOI(builder);
            Markers.Add(marker.Id, marker);
            marker.StartInitialization();
            return marker;
        }

        internal static void Unregister(MapPOI marker)
        {
            if (Markers.TryGetValue(marker.Id, out MapPOI? registered) && ReferenceEquals(marker, registered))
            {
                Markers.Remove(marker.Id);
            }
        }

        internal static void ResetForSceneChange()
        {
            var snapshot = new List<MapPOI>(Markers.Values);
            Markers.Clear();

            foreach (MapPOI marker in snapshot)
            {
                marker.InvalidateForSceneChange();
            }
        }

        internal static string NormalizeId(string id, string parameterName)
        {
            if (id == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            string normalized = id.Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Map POI IDs cannot be empty or whitespace.", parameterName);
            }

            return normalized;
        }

        private static void PruneInvalidMarkers()
        {
            var invalidIds = new List<string>();
            foreach (KeyValuePair<string, MapPOI> pair in Markers)
            {
                if (!pair.Value.IsRegistryValid)
                {
                    invalidIds.Add(pair.Key);
                }
            }

            foreach (string id in invalidIds)
            {
                Markers.Remove(id);
            }
        }
    }
}
