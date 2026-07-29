namespace S1API.Entities.Voices
{
    /// <summary>
    /// Identifies a supported base-game NPC voice database.
    /// </summary>
    /// <remarks>
    /// A voice definition selects a reusable clip database. Pitch remains a separate NPC setting.
    /// </remarks>
    public sealed class NPCVoiceDefinition
    {
        internal NPCVoiceDefinition(string id, string displayName, string databaseName)
        {
            Id = id;
            DisplayName = displayName;
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Gets the stable, case-insensitive S1API identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable voice name.
        /// </summary>
        public string DisplayName { get; }

        internal string DatabaseName { get; }

        /// <inheritdoc />
        public override string ToString() => Id;
    }
}
