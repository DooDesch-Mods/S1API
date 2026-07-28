using System;
using System.Collections.Generic;
using System.Globalization;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace S1API.Internal.Rendering
{
    internal sealed class PresentationWorkbenchView : IDisposable
    {
        private readonly List<ButtonBinding> _buttonBindings =
            new List<ButtonBinding>();
        private readonly List<InputBinding> _inputBindings =
            new List<InputBinding>();
        private readonly GameObject _root;
        private readonly Transform _modeRow;
        private readonly Text _contextText;
        private readonly RawImage _preview;
        private readonly GameObject _previewPanel;
        private readonly InputField[] _positionFields;
        private readonly InputField[] _rotationFields;
        private readonly InputField[] _scaleFields;
        private readonly GameObject _positionRow;
        private readonly GameObject _scaleRow;
        private readonly GameObject _iconRow;
        private readonly Text _fitLabel;
        private readonly InputField _cameraFillField;
        private readonly Text _status;

        internal PresentationWorkbenchView(
            PresentationWorkbenchDefinition definition,
            Action<PresentationWorkbenchMode> selectMode,
            Action<Vector3, Vector3, Vector3> updateTransform,
            Action toggleFit,
            Action<float> updateCameraFill,
            Action reset,
            Action copy,
            Action close)
        {
            _root = new GameObject("S1API Presentation Workbench");
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            _root.AddComponent<GraphicRaycaster>();
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject shell = Panel(
                "Shell",
                _root.transform,
                new Color(0.035f, 0.04f, 0.05f, 0.97f));
            var shellRect = shell.GetComponent<RectTransform>();
            shellRect.anchorMin = new Vector2(0.025f, 0.04f);
            shellRect.anchorMax = new Vector2(0.975f, 0.96f);
            shellRect.offsetMin = Vector2.zero;
            shellRect.offsetMax = Vector2.zero;

            Text title = CreateText(
                "Title",
                $"{definition.DisplayName}\n<size=18><color=#92A1B5>{definition.Id}</color></size>",
                shell.transform,
                28,
                TextAnchor.UpperLeft,
                FontStyle.Bold);
            SetRect(title.rectTransform, 0.025f, 0.88f, 0.72f, 0.975f);

            Button closeButton = CreateButton("Close", "Close", shell.transform);
            SetRect(
                closeButton.GetComponent<RectTransform>(),
                0.87f,
                0.91f,
                0.97f,
                0.97f);
            Bind(closeButton, close);

            GameObject controls = Panel(
                "Controls",
                shell.transform,
                new Color(0.075f, 0.085f, 0.105f, 1f));
            SetRect(
                controls.GetComponent<RectTransform>(),
                0.025f,
                0.05f,
                0.47f,
                0.865f);

            GameObject modes = new GameObject("Modes");
            modes.transform.SetParent(controls.transform, false);
            modes.AddComponent<RectTransform>();
            _modeRow = modes.transform;
            SetRect(
                modes.GetComponent<RectTransform>(),
                0.04f,
                0.88f,
                0.96f,
                0.965f);

            float nextModeX = 0f;
            AddModeButton(
                definition.SupportsFirstPerson,
                "First person",
                PresentationWorkbenchMode.FirstPerson,
                ref nextModeX,
                selectMode);
            AddModeButton(
                definition.SupportsAvatar,
                "Avatar",
                PresentationWorkbenchMode.Avatar,
                ref nextModeX,
                selectMode);
            AddModeButton(
                definition.SupportsIcon,
                "Icon",
                PresentationWorkbenchMode.Icon,
                ref nextModeX,
                selectMode);

            _contextText = CreateText(
                "Context",
                string.Empty,
                controls.transform,
                18,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            SetRect(_contextText.rectTransform, 0.04f, 0.81f, 0.96f, 0.875f);

            _positionRow = CreateVectorRow(
                "Position",
                controls.transform,
                0.68f,
                out _positionFields);
            GameObject rotationRow = CreateVectorRow(
                "Rotation",
                controls.transform,
                0.54f,
                out _rotationFields);
            _scaleRow = CreateVectorRow(
                "Scale",
                controls.transform,
                0.40f,
                out _scaleFields);

            BindVectorFields(
                _positionFields,
                _rotationFields,
                _scaleFields,
                updateTransform);

            _iconRow = new GameObject("IconSettings");
            _iconRow.transform.SetParent(controls.transform, false);
            _iconRow.AddComponent<RectTransform>();
            SetRect(
                _iconRow.GetComponent<RectTransform>(),
                0.04f,
                0.28f,
                0.96f,
                0.385f);

            Button fitButton = CreateButton(
                "Fit",
                "Fit: On",
                _iconRow.transform);
            SetRect(
                fitButton.GetComponent<RectTransform>(),
                0f,
                0.12f,
                0.39f,
                0.88f);
            _fitLabel = fitButton.GetComponentInChildren<Text>();
            Bind(fitButton, toggleFit);

            _cameraFillField = CreateInput(
                "CameraFill",
                _iconRow.transform,
                "Fill");
            SetRect(
                _cameraFillField.GetComponent<RectTransform>(),
                0.43f,
                0.12f,
                1f,
                0.88f);
            Bind(
                _cameraFillField,
                value =>
                {
                    if (TryParse(value, out float parsed))
                    {
                        updateCameraFill(parsed);
                    }
                    else if (!IsIncompleteNumericInput(value))
                    {
                        SetStatus("Camera fill must be a finite number.");
                    }
                },
                live: true);

            Button resetButton =
                CreateButton("Reset", "Reset", controls.transform);
            SetRect(
                resetButton.GetComponent<RectTransform>(),
                0.04f,
                0.17f,
                0.47f,
                0.245f);
            Bind(resetButton, reset);

            Button copyButton =
                CreateButton("Copy", "Copy C#", controls.transform);
            SetRect(
                copyButton.GetComponent<RectTransform>(),
                0.53f,
                0.17f,
                0.96f,
                0.245f);
            Bind(copyButton, copy);

            _status = CreateText(
                "Status",
                "Preview edits are temporary. Copy C# to persist them.",
                controls.transform,
                15,
                TextAnchor.UpperLeft,
                FontStyle.Normal);
            SetRect(_status.rectTransform, 0.04f, 0.025f, 0.96f, 0.145f);

            _previewPanel = Panel(
                "Preview",
                shell.transform,
                new Color(0.015f, 0.018f, 0.022f, 1f));
            SetRect(
                _previewPanel.GetComponent<RectTransform>(),
                0.495f,
                0.05f,
                0.975f,
                0.865f);
            var previewTexture = new GameObject("PreviewTexture");
            previewTexture.transform.SetParent(_previewPanel.transform, false);
            var previewRect = previewTexture.AddComponent<RectTransform>();
            SetRect(previewRect, 0.025f, 0.025f, 0.975f, 0.975f);
            _preview = previewTexture.AddComponent<RawImage>();
            _preview.color = Color.white;
        }

        internal void SetMode(
            PresentationWorkbenchMode mode,
            PresentationWorkbenchTransform transform,
            bool fitToCamera,
            float cameraFill)
        {
            _contextText.text =
                mode == PresentationWorkbenchMode.FirstPerson
                    ? "First-person viewmodel"
                    : mode == PresentationWorkbenchMode.Avatar
                        ? "Third-person avatar"
                        : "Native icon capture";
            bool icon = mode == PresentationWorkbenchMode.Icon;
            _positionRow.SetActive(!icon);
            _scaleRow.SetActive(!icon || !fitToCamera);
            _iconRow.SetActive(icon);
            _previewPanel.SetActive(mode != PresentationWorkbenchMode.FirstPerson);
            _fitLabel.text = fitToCamera ? "Fit: On" : "Fit: Off";
            _cameraFillField.SetTextWithoutNotify(Format(cameraFill));
            SetVector(_positionFields, transform.LocalPosition);
            SetVector(_rotationFields, transform.LocalEulerAngles);
            SetVector(_scaleFields, transform.LocalScale);
            SetStatus(
                mode == PresentationWorkbenchMode.Avatar
                    ? "Drag to orbit and scroll to zoom. Preview edits are temporary."
                    : "Preview edits update live. Copy C# to persist them.");
        }

        internal void SetPreview(Texture? texture)
        {
            _preview.texture = texture;
        }

        internal bool IsPointerOverPreview(Vector2 screenPoint) =>
            _previewPanel.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(
                _preview.rectTransform,
                screenPoint);

        internal void SetStatus(string message)
        {
            _status.text = message;
        }

        public void Dispose()
        {
            foreach (ButtonBinding binding in _buttonBindings)
                EventHelper.RemoveListener(binding.Action, binding.Button.onClick);
            foreach (InputBinding binding in _inputBindings)
            {
                EventHelper.RemoveListener(
                    binding.Action,
                    binding.Live
                        ? binding.Input.onValueChanged
                        : binding.Input.onEndEdit);
            }

            _buttonBindings.Clear();
            _inputBindings.Clear();
            Object.Destroy(_root);
        }

        private void AddModeButton(
            bool supported,
            string label,
            PresentationWorkbenchMode mode,
            ref float nextX,
            Action<PresentationWorkbenchMode> selectMode)
        {
            if (!supported)
                return;

            Button button = CreateButton(mode.ToString(), label, _modeRow);
            SetRect(
                button.GetComponent<RectTransform>(),
                nextX,
                0f,
                Mathf.Min(nextX + 0.31f, 1f),
                1f);
            Bind(button, () => selectMode(mode));
            nextX += 0.335f;
        }

        private void BindVectorFields(
            IReadOnlyList<InputField> position,
            IReadOnlyList<InputField> rotation,
            IReadOnlyList<InputField> scale,
            Action<Vector3, Vector3, Vector3> update)
        {
            void Apply(string _)
            {
                if (TryRead(position, out Vector3 parsedPosition) &&
                    TryRead(rotation, out Vector3 parsedRotation) &&
                    TryRead(scale, out Vector3 parsedScale))
                {
                    update(parsedPosition, parsedRotation, parsedScale);
                }
                else if (!HasIncompleteNumericInput(position) &&
                         !HasIncompleteNumericInput(rotation) &&
                         !HasIncompleteNumericInput(scale))
                {
                    SetStatus("Enter finite numeric values for every axis.");
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Bind(position[i], Apply, live: true);
                Bind(rotation[i], Apply, live: true);
                Bind(scale[i], Apply, live: true);
            }
        }

        private void Bind(Button button, Action action)
        {
            EventHelper.AddListener(action, button.onClick);
            _buttonBindings.Add(new ButtonBinding(button, action));
        }

        private void Bind(
            InputField input,
            Action<string> action,
            bool live = false)
        {
            Action<string> bindingAction = CreateInputBindingAction(action);
            EventHelper.AddListener(
                bindingAction,
                live ? input.onValueChanged : input.onEndEdit);
            _inputBindings.Add(new InputBinding(input, bindingAction, live));
        }

        internal static Action<string> CreateInputBindingAction(
            Action<string> action) =>
            value => action(value);

        private static GameObject CreateVectorRow(
            string label,
            Transform parent,
            float y,
            out InputField[] fields)
        {
            var row = new GameObject(label);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            SetRect(row.GetComponent<RectTransform>(), 0.04f, y, 0.96f, y + 0.115f);

            Text text = CreateText(
                "Label",
                label,
                row.transform,
                15,
                TextAnchor.UpperLeft,
                FontStyle.Bold);
            SetRect(text.rectTransform, 0f, 0.62f, 1f, 1f);

            fields = new InputField[3];
            string[] axes = { "X", "Y", "Z" };
            for (int i = 0; i < axes.Length; i++)
            {
                fields[i] = CreateInput(axes[i], row.transform, axes[i]);
                float min = i * 0.335f;
                SetRect(
                    fields[i].GetComponent<RectTransform>(),
                    min,
                    0f,
                    Mathf.Min(min + 0.31f, 1f),
                    0.56f);
            }

            return row;
        }

        private static InputField CreateInput(
            string name,
            Transform parent,
            string placeholder)
        {
            GameObject root = Panel(
                name,
                parent,
                new Color(0.12f, 0.135f, 0.16f, 1f));
            var input = root.AddComponent<InputField>();
            Text text = CreateText(
                "Text",
                string.Empty,
                root.transform,
                16,
                TextAnchor.MiddleLeft,
                FontStyle.Normal);
            SetRect(text.rectTransform, 0.06f, 0f, 0.94f, 1f);
            Text hint = CreateText(
                "Placeholder",
                placeholder,
                root.transform,
                16,
                TextAnchor.MiddleLeft,
                FontStyle.Italic);
            hint.color = new Color(0.55f, 0.6f, 0.67f, 1f);
            SetRect(hint.rectTransform, 0.06f, 0f, 0.94f, 1f);
            input.textComponent = text;
            input.placeholder = hint;
            input.contentType = InputField.ContentType.DecimalNumber;
            return input;
        }

        private static Button CreateButton(
            string name,
            string label,
            Transform parent)
        {
            GameObject root = Panel(
                name,
                parent,
                new Color(0.16f, 0.32f, 0.5f, 1f));
            var button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            Text text = CreateText(
                "Label",
                label,
                root.transform,
                16,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);
            SetRect(text.rectTransform, 0f, 0f, 1f, 1f);
            return button;
        }

        private static GameObject Panel(
            string name,
            Transform parent,
            Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();
            root.AddComponent<Image>().color = color;
            return root;
        }

        private static Text CreateText(
            string name,
            string value,
            Transform parent,
            int size,
            TextAnchor anchor,
            FontStyle style)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var text = root.AddComponent<Text>();
            text.text = value;
            text.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetVector(InputField[] fields, Vector3 value)
        {
            fields[0].SetTextWithoutNotify(Format(value.x));
            fields[1].SetTextWithoutNotify(Format(value.y));
            fields[2].SetTextWithoutNotify(Format(value.z));
        }

        private static bool TryRead(
            IReadOnlyList<InputField> fields,
            out Vector3 result)
        {
            if (TryParse(fields[0].text, out float x) &&
                TryParse(fields[1].text, out float y) &&
                TryParse(fields[2].text, out float z))
            {
                result = new Vector3(x, y, z);
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        private static bool TryParse(string value, out float result) =>
            float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result) &&
            !float.IsNaN(result) &&
            !float.IsInfinity(result);

        internal static bool IsIncompleteNumericInput(string value)
        {
            string text = value?.Trim() ?? string.Empty;
            if (text.Length == 0 ||
                text == "+" ||
                text == "-" ||
                text == "." ||
                text == "+." ||
                text == "-.")
            {
                return true;
            }

            int exponentIndex = text.IndexOf('e');
            if (exponentIndex < 0)
            {
                exponentIndex = text.IndexOf('E');
            }

            if (exponentIndex < 0)
            {
                return false;
            }

            int exponentLength = text.Length - exponentIndex - 1;
            return exponentLength == 0 ||
                   (exponentLength == 1 &&
                    (text[text.Length - 1] == '+' ||
                     text[text.Length - 1] == '-'));
        }

        private static bool HasIncompleteNumericInput(
            IReadOnlyList<InputField> fields)
        {
            for (int index = 0; index < fields.Count; index++)
            {
                if (IsIncompleteNumericInput(fields[index].text))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Format(float value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private sealed class ButtonBinding
        {
            internal ButtonBinding(Button button, Action action)
            {
                Button = button;
                Action = action;
            }

            internal Button Button { get; }

            internal Action Action { get; }
        }

        private sealed class InputBinding
        {
            internal InputBinding(
                InputField input,
                Action<string> action,
                bool live)
            {
                Input = input;
                Action = action;
                Live = live;
            }

            internal InputField Input { get; }

            internal Action<string> Action { get; }

            internal bool Live { get; }
        }
    }
}
