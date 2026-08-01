using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace S1API.Internal.NPCWorkbench
{
    internal sealed class NPCWorkbenchExportResult
    {
        internal NPCWorkbenchExportResult(string code, IReadOnlyList<NPCWorkbenchDiagnostic> diagnostics)
        {
            Code = code;
            Diagnostics = diagnostics;
        }

        internal string Code { get; }
        internal IReadOnlyList<NPCWorkbenchDiagnostic> Diagnostics { get; }
        internal bool CanExport => Diagnostics.All(
            diagnostic => diagnostic.Severity != NPCWorkbenchDiagnosticSeverity.Error);
    }

    internal static class NPCWorkbenchExporter
    {
        internal static NPCWorkbenchExportResult Export(NPCWorkbenchDraft draft)
        {
            if (draft == null)
                throw new ArgumentNullException(nameof(draft));

            var diagnostics = draft.Validate();
            if (diagnostics.Any(diagnostic => diagnostic.Severity == NPCWorkbenchDiagnosticSeverity.Error))
                return new NPCWorkbenchExportResult(string.Empty, diagnostics);

            var appearance = draft.Appearance;
            var code = new StringBuilder();
            code.AppendLine("builder.WithAppearanceDefaults(appearance =>");
            code.AppendLine("{");
            code.Append("    appearance.Gender = ").Append(Float(appearance.Gender)).AppendLine(";");
            code.Append("    appearance.Height = ").Append(Float(appearance.Height)).AppendLine(";");
            code.Append("    appearance.Weight = ").Append(Float(appearance.Weight)).AppendLine(";");
            code.Append("    appearance.SkinColor = ").Append(Color(appearance.SkinColor)).AppendLine(";");
            code.Append("    appearance.LeftEyeLidColor = ").Append(Color(appearance.LeftEyeLidColor)).AppendLine(";");
            code.Append("    appearance.RightEyeLidColor = ").Append(Color(appearance.RightEyeLidColor)).AppendLine(";");
            code.Append("    appearance.EyeBallTint = ").Append(Color(appearance.EyeBallTint)).AppendLine(";");
            code.Append("    appearance.EyeballMaterialIdentifier = ")
                .Append(Quote(appearance.EyeballMaterialIdentifier)).AppendLine(";");
            code.Append("    appearance.PupilDilation = ").Append(Float(appearance.PupilDilation)).AppendLine(";");
            code.Append("    appearance.EyebrowScale = ").Append(Float(appearance.EyebrowScale)).AppendLine(";");
            code.Append("    appearance.EyebrowThickness = ").Append(Float(appearance.EyebrowThickness)).AppendLine(";");
            code.Append("    appearance.EyebrowRestingHeight = ")
                .Append(Float(appearance.EyebrowRestingHeight)).AppendLine(";");
            code.Append("    appearance.EyebrowRestingAngle = ")
                .Append(Float(appearance.EyebrowRestingAngle)).AppendLine(";");
            code.Append("    appearance.LeftEye = (")
                .Append(Float(appearance.LeftEye.TopLidOpen)).Append(", ")
                .Append(Float(appearance.LeftEye.BottomLidOpen)).AppendLine(");");
            code.Append("    appearance.RightEye = (")
                .Append(Float(appearance.RightEye.TopLidOpen)).Append(", ")
                .Append(Float(appearance.RightEye.BottomLidOpen)).AppendLine(");");
            code.Append("    appearance.HairPath = ")
                .Append(PathExpression(NPCWorkbenchPathKind.Hair, appearance.HairPath))
                .AppendLine(";");
            code.Append("    appearance.HairColor = ").Append(Color(appearance.HairColor)).AppendLine(";");
            AppendLayers(code, NPCWorkbenchPathKind.FaceLayer, "WithFaceLayer", appearance.FaceLayers);
            AppendLayers(code, NPCWorkbenchPathKind.BodyLayer, "WithBodyLayer", appearance.BodyLayers);
            AppendLayers(code, NPCWorkbenchPathKind.Accessory, "WithAccessoryLayer", appearance.Accessories);

            if (!string.IsNullOrWhiteSpace(appearance.ImpostorId))
                code.Append("    appearance.WithImpostor(").Append(Quote(appearance.ImpostorId!)).AppendLine(");");

            code.AppendLine("});");
            return new NPCWorkbenchExportResult(code.ToString(), diagnostics);
        }

        private static void AppendLayers(
            StringBuilder code,
            NPCWorkbenchPathKind kind,
            string method,
            IEnumerable<NPCWorkbenchLayer> layers)
        {
            foreach (var layer in layers)
            {
                var catalogEntry = NPCWorkbenchPathCatalog.Find(kind, layer.Path);
                code.Append("    appearance.")
                    .Append(method);
                if (catalogEntry != null)
                    code.Append("<").Append(catalogEntry.CSharpTypeName).Append(">");

                code.Append("(")
                    .Append(catalogEntry?.CSharpExpression ?? Quote(layer.Path)).Append(", ")
                    .Append(Color(layer.Color)).AppendLine(");");
            }
        }

        private static string PathExpression(NPCWorkbenchPathKind kind, string path) =>
            NPCWorkbenchPathCatalog.Find(kind, path)?.CSharpExpression ?? Quote(path);

        private static string Quote(string value) =>
            "\"" + (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t") + "\"";

        private static string Float(float value) =>
            value.ToString("0.0#######", CultureInfo.InvariantCulture) + "f";

        private static string Color(NPCWorkbenchColor value) =>
            $"new Color32({value.Red}, {value.Green}, {value.Blue}, {value.Alpha})";
    }
}
