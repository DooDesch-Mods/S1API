namespace S1API.Deliveries
{
    /// <summary>
    /// Describes the current lifecycle state of a delivery.
    /// </summary>
    public enum DeliveryStatus
    {
        /// <summary>The order is travelling to its destination.</summary>
        InTransit = 0,

        /// <summary>The order is waiting for its destination's loading dock.</summary>
        Waiting = 1,

        /// <summary>The delivery vehicle is present at the destination.</summary>
        Arrived = 2,

        /// <summary>The order has been unloaded and is no longer active.</summary>
        Completed = 3
    }
}
