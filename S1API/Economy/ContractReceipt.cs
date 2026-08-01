using System;

namespace S1API.Economy
{
    /// <summary>
    /// Lightweight DTO mirroring base game's ContractReceipt for analytics/history.
    /// </summary>
    public sealed class ContractReceipt
    {
        /// <summary>
        /// Creates an empty receipt suitable for deserialization or incremental population.
        /// </summary>
        public ContractReceipt()
        {
        }

        /// <summary>
        /// Gets or sets the receipt identifier.
        /// </summary>
        public int ReceiptId { get; set; } = -1;

        /// <summary>
        /// Gets or sets the customer identifier associated with the contract.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount paid when the contract completed.
        /// </summary>
        public float AmountPaid { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the completion day and time.
        /// </summary>
        public (int days, int time) CompletionTime { get; set; } = (0, 0);

        /// <summary>
        /// Gets or sets the delivered item identifiers and quantities.
        /// </summary>
        public (string id, int quantity)[] Items { get; set; } = Array.Empty<(string id, int quantity)>();
    }
}


