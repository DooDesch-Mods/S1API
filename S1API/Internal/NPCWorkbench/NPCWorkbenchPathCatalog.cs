using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using S1API.Entities.Appearances.Base;
using S1API.Entities.Appearances.CustomizationFields;

namespace S1API.Internal.NPCWorkbench
{
    internal enum NPCWorkbenchPathKind
    {
        Hair,
        FaceLayer,
        BodyLayer,
        Accessory
    }

    internal sealed class NPCWorkbenchPathEntry
    {
        internal NPCWorkbenchPathEntry(
            NPCWorkbenchPathKind kind,
            Type declaringType,
            string memberName,
            string path)
        {
            Kind = kind;
            DeclaringType = declaringType;
            MemberName = memberName;
            Path = path;
        }

        internal NPCWorkbenchPathKind Kind { get; }
        internal Type DeclaringType { get; }
        internal string MemberName { get; }
        internal string Path { get; }
        internal string GroupName => DeclaringType.Name;
        internal string DisplayName => $"{GroupName} / {MemberName}";
        internal string CSharpTypeName => $"global::{DeclaringType.FullName}";
        internal string CSharpExpression => $"{CSharpTypeName}.{MemberName}";
    }

    internal static class NPCWorkbenchPathCatalog
    {
        private static readonly IReadOnlyDictionary<NPCWorkbenchPathKind, IReadOnlyList<NPCWorkbenchPathEntry>>
            EntriesByKind = Build();

        internal static IReadOnlyList<NPCWorkbenchPathEntry> Get(NPCWorkbenchPathKind kind) =>
            EntriesByKind[kind];

        internal static NPCWorkbenchPathEntry? Find(NPCWorkbenchPathKind kind, string path) =>
            Get(kind).FirstOrDefault(entry =>
                string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase));

        private static IReadOnlyDictionary<NPCWorkbenchPathKind, IReadOnlyList<NPCWorkbenchPathEntry>> Build()
        {
            return new Dictionary<NPCWorkbenchPathKind, IReadOnlyList<NPCWorkbenchPathEntry>>
            {
                [NPCWorkbenchPathKind.Hair] = ReadConstants(
                    NPCWorkbenchPathKind.Hair,
                    new[] { typeof(HairStyle) }),
                [NPCWorkbenchPathKind.FaceLayer] = ReadDerivedConstants<BaseFaceAppearance>(
                    NPCWorkbenchPathKind.FaceLayer),
                [NPCWorkbenchPathKind.BodyLayer] = ReadDerivedConstants<BaseBodyAppearance>(
                    NPCWorkbenchPathKind.BodyLayer),
                [NPCWorkbenchPathKind.Accessory] = ReadDerivedConstants<BaseAccessoryAppearance>(
                    NPCWorkbenchPathKind.Accessory)
            };
        }

        private static IReadOnlyList<NPCWorkbenchPathEntry> ReadDerivedConstants<TBase>(
            NPCWorkbenchPathKind kind)
        {
            var baseType = typeof(TBase);
            var types = baseType.Assembly.GetTypes()
                .Where(type => type != baseType && baseType.IsAssignableFrom(type));
            return ReadConstants(kind, types);
        }

        private static IReadOnlyList<NPCWorkbenchPathEntry> ReadConstants(
            NPCWorkbenchPathKind kind,
            IEnumerable<Type> types)
        {
            return types
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .SelectMany(type => type
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                    .OrderBy(field => field.Name, StringComparer.Ordinal)
                    .Select(field => new
                    {
                        Type = type,
                        Field = field,
                        Path = field.GetRawConstantValue() as string
                    }))
                .Where(value => !string.IsNullOrWhiteSpace(value.Path))
                .Select(value => new NPCWorkbenchPathEntry(
                    kind,
                    value.Type,
                    value.Field.Name,
                    value.Path!))
                .ToArray();
        }
    }
}
