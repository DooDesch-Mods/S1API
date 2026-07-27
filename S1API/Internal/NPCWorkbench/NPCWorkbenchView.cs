#if IL2CPPMELON
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using S1API.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace S1API.Internal.NPCWorkbench
{
    internal sealed class NPCWorkbenchView : IDisposable
    {
        private static readonly Log Logger = new Log("NPCWorkbench");
        private static readonly Color PanelColor = new Color(0.075f, 0.09f, 0.12f, 0.98f);
        private static readonly Color RaisedColor = new Color(0.12f, 0.15f, 0.19f, 1f);
        private static readonly Color AccentColor = new Color(0.18f, 0.55f, 0.84f, 1f);
        private readonly GameObject _root;
        private readonly RectTransform _editorContent;
        private readonly RawImage _previewImage;
        private readonly Text _sourceText;
        private readonly Text _statusText;
        private readonly Text _codeText;
        private readonly NPCWorkbenchPreview? _preview;
        private readonly CursorLockMode _previousCursorLock;
        private readonly bool _previousCursorVisible;
        private readonly bool? _previousCanLook;
        private readonly bool? _previousCanMove;
        private NPCWorkbenchDraft _draft = new NPCWorkbenchDraft();
        private Vector3 _lastMousePosition;
        private bool _inputCaptured;
        private bool _disposed;

        private NPCWorkbenchView(
            GameObject root,
            RectTransform editorContent,
            RawImage previewImage,
            Text sourceText,
            Text statusText,
            Text codeText,
            NPCWorkbenchPreview? preview,
            CursorLockMode previousCursorLock,
            bool previousCursorVisible,
            bool? previousCanLook,
            bool? previousCanMove)
        {
            _root = root;
            _editorContent = editorContent;
            _previewImage = previewImage;
            _sourceText = sourceText;
            _statusText = statusText;
            _codeText = codeText;
            _preview = preview;
            _previousCursorLock = previousCursorLock;
            _previousCursorVisible = previousCursorVisible;
            _previousCanLook = previousCanLook;
            _previousCanMove = previousCanMove;
            BuildEditor();
            Refresh();
        }

        internal static NPCWorkbenchView Create()
        {
            var previousCursorLock = Cursor.lockState;
            var previousCursorVisible = Cursor.visible;
            bool? previousCanLook = null;
            bool? previousCanMove = null;

            if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.InstanceExists)
            {
                var camera = S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.Instance;
                previousCanLook = camera.CanLook;
            }

            if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.InstanceExists)
            {
                var movement = S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.Instance;
                previousCanMove = movement.CanMove;
            }

            var root = new GameObject("S1API NPC Workbench");
            Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var dim = Panel("Backdrop", root.transform, new Color(0.015f, 0.02f, 0.03f, 0.94f));
            Stretch(dim.GetComponent<RectTransform>());

            var shell = Panel("Shell", dim.transform, PanelColor);
            var shellRect = shell.GetComponent<RectTransform>();
            shellRect.anchorMin = new Vector2(0.03f, 0.04f);
            shellRect.anchorMax = new Vector2(0.97f, 0.96f);
            shellRect.offsetMin = Vector2.zero;
            shellRect.offsetMax = Vector2.zero;

            var header = Panel("Header", shell.transform, new Color(0.055f, 0.07f, 0.095f, 1f));
            Anchor(header.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 0f, -76f, 0f, 0f);
            var title = Label("Title", header.transform, "NPC Appearance Workbench", 28, TextAnchor.MiddleLeft, FontStyle.Bold);
            Anchor(title.rectTransform, 0f, 0f, 0.4f, 1f, 28f, 0f, 0f, 0f);
            var sourceText = Label("Source", header.transform, "Blank draft", 15, TextAnchor.MiddleCenter);
            sourceText.color = new Color(0.62f, 0.72f, 0.82f);
            Anchor(sourceText.rectTransform, 0.35f, 0f, 0.72f, 1f, 0f, 0f, 0f, 0f);
            var closeButton = Button("Close", header.transform, "Close", new Color(0.5f, 0.16f, 0.17f));
            Anchor(closeButton.GetComponent<RectTransform>(), 1f, 0.5f, 1f, 0.5f, -132f, -22f, -24f, 22f);

            var editor = Panel("Editor", shell.transform, new Color(0.055f, 0.067f, 0.085f, 1f));
            Anchor(editor.GetComponent<RectTransform>(), 0f, 0f, 0.34f, 1f, 16f, 16f, -8f, -92f);
            var editorScroll = Scroll("Editor Scroll", editor.transform, out var editorContent);

            var previewPanel = Panel("Preview", shell.transform, new Color(0.035f, 0.045f, 0.06f, 1f));
            Anchor(previewPanel.GetComponent<RectTransform>(), 0.34f, 0f, 0.69f, 1f, 8f, 16f, -8f, -92f);
            var previewImageObject = new GameObject("Avatar Preview");
            previewImageObject.transform.SetParent(previewPanel.transform, false);
            var previewRect = previewImageObject.AddComponent<RectTransform>();
            Anchor(previewRect, 0f, 0.12f, 1f, 1f, 12f, 0f, -12f, -12f);
            var previewImage = previewImageObject.AddComponent<RawImage>();
            previewImage.color = Color.white;

            var previewHint = Label(
                "Preview Hint",
                previewPanel.transform,
                "Drag to orbit  |  Wheel to zoom",
                14,
                TextAnchor.MiddleCenter);
            previewHint.color = new Color(0.62f, 0.68f, 0.75f);
            Anchor(previewHint.rectTransform, 0f, 0.07f, 1f, 0.12f, 8f, 0f, -8f, 0f);
            var poseRow = Panel("Pose Row", previewPanel.transform, Color.clear);
            Anchor(poseRow.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.07f, 12f, 8f, -12f, -4f);
            AddHorizontalLayout(poseRow, 8);
            var standButton = Button("Stand", poseRow.transform, "Stand", RaisedColor);
            var sitButton = Button("Sit", poseRow.transform, "Sit", RaisedColor);
            var crouchButton = Button("Crouch", poseRow.transform, "Crouch", RaisedColor);
            var resetButton = Button("Reset View", poseRow.transform, "Reset View", RaisedColor);
            ConfigureFlexibleButton(standButton);
            ConfigureFlexibleButton(sitButton);
            ConfigureFlexibleButton(crouchButton);
            ConfigureFlexibleButton(resetButton);

            var output = Panel("Output", shell.transform, new Color(0.055f, 0.067f, 0.085f, 1f));
            Anchor(output.GetComponent<RectTransform>(), 0.69f, 0f, 1f, 1f, 8f, 16f, -16f, -92f);
            var outputTitle = Label("Output Title", output.transform, "Appearance C# export", 19, TextAnchor.MiddleLeft, FontStyle.Bold);
            Anchor(outputTitle.rectTransform, 0f, 1f, 1f, 1f, 16f, -54f, -16f, -10f);
            var statusText = Label("Diagnostics", output.transform, string.Empty, 14, TextAnchor.UpperLeft);
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Truncate;
            Anchor(statusText.rectTransform, 0f, 0.78f, 1f, 0.95f, 16f, 0f, -16f, 0f);
            var codeScroll = Scroll("Code Scroll", output.transform, out var codeContent);
            Anchor(codeScroll.GetComponent<RectTransform>(), 0f, 0.12f, 1f, 0.78f, 12f, 0f, -12f, 0f);
            var codeText = Label("Code", codeContent, string.Empty, 12, TextAnchor.UpperLeft);
            codeText.font = BuiltinFont();
            codeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            codeText.verticalOverflow = VerticalWrapMode.Overflow;
            codeText.gameObject.AddComponent<LayoutElement>().minHeight = 500f;
            var exportRow = Panel("Export Row", output.transform, Color.clear);
            Anchor(exportRow.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.12f, 12f, 12f, -12f, -8f);
            AddHorizontalLayout(exportRow, 8);
            var copyButton = Button("Copy", exportRow.transform, "Copy C#", AccentColor);
            var validateButton = Button("Validate", exportRow.transform, "Validate", RaisedColor);
            ConfigureFlexibleButton(copyButton);
            ConfigureFlexibleButton(validateButton);

            NPCWorkbenchPreview.TryCreate(out var preview, out var previewFailure);
            if (preview != null)
                previewImage.texture = preview.Texture;
            else
                statusText.text = previewFailure;

            var view = new NPCWorkbenchView(
                root,
                editorContent,
                previewImage,
                sourceText,
                statusText,
                codeText,
                preview,
                previousCursorLock,
                previousCursorVisible,
                previousCanLook,
                previousCanMove);
            closeButton.GetComponent<Button>().onClick.AddListener((UnityAction)view.Dispose);
            standButton.GetComponent<Button>().onClick.AddListener((UnityAction)(() => view._preview?.SetPose(0)));
            sitButton.GetComponent<Button>().onClick.AddListener((UnityAction)(() => view._preview?.SetPose(1)));
            crouchButton.GetComponent<Button>().onClick.AddListener((UnityAction)(() => view._preview?.SetPose(2)));
            resetButton.GetComponent<Button>().onClick.AddListener((UnityAction)(() => view._preview?.ResetView()));
            copyButton.GetComponent<Button>().onClick.AddListener((UnityAction)view.CopyExport);
            validateButton.GetComponent<Button>().onClick.AddListener((UnityAction)view.Refresh);
            view.ActivateInputCapture();
            return view;
        }

        private void ActivateInputCapture()
        {
            _inputCaptured = true;
            try
            {
                if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.InstanceExists)
                {
                    var camera = S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.Instance;
                    camera.AddActiveUIElement("S1API NPC Workbench");
                    camera.SetCanLook(false);
                }
                if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.InstanceExists)
                    S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.Instance.CanMove = false;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Tick()
        {
            if (_disposed)
                return;

            _preview?.Tick();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                Dispose();
                return;
            }

            var mouse = UnityEngine.Input.mousePosition;
            var inside = RectTransformUtility.RectangleContainsScreenPoint(_previewImage.rectTransform, mouse);
            if (inside && UnityEngine.Input.GetMouseButton(0))
            {
                var delta = mouse - _lastMousePosition;
                _preview?.Orbit(delta.x * 0.35f, delta.y * 0.35f);
            }
            if (inside)
                _preview?.Zoom(UnityEngine.Input.mouseScrollDelta.y * 0.2f);
            _lastMousePosition = mouse;
        }

        private void BuildEditor()
        {
            ClearChildren(_editorContent);
            Section("Source");
            var sourceRow = Row("Source Buttons");
            AddButton(sourceRow, "Blank", () =>
            {
                _draft = new NPCWorkbenchDraft();
                BuildEditor();
                Refresh();
            });
            AddButton(sourceRow, "Import native", () => ShowSourcePicker(custom: false));
            AddButton(sourceRow, "Import S1API", () => ShowSourcePicker(custom: true));

            Section("Body");
            AddFloat("Gender", _draft.Appearance.Gender, value => _draft.Appearance.Gender = value);
            AddFloat("Height", _draft.Appearance.Height, value => _draft.Appearance.Height = value);
            AddFloat("Weight", _draft.Appearance.Weight, value => _draft.Appearance.Weight = value);
            AddColor("Skin color", _draft.Appearance.SkinColor, value => _draft.Appearance.SkinColor = value);

            Section("Eyes");
            AddColor(
                "Left eyelid color",
                _draft.Appearance.LeftEyeLidColor,
                value => _draft.Appearance.LeftEyeLidColor = value);
            AddColor(
                "Right eyelid color",
                _draft.Appearance.RightEyeLidColor,
                value => _draft.Appearance.RightEyeLidColor = value);
            AddColor("Eyeball tint", _draft.Appearance.EyeBallTint, value => _draft.Appearance.EyeBallTint = value);
            AddInput(
                "Eyeball material ID",
                _draft.Appearance.EyeballMaterialIdentifier,
                value => _draft.Appearance.EyeballMaterialIdentifier = value);
            AddFloat("Pupil dilation", _draft.Appearance.PupilDilation, value => _draft.Appearance.PupilDilation = value);
            AddEyeControls(
                "Left eye",
                () => _draft.Appearance.LeftEye,
                value => _draft.Appearance.LeftEye = value);
            AddEyeControls(
                "Right eye",
                () => _draft.Appearance.RightEye,
                value => _draft.Appearance.RightEye = value);

            Section("Eyebrows");
            AddFloat("Scale", _draft.Appearance.EyebrowScale, value => _draft.Appearance.EyebrowScale = value);
            AddFloat(
                "Thickness",
                _draft.Appearance.EyebrowThickness,
                value => _draft.Appearance.EyebrowThickness = value);
            AddFloat(
                "Resting height",
                _draft.Appearance.EyebrowRestingHeight,
                value => _draft.Appearance.EyebrowRestingHeight = value);
            AddFloat(
                "Resting angle",
                _draft.Appearance.EyebrowRestingAngle,
                value => _draft.Appearance.EyebrowRestingAngle = value);

            Section("Hair");
            AddPathSelection(
                "Hair style",
                NPCWorkbenchPathKind.Hair,
                _draft.Appearance.HairPath,
                value => _draft.Appearance.HairPath = value);
            AddInput(
                "Custom hair path",
                _draft.Appearance.HairPath,
                value => _draft.Appearance.HairPath = value);
            AddColor("Hair color", _draft.Appearance.HairColor, value => _draft.Appearance.HairColor = value);
            AddInput(
                "Impostor settings ID",
                _draft.Appearance.ImpostorId ?? string.Empty,
                value => _draft.Appearance.ImpostorId = string.IsNullOrWhiteSpace(value) ? null : value);

            AddLayerEditor("Face layers", NPCWorkbenchPathKind.FaceLayer, _draft.Appearance.FaceLayers);
            AddLayerEditor("Body layers", NPCWorkbenchPathKind.BodyLayer, _draft.Appearance.BodyLayers);
            AddLayerEditor("Accessories", NPCWorkbenchPathKind.Accessory, _draft.Appearance.Accessories);
        }

        private void ShowSourcePicker(bool custom)
        {
            var options = NPCWorkbenchRuntimeAdapter.GetSources(custom);
            if (options.Count == 0)
            {
                SetStatus(custom ? "No configured S1API NPCs are available." : "No native NPCs are available.");
                return;
            }

            ClearChildren(_editorContent);
            Section(custom ? "Import configured S1API NPC" : "Import native NPC");
            AddNotice("Import copies only current appearance settings into a detached draft. The source NPC is never mutated.");
            foreach (var option in options)
            {
                var row = Row(option.Id);
                AddButton(row, option.DisplayName, () =>
                {
                    try
                    {
                        _draft = NPCWorkbenchRuntimeAdapter.Import(option.Id);
                        BuildEditor();
                        Refresh();
                    }
                    catch (Exception ex)
                    {
                        SetStatus(ex.Message);
                    }
                });
            }
            var back = Row("Back");
            AddButton(back, "Back", BuildEditor);
        }

        private void AddColor(string label, NPCWorkbenchColor value, Action<NPCWorkbenchColor> changed)
        {
            AddInput($"{label} (RRGGBBAA)", value.ToHex(), text =>
            {
                if (NPCWorkbenchColor.TryParse(text, out var parsed))
                    changed(parsed);
            });
        }

        private void AddEyeControls(
            string label,
            Func<NPCWorkbenchEyeSettings> getValue,
            Action<NPCWorkbenchEyeSettings> changed)
        {
            AddFloat($"{label} top lid", getValue().TopLidOpen, top =>
            {
                var current = getValue();
                changed(new NPCWorkbenchEyeSettings(top, current.BottomLidOpen));
            });
            AddFloat($"{label} bottom lid", getValue().BottomLidOpen, bottom =>
            {
                var current = getValue();
                changed(new NPCWorkbenchEyeSettings(current.TopLidOpen, bottom));
            });
        }

        private void AddLayerEditor(
            string title,
            NPCWorkbenchPathKind kind,
            List<NPCWorkbenchLayer> layers)
        {
            Section(title);
            var addRow = Row($"{title} Add");
            AddButton(addRow, "Add layer", () =>
            {
                layers.Add(new NPCWorkbenchLayer());
                BuildEditor();
                Refresh();
            });

            for (var index = 0; index < layers.Count; index++)
            {
                var capturedIndex = index;
                var layer = layers[index];
                AddPathSelection(
                    $"{index + 1}. S1API path",
                    kind,
                    layer.Path,
                    value => layer.Path = value);
                AddInput($"{index + 1}. Custom path", layer.Path, value => layer.Path = value);
                AddColor($"{index + 1}. Tint", layer.Color, value => layer.Color = value);
                var actions = Row($"{title} {index + 1} Actions");
                AddButton(actions, "Up", () => MoveLayer(layers, capturedIndex, capturedIndex - 1));
                AddButton(actions, "Down", () => MoveLayer(layers, capturedIndex, capturedIndex + 1));
                AddButton(actions, "Remove", () =>
                {
                    layers.RemoveAt(capturedIndex);
                    BuildEditor();
                    Refresh();
                });
            }
        }

        private void AddPathSelection(
            string label,
            NPCWorkbenchPathKind kind,
            string path,
            Action<string> selected)
        {
            var entry = NPCWorkbenchPathCatalog.Find(kind, path);
            var row = Row(label);
            var caption = Label(
                label + " Label",
                row.transform,
                entry?.DisplayName ?? (string.IsNullOrWhiteSpace(path) ? "None selected" : "Custom path"),
                14,
                TextAnchor.MiddleLeft);
            caption.gameObject.AddComponent<LayoutElement>().preferredWidth = 205f;
            AddButton(row, "Choose", () => ShowPathPicker(kind, selected));
            if (kind == NPCWorkbenchPathKind.Hair && !string.IsNullOrEmpty(path))
            {
                AddButton(row, "Clear", () =>
                {
                    selected(string.Empty);
                    BuildEditor();
                    Refresh();
                });
            }
        }

        private void ShowPathPicker(NPCWorkbenchPathKind kind, Action<string> selected)
        {
            ClearChildren(_editorContent);
            Section(PathPickerTitle(kind));
            AddNotice(
                "Choose a typed S1API appearance path. The raw path field remains available for custom assets.");

            foreach (var group in NPCWorkbenchPathCatalog.Get(kind).GroupBy(entry => entry.GroupName))
            {
                Section(group.Key);
                foreach (var entry in group)
                {
                    var row = Row(entry.DisplayName);
                    AddButton(row, entry.MemberName, () =>
                    {
                        selected(entry.Path);
                        BuildEditor();
                        Refresh();
                    });
                }
            }

            var back = Row("Back");
            AddButton(back, "Back without changes", BuildEditor);
        }

        private static string PathPickerTitle(NPCWorkbenchPathKind kind)
        {
            switch (kind)
            {
                case NPCWorkbenchPathKind.Hair:
                    return "Choose hair style";
                case NPCWorkbenchPathKind.FaceLayer:
                    return "Choose face layer";
                case NPCWorkbenchPathKind.BodyLayer:
                    return "Choose body or clothing layer";
                case NPCWorkbenchPathKind.Accessory:
                    return "Choose accessory";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private void MoveLayer(List<NPCWorkbenchLayer> layers, int from, int to)
        {
            if (to < 0 || to >= layers.Count)
                return;
            var layer = layers[from];
            layers.RemoveAt(from);
            layers.Insert(to, layer);
            BuildEditor();
            Refresh();
        }

        private void Refresh()
        {
            if (_disposed)
                return;

            var result = NPCWorkbenchExporter.Export(_draft);
            _sourceText.text = _draft.SourceDisplayName;
            _codeText.text = result.CanExport ? result.Code : "Fix validation errors to generate C#.";
            if (result.Diagnostics.Count == 0)
            {
                _statusText.color = new Color(0.48f, 0.86f, 0.62f);
                _statusText.text = "Appearance is ready to export. Preview is local-only and detached.";
            }
            else
            {
                _statusText.color = result.CanExport
                    ? new Color(0.95f, 0.76f, 0.36f)
                    : new Color(1f, 0.42f, 0.42f);
                _statusText.text = string.Join(
                    "\n",
                    result.Diagnostics.Take(6).Select(diagnostic =>
                        $"{diagnostic.Severity}: {diagnostic.Message}"));
            }
            _preview?.ScheduleApply(_draft);
        }

        private void CopyExport()
        {
            var result = NPCWorkbenchExporter.Export(_draft);
            if (!result.CanExport)
            {
                Refresh();
                return;
            }

            GUIUtility.systemCopyBuffer = result.Code;
            SetStatus("Appearance C# copied to the clipboard.");
        }

        private void SetStatus(string message)
        {
            _statusText.color = new Color(0.75f, 0.82f, 0.9f);
            _statusText.text = message;
        }

        private void Section(string title)
        {
            var label = Label(title, _editorContent, title, 18, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.color = new Color(0.62f, 0.82f, 1f);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        }

        private GameObject Row(string name)
        {
            var row = Panel(name, _editorContent, Color.clear);
            AddHorizontalLayout(row, 8);
            row.AddComponent<LayoutElement>().preferredHeight = 42f;
            return row;
        }

        private void AddInput(string label, string value, Action<string> changed)
        {
            var row = Row(label);
            var caption = Label(label + " Label", row.transform, label, 14, TextAnchor.MiddleLeft);
            caption.gameObject.AddComponent<LayoutElement>().preferredWidth = 205f;
            var input = InputField(label + " Input", row.transform, value);
            input.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            input.onEndEdit.AddListener((UnityAction<string>)(text =>
            {
                changed(text);
                Refresh();
            }));
        }

        private void AddFloat(string label, float value, Action<float> changed) =>
            AddInput(label, value.ToString("0.####", CultureInfo.InvariantCulture), text =>
            {
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    changed(parsed);
            });

        private void AddNotice(string text)
        {
            var notice = Label("Notice", _editorContent, text, 13, TextAnchor.UpperLeft);
            notice.color = new Color(0.65f, 0.7f, 0.76f);
            notice.horizontalOverflow = HorizontalWrapMode.Wrap;
            notice.verticalOverflow = VerticalWrapMode.Overflow;
            notice.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
        }

        private static GameObject AddButton(GameObject row, string text, Action clicked)
        {
            var button = Button(text, row.transform, text, RaisedColor);
            ConfigureFlexibleButton(button);
            button.GetComponent<Button>().onClick.AddListener((UnityAction)(() => clicked()));
            return button;
        }

        private static void ConfigureFlexibleButton(GameObject button)
        {
            var layout = button.AddComponent<LayoutElement>();
            layout.minWidth = 72f;
            layout.preferredWidth = 108f;
            layout.flexibleWidth = 1f;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                _preview?.Dispose();

                if (_inputCaptured &&
                    S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.InstanceExists)
                {
                    var camera = S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.Instance;
                    camera.RemoveActiveUIElement("S1API NPC Workbench");
                    if (_previousCanLook.HasValue)
                        camera.SetCanLook(_previousCanLook.Value);
                }
                if (_inputCaptured &&
                    S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.InstanceExists &&
                    _previousCanMove.HasValue)
                {
                    S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.Instance.CanMove =
                        _previousCanMove.Value;
                }

                if (_inputCaptured)
                {
                    Cursor.lockState = _previousCursorLock;
                    Cursor.visible = _previousCursorVisible;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error restoring game state: {ex}");
            }
            finally
            {
                if (_root != null)
                    Object.Destroy(_root);
                NPCWorkbenchRuntime.NotifyClosed(this);
            }
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text Label(
            string name,
            Transform parent,
            string text,
            int size,
            TextAnchor alignment,
            FontStyle style = FontStyle.Normal)
        {
            var label = new GameObject(name);
            label.transform.SetParent(parent, false);
            label.AddComponent<RectTransform>();
            var component = label.AddComponent<Text>();
            component.font = BuiltinFont();
            component.fontSize = size;
            component.fontStyle = style;
            component.alignment = alignment;
            component.color = Color.white;
            component.text = text;
            return component;
        }

        private static GameObject Button(string name, Transform parent, string text, Color color)
        {
            var button = Panel(name, parent, color);
            var component = button.AddComponent<Button>();
            component.targetGraphic = button.GetComponent<Image>();
            var label = Label("Label", button.transform, text, 14, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        private static InputField InputField(string name, Transform parent, string value)
        {
            var root = Panel(name, parent, new Color(0.035f, 0.045f, 0.06f, 1f));
            var input = root.AddComponent<InputField>();
            input.targetGraphic = root.GetComponent<Image>();
            var text = Label("Text", root.transform, value, 14, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            Anchor(text.rectTransform, 0f, 0f, 1f, 1f, 10f, 2f, -10f, -2f);
            input.textComponent = text;
            input.text = value;
            return input;
        }

        private static GameObject Scroll(string name, Transform parent, out RectTransform content)
        {
            var root = Panel(name, parent, new Color(0.025f, 0.032f, 0.043f, 1f));
            Stretch(root.GetComponent<RectTransform>());
            var viewport = Panel("Viewport", root.transform, Color.clear);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<RectMask2D>();
            var contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport.transform, false);
            content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(12f, 0f);
            content.offsetMax = new Vector2(-12f, 0f);
            var layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.padding = new RectOffset(4, 4, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return root;
        }

        private static void AddHorizontalLayout(GameObject root, float spacing)
        {
            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
        }

        private static Font BuiltinFont() => Resources.GetBuiltinResource<Font>("Arial.ttf");

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
                Object.Destroy(parent.GetChild(index).gameObject);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float left,
            float bottom,
            float right,
            float top)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }
    }
}
