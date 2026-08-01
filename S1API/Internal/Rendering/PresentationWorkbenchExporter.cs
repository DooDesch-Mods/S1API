using System.Globalization;
using System.Text;
using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal static class PresentationWorkbenchExporter
    {
        internal static string FormatTransform(
            PresentationWorkbenchExportKind exportKind,
            Vector3 position,
            Vector3 eulerAngles,
            Vector3 scale)
        {
            if (exportKind == PresentationWorkbenchExportKind.ProductTransform)
            {
                return
                    "new ProductPresentationTransform(\n" +
                    $"    {FormatVector(position)},\n" +
                    $"    {FormatVector(eulerAngles)},\n" +
                    $"    {FormatVector(scale)})";
            }

            return
                $"visual.transform.localPosition = {FormatVector(position)};\n" +
                $"visual.transform.localEulerAngles = {FormatVector(eulerAngles)};\n" +
                $"visual.transform.localScale = {FormatVector(scale)};";
        }

        internal static string FormatIcon(
            Vector3 eulerAngles,
            Vector3 scale,
            bool fitToCamera,
            float cameraFill,
            int size)
        {
            var result = new StringBuilder();
            result.Append("iconSource.transform.localEulerAngles = ")
                .Append(FormatVector(eulerAngles))
                .AppendLine(";");
            result.Append("iconSource.transform.localScale = ")
                .Append(FormatVector(scale))
                .AppendLine(";");
            result.AppendLine("IconFactory.GenerateIconSprite(");
            result.AppendLine("    iconSource.transform,");
            result.Append("    size: ")
                .Append(size.ToString(CultureInfo.InvariantCulture))
                .AppendLine(",");
            result.AppendLine("    bakeSkinnedMeshes: true,");
            result.Append("    fitToCamera: ")
                .Append(fitToCamera ? "true" : "false")
                .AppendLine(",");
            result.Append("    cameraFill: ")
                .Append(FormatFloat(cameraFill))
                .Append(");");
            return result.ToString();
        }

        private static string FormatVector(Vector3 value) =>
            $"new Vector3({FormatFloat(value.x)}, {FormatFloat(value.y)}, " +
            $"{FormatFloat(value.z)})";

        private static string FormatFloat(float value)
        {
            if (Mathf.Abs(value) < 0.0000005f)
                value = 0f;

            return value.ToString("0.######", CultureInfo.InvariantCulture) + "f";
        }
    }
}
