using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Internal.Vehicles;

#if IL2CPPMELON
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1Connection = Il2CppFishNet.Connection.NetworkConnection;
#elif MONOMELON
using S1Vehicles = ScheduleOne.Vehicles;
using S1Connection = FishNet.Connection.NetworkConnection;
#endif

namespace S1API.Internal.Patches
{
    [HarmonyPatch]
    internal static class VehicleSeatPatches
    {
        internal static MethodBase TargetMethod() =>
            typeof(S1Vehicles.LandVehicle)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method =>
                    method.Name.StartsWith("RpcLogic___SetSeatOccupant_", StringComparison.Ordinal)
                    && method.ReturnType == typeof(void)
                    && method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(S1Connection), typeof(int), typeof(S1Connection) }));

        [HarmonyPostfix]
        private static void Postfix(S1Vehicles.LandVehicle __instance, int __1) =>
            VehicleSeats.Refresh(__instance, __1);
    }
}
