using System.Globalization;
using System.Text;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal static class PresentationWorkbenchExporter
    {
        internal static string FormatTransform(
            PresentationWorkbenchMode mode,
            Vector3 position,
            Vector3 eulerAngles,
            Vector3 scale)
        {
            string typeName =
                mode == PresentationWorkbenchMode.Avatar
                    ? "ProductPresentationTransform"
                    : "PresentationWorkbenchTransform";
            return
                $"new {typeName}(\n" +
                $"    {FormatVector(position)},\n" +
                $"    {FormatVector(eulerAngles)},\n" +
                $"    {FormatVector(scale)})";
        }

        internal static string FormatIcon(
            Vector3 eulerAngles,
            Vector3 scale,
            bool fitToCamera,
            float cameraFill,
            int size)
        {
            var result = new StringBuilder();
            result.AppendLine(".WithIconPreview(");
            result.AppendLine("    provider,");
            result.Append("    ").Append(FormatVector(eulerAngles)).AppendLine(",");
            result.Append("    fitToCamera: ")
                .Append(fitToCamera ? "true" : "false")
                .AppendLine(",");
            result.Append("    cameraFill: ")
                .Append(FormatFloat(cameraFill))
                .AppendLine(",");
            result.Append("    size: ")
                .Append(size.ToString(CultureInfo.InvariantCulture))
                .AppendLine(",");
            result.Append("    initialScale: ")
                .Append(FormatVector(scale))
                .Append(")");
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
