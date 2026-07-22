#if (IL2CPPMELON)
using Il2Cpp;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Map = Il2CppScheduleOne.Map;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1VehiclesAI = Il2CppScheduleOne.Vehicles.AI;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
#elif MONOMELON
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Map = ScheduleOne.Map;
using S1Vehicles = ScheduleOne.Vehicles;
using S1VehiclesAI = ScheduleOne.Vehicles.AI;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
#endif
using UnityEngine;
using S1API.Map;
using S1API.Vehicles;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Compatibility specification for the removed customer deal signal.
    /// </summary>
    /// <remarks>
    /// Schedule I 0.4.6 removed <c>NPCSignal_WaitForDelivery</c>. Prefab processing recognizes
    /// this specification and configures the current customer deal-attendance behaviour instead.
    /// Applying it at runtime is a once-warned no-op.
    /// </remarks>
    [System.Obsolete("NPCSignal_WaitForDelivery was removed in game version 0.4.6. Customer deal attendance is configured automatically.")]
    public sealed class EnsureDealSignalSpec : IScheduleActionSpec
    {
        /// <summary>
        /// Applies this specification to the given NPC schedule by ensuring a deal signal exists.
        /// </summary>
        /// <param name="schedule">The NPC schedule to ensure the deal signal on.</param>
        /// <remarks>
        /// This method calls the retained <see cref="NPCSchedule.EnsureDealSignal"/> compatibility no-op.
        /// </remarks>
        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            schedule.EnsureDealSignal();
        }
    }
}
