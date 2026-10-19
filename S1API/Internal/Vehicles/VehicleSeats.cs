using System;
using System.Collections.Generic;
using S1API.Lifecycle;
using S1API.Vehicles;

#if IL2CPPMELON
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using S1Vehicles = ScheduleOne.Vehicles;
#endif

namespace S1API.Internal.Vehicles
{
    internal static class VehicleSeats
    {
        private static readonly Dictionary<S1Vehicles.LandVehicle, IReadOnlyList<VehicleSeatInfo>> Cache =
            new Dictionary<S1Vehicles.LandVehicle, IReadOnlyList<VehicleSeatInfo>>();
        private static readonly IReadOnlyList<VehicleSeatInfo> Empty = Array.AsReadOnly(Array.Empty<VehicleSeatInfo>());
        private static bool _lifecycleHooked;

        internal static IReadOnlyList<VehicleSeatInfo> Get(S1Vehicles.LandVehicle? vehicle)
        {
            if (ReferenceEquals(vehicle, null) || vehicle == null)
                return Empty;
            if (Cache.TryGetValue(vehicle, out var seats))
                return seats;

            var native = vehicle.Seats;
            if (native == null)
                return Empty;
            var result = new VehicleSeatInfo[native.Length];
            for (int i = 0; i < native.Length; i++)
                result[i] = new VehicleSeatInfo(i, native[i]);
            seats = Array.AsReadOnly(result);
            Cache.Add(vehicle, seats);
            if (!_lifecycleHooked)
            {
                GameLifecycle.OnPreSceneChange += Clear;
                _lifecycleHooked = true;
            }
            return seats;
        }

        internal static void Refresh(S1Vehicles.LandVehicle vehicle, int index)
        {
            if (Cache.TryGetValue(vehicle, out var seats) && index >= 0 && index < seats.Count)
                seats[index].Refresh();
        }

        internal static void Tick()
        {
            if (Cache.Count == 0)
                return;
            // Callbacks may destroy vehicles or resolve more seat collections.
            foreach (var entry in new List<KeyValuePair<S1Vehicles.LandVehicle, IReadOnlyList<VehicleSeatInfo>>>(Cache))
            {
                if (entry.Key == null)
                    Remove(entry.Key!);
                else
                    foreach (var seat in entry.Value)
                        seat.Refresh();
            }
        }

        internal static void Remove(S1Vehicles.LandVehicle vehicle)
        {
            if (!Cache.TryGetValue(vehicle, out var seats))
                return;
            Cache.Remove(vehicle);
            foreach (var seat in seats)
                seat.Invalidate();
        }

        internal static void Clear()
        {
            foreach (var seats in Cache.Values)
                foreach (var seat in seats)
                    seat.Invalidate();
            Cache.Clear();
        }
    }
}
