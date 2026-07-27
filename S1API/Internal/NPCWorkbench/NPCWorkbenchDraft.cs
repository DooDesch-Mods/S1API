using System;
using System.Collections.Generic;
using System.Linq;

namespace S1API.Internal.NPCWorkbench
{
    internal enum NPCWorkbenchSourceKind
    {
        Blank,
        Native,
        S1API
    }

    internal enum NPCWorkbenchDiagnosticSeverity
    {
        Information,
        Warning,
        Error
    }

    internal sealed class NPCWorkbenchDraft
    {
        internal NPCWorkbenchSourceKind SourceKind { get; set; }
        internal string SourceId { get; set; } = string.Empty;
        internal string SourceDisplayName { get; set; } = "Blank appearance";
        internal NPCWorkbenchAppearance Appearance { get; } = new NPCWorkbenchAppearance();

        internal NPCWorkbenchDraft Clone()
        {
            var clone = new NPCWorkbenchDraft
            {
                SourceKind = SourceKind,
                SourceId = SourceId,
                SourceDisplayName = SourceDisplayName
            };
            clone.Appearance.CopyFrom(Appearance);
            return clone;
        }

        internal IReadOnlyList<NPCWorkbenchDiagnostic> Validate()
        {
            var diagnostics = new List<NPCWorkbenchDiagnostic>();
            Appearance.Validate(diagnostics);
            return diagnostics;
        }
    }

    internal sealed class NPCWorkbenchAppearance
    {
        internal float Gender { get; set; }
        internal float Height { get; set; } = 1f;
        internal float Weight { get; set; } = 0.5f;
        internal NPCWorkbenchColor SkinColor { get; set; } = new NPCWorkbenchColor(150, 120, 95, 255);
        internal NPCWorkbenchColor LeftEyeLidColor { get; set; } = new NPCWorkbenchColor(150, 120, 95, 255);
        internal NPCWorkbenchColor RightEyeLidColor { get; set; } = new NPCWorkbenchColor(150, 120, 95, 255);
        internal NPCWorkbenchColor EyeBallTint { get; set; } = NPCWorkbenchColor.White;
        internal string EyeballMaterialIdentifier { get; set; } = "Default";
        internal float PupilDilation { get; set; } = 1f;
        internal float EyebrowScale { get; set; } = 1f;
        internal float EyebrowThickness { get; set; } = 1f;
        internal float EyebrowRestingHeight { get; set; }
        internal float EyebrowRestingAngle { get; set; }
        internal NPCWorkbenchEyeSettings LeftEye { get; set; } = new NPCWorkbenchEyeSettings(0.5f, 0.5f);
        internal NPCWorkbenchEyeSettings RightEye { get; set; } = new NPCWorkbenchEyeSettings(0.5f, 0.5f);
        internal string HairPath { get; set; } = string.Empty;
        internal NPCWorkbenchColor HairColor { get; set; } = NPCWorkbenchColor.Black;
        internal string? ImpostorId { get; set; }
        internal List<NPCWorkbenchLayer> FaceLayers { get; } = new List<NPCWorkbenchLayer>();
        internal List<NPCWorkbenchLayer> BodyLayers { get; } = new List<NPCWorkbenchLayer>();
        internal List<NPCWorkbenchLayer> Accessories { get; } = new List<NPCWorkbenchLayer>();

        internal void CopyFrom(NPCWorkbenchAppearance source)
        {
            Gender = source.Gender;
            Height = source.Height;
            Weight = source.Weight;
            SkinColor = source.SkinColor;
            LeftEyeLidColor = source.LeftEyeLidColor;
            RightEyeLidColor = source.RightEyeLidColor;
            EyeBallTint = source.EyeBallTint;
            EyeballMaterialIdentifier = source.EyeballMaterialIdentifier;
            PupilDilation = source.PupilDilation;
            EyebrowScale = source.EyebrowScale;
            EyebrowThickness = source.EyebrowThickness;
            EyebrowRestingHeight = source.EyebrowRestingHeight;
            EyebrowRestingAngle = source.EyebrowRestingAngle;
            LeftEye = source.LeftEye;
            RightEye = source.RightEye;
            HairPath = source.HairPath;
            HairColor = source.HairColor;
            ImpostorId = source.ImpostorId;
            CopyLayers(FaceLayers, source.FaceLayers);
            CopyLayers(BodyLayers, source.BodyLayers);
            CopyLayers(Accessories, source.Accessories);
        }

        internal void Validate(List<NPCWorkbenchDiagnostic> diagnostics)
        {
            ValidateRange(diagnostics, Gender, 0f, 1f, "appearance.gender", "Gender");
            ValidateRange(diagnostics, Height, 0.5f, 2f, "appearance.height", "Height");
            ValidateRange(diagnostics, Weight, 0f, 1f, "appearance.weight", "Weight");
            ValidateRange(diagnostics, PupilDilation, 0f, 2f, "appearance.pupil", "Pupil dilation");
            ValidateRange(diagnostics, EyebrowScale, 0f, 2f, "appearance.eyebrow-scale", "Eyebrow scale");
            ValidateRange(diagnostics, EyebrowThickness, 0f, 2f, "appearance.eyebrow-thickness", "Eyebrow thickness");
            ValidateRange(
                diagnostics,
                EyebrowRestingHeight,
                -1f,
                1f,
                "appearance.eyebrow-height",
                "Eyebrow resting height");
            ValidateRange(
                diagnostics,
                EyebrowRestingAngle,
                -1f,
                1f,
                "appearance.eyebrow-angle",
                "Eyebrow resting angle");
            ValidateEye(diagnostics, LeftEye, "left");
            ValidateEye(diagnostics, RightEye, "right");
            ValidateLayers(diagnostics, FaceLayers, "face");
            ValidateLayers(diagnostics, BodyLayers, "body");
            ValidateLayers(diagnostics, Accessories, "accessory");
        }

        private static void CopyLayers(List<NPCWorkbenchLayer> target, IEnumerable<NPCWorkbenchLayer> source)
        {
            target.Clear();
            target.AddRange(source.Select(layer => layer.Clone()));
        }

        private static void ValidateEye(
            List<NPCWorkbenchDiagnostic> diagnostics,
            NPCWorkbenchEyeSettings eye,
            string side)
        {
            ValidateRange(
                diagnostics,
                eye.TopLidOpen,
                0f,
                1f,
                $"appearance.{side}-eye-top",
                $"{side} top eyelid");
            ValidateRange(
                diagnostics,
                eye.BottomLidOpen,
                0f,
                1f,
                $"appearance.{side}-eye-bottom",
                $"{side} bottom eyelid");
        }

        private static void ValidateLayers(
            List<NPCWorkbenchDiagnostic> diagnostics,
            IReadOnlyList<NPCWorkbenchLayer> layers,
            string kind)
        {
            for (var index = 0; index < layers.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(layers[index].Path))
                {
                    diagnostics.Add(NPCWorkbenchDiagnostic.Error(
                        $"appearance.{kind}-layer.path",
                        $"{kind} layer {index + 1} requires a resource path."));
                }
            }
        }

        private static void ValidateRange(
            List<NPCWorkbenchDiagnostic> diagnostics,
            float value,
            float minimum,
            float maximum,
            string code,
            string label)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum || value > maximum)
                diagnostics.Add(NPCWorkbenchDiagnostic.Error(code, $"{label} must be between {minimum} and {maximum}."));
        }
    }

    internal sealed class NPCWorkbenchLayer
    {
        internal string Path { get; set; } = string.Empty;
        internal NPCWorkbenchColor Color { get; set; } = NPCWorkbenchColor.White;

        internal NPCWorkbenchLayer Clone() => new NPCWorkbenchLayer
        {
            Path = Path,
            Color = Color
        };
    }

    internal readonly struct NPCWorkbenchEyeSettings
    {
        internal NPCWorkbenchEyeSettings(float topLidOpen, float bottomLidOpen)
        {
            TopLidOpen = topLidOpen;
            BottomLidOpen = bottomLidOpen;
        }

        internal float TopLidOpen { get; }
        internal float BottomLidOpen { get; }
    }

    internal readonly struct NPCWorkbenchColor
    {
        internal static NPCWorkbenchColor Black => new NPCWorkbenchColor(0, 0, 0, 255);
        internal static NPCWorkbenchColor White => new NPCWorkbenchColor(255, 255, 255, 255);

        internal NPCWorkbenchColor(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        internal byte Red { get; }
        internal byte Green { get; }
        internal byte Blue { get; }
        internal byte Alpha { get; }

        internal string ToHex() => $"{Red:X2}{Green:X2}{Blue:X2}{Alpha:X2}";

        internal static bool TryParse(string? value, out NPCWorkbenchColor color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var text = value.Trim().TrimStart('#');
            if (text.Length == 6)
                text += "FF";
            if (text.Length != 8)
                return false;

            if (!byte.TryParse(text.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var red) ||
                !byte.TryParse(text.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var green) ||
                !byte.TryParse(text.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var blue) ||
                !byte.TryParse(text.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out var alpha))
            {
                return false;
            }

            color = new NPCWorkbenchColor(red, green, blue, alpha);
            return true;
        }
    }

    internal sealed class NPCWorkbenchDiagnostic
    {
        private NPCWorkbenchDiagnostic(
            NPCWorkbenchDiagnosticSeverity severity,
            string code,
            string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        internal NPCWorkbenchDiagnosticSeverity Severity { get; }
        internal string Code { get; }
        internal string Message { get; }

        internal static NPCWorkbenchDiagnostic Error(string code, string message) =>
            new NPCWorkbenchDiagnostic(NPCWorkbenchDiagnosticSeverity.Error, code, message);
    }
}
