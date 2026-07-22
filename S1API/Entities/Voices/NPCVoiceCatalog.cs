using System;
using System.Collections.Generic;
using System.Linq;

namespace S1API.Entities.Voices
{
    /// <summary>
    /// Provides stable identifiers for the supported base-game NPC voices.
    /// </summary>
    public static class NPCVoiceCatalog
    {
        /// <summary>Gets the cold voice.</summary>
        public static NPCVoiceDefinition Cold { get; } =
            new NPCVoiceDefinition("cold", "Cold", "Cold VO");

        /// <summary>Gets the crackhead voice.</summary>
        public static NPCVoiceDefinition Crackhead { get; } =
            new NPCVoiceDefinition("crackhead", "Crackhead", "Crackhead VO");

        /// <summary>Gets the first female voice.</summary>
        public static NPCVoiceDefinition Female1 { get; } =
            new NPCVoiceDefinition("female-1", "Female 1", "Female1 VO");

        /// <summary>Gets the second female voice.</summary>
        public static NPCVoiceDefinition Female2 { get; } =
            new NPCVoiceDefinition("female-2", "Female 2", "Female2 VO");

        /// <summary>Gets the goblin voice.</summary>
        public static NPCVoiceDefinition Goblin { get; } =
            new NPCVoiceDefinition("goblin", "Goblin", "Goblin VO");

        /// <summary>Gets the hippie voice.</summary>
        public static NPCVoiceDefinition Hippie { get; } =
            new NPCVoiceDefinition("hippie", "Hippie", "Hippie VO");

        /// <summary>Gets the Joel voice.</summary>
        public static NPCVoiceDefinition Joel { get; } =
            new NPCVoiceDefinition("joel", "Joel", "Joel VO");

        /// <summary>Gets the monotone voice.</summary>
        public static NPCVoiceDefinition Monotone { get; } =
            new NPCVoiceDefinition("monotone", "Monotone", "Monotone VO");

        /// <summary>Gets the redneck voice.</summary>
        public static NPCVoiceDefinition Redneck { get; } =
            new NPCVoiceDefinition("redneck", "Redneck", "Redneck VO");

        /// <summary>Gets the timid voice.</summary>
        public static NPCVoiceDefinition Timid { get; } =
            new NPCVoiceDefinition("timid", "Timid", "Timid VO");

        /// <summary>Gets the Tyler voice.</summary>
        public static NPCVoiceDefinition Tyler { get; } =
            new NPCVoiceDefinition("tyler", "Tyler", "Tyler VO");

        private static readonly IReadOnlyList<NPCVoiceDefinition> Definitions =
            Array.AsReadOnly(new[]
            {
                Cold,
                Crackhead,
                Female1,
                Female2,
                Goblin,
                Hippie,
                Joel,
                Monotone,
                Redneck,
                Timid,
                Tyler
            });

        private static readonly Dictionary<string, NPCVoiceDefinition> DefinitionsById =
            Definitions.ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets every supported voice definition.
        /// </summary>
        public static IReadOnlyList<NPCVoiceDefinition> All => Definitions;

        /// <summary>
        /// Gets a voice by its stable identifier.
        /// </summary>
        /// <param name="identifier">The case-insensitive voice identifier.</param>
        /// <returns>The matching voice definition.</returns>
        /// <exception cref="ArgumentException">Thrown when the identifier is missing or unsupported.</exception>
        public static NPCVoiceDefinition Get(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("An NPC voice identifier is required.", nameof(identifier));

            string normalizedIdentifier = identifier.Trim();
            if (DefinitionsById.TryGetValue(normalizedIdentifier, out NPCVoiceDefinition? definition)
                && definition != null)
                return definition;

            throw new ArgumentException(
                $"Unsupported NPC voice identifier '{identifier}'. Supported identifiers: {string.Join(", ", Definitions.Select(definition => definition.Id))}.",
                nameof(identifier));
        }

        /// <summary>
        /// Attempts to get a voice by its stable identifier.
        /// </summary>
        /// <param name="identifier">The case-insensitive voice identifier.</param>
        /// <param name="definition">The matching definition, or <c>null</c> when no match exists.</param>
        /// <returns><c>true</c> when the identifier is supported; otherwise, <c>false</c>.</returns>
        public static bool TryGet(string identifier, out NPCVoiceDefinition? definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(identifier)
                   && DefinitionsById.TryGetValue(identifier.Trim(), out definition);
        }
    }
}
