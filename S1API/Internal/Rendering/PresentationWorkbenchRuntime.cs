#if IL2CPPMELON
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Logging;
using S1API.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Rendering
{
    internal static class PresentationWorkbenchRuntime
    {
        private const string ActiveUiName = "S1API.PresentationWorkbench";
        private const float IconDebounceSeconds = 0.12f;
        private static readonly Log Logger = new Log("PresentationWorkbench");
        private static Session? _session;

        internal static bool IsOpen => _session != null;

        internal static string? ActiveDefinitionId => _session?.Definition.Id;

        internal static bool Open(string id, string? targetKind = null)
        {
            Close();
            PresentationWorkbenchDefinition? definition = null;
            try
            {
                if (!PresentationWorkbenchResolver.TryResolve(
                        id,
                        targetKind,
                        out definition,
                        out string failure) ||
                    definition == null)
                {
                    Logger.Warning(failure);
                    return false;
                }

                if (!TryGetGameplayState(
                        out S1PlayerScripts.Player? player,
                        out S1PlayerScripts.PlayerInventory? inventory,
                        out S1PlayerScripts.PlayerCamera? playerCamera,
                        out S1PlayerScripts.PlayerMovement? movement))
                {
                    Logger.Warning(
                        "The presentation workbench requires a spawned local player in " +
                        "a render-ready Main or Tutorial scene.");
                    return false;
                }

                var session = new Session(
                    definition,
                    player!,
                    inventory!,
                    playerCamera!,
                    movement!);
                _session = session;
                session.Open();
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Could not open presentation workbench " +
                    $"'{definition?.Id ?? id}': " +
                    exception);
                Close();
                return false;
            }
        }

        internal static void Tick()
        {
            try
            {
                _session?.Tick();
            }
            catch (Exception exception)
            {
                Logger.Error($"Presentation workbench update failed: {exception}");
                Close();
            }
        }

        internal static void Close()
        {
            Session? session = _session;
            _session = null;
            if (session == null)
                return;

            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Presentation workbench cleanup failed: {exception}");
            }
        }

        private static bool TryGetGameplayState(
            out S1PlayerScripts.Player? player,
            out S1PlayerScripts.PlayerInventory? inventory,
            out S1PlayerScripts.PlayerCamera? playerCamera,
            out S1PlayerScripts.PlayerMovement? movement)
        {
            player = S1PlayerScripts.Player.Local;
            inventory =
                S1DevUtilities.PlayerSingleton<
                    S1PlayerScripts.PlayerInventory>.InstanceExists
                    ? S1DevUtilities.PlayerSingleton<
                        S1PlayerScripts.PlayerInventory>.Instance
                    : null;
            playerCamera =
                S1DevUtilities.PlayerSingleton<
                    S1PlayerScripts.PlayerCamera>.InstanceExists
                    ? S1DevUtilities.PlayerSingleton<
                        S1PlayerScripts.PlayerCamera>.Instance
                    : null;
            movement =
                S1DevUtilities.PlayerSingleton<
                    S1PlayerScripts.PlayerMovement>.InstanceExists
                    ? S1DevUtilities.PlayerSingleton<
                        S1PlayerScripts.PlayerMovement>.Instance
                    : null;
            return player != null &&
                   inventory != null &&
                   playerCamera != null &&
                   movement != null &&
                   inventory.EquipContainer != null;
        }

        private sealed class Session : IDisposable
        {
            private readonly S1PlayerScripts.Player _player;
            private readonly S1PlayerScripts.PlayerInventory _inventory;
            private readonly S1PlayerScripts.PlayerCamera _playerCamera;
            private readonly S1PlayerScripts.PlayerMovement _movement;
            private readonly bool _previousCanLook;
            private readonly bool _previousCanMove;
            private readonly bool _previousAvatarVisible;
            private readonly bool _previousHotbarEnabled;
            private readonly bool _previousEquippingEnabled;
            private readonly CursorLockMode _previousCursorLockMode;
            private readonly bool _previousCursorVisible;
            private readonly GameObject? _previousEquippable;
            private readonly bool _previousEquippableActive;
            private readonly PreviewState? _firstPerson;
            private readonly PreviewState? _avatar;
            private readonly IconState? _icon;
            private readonly PresentationWorkbenchView _view;
            private PresentationWorkbenchMode _mode;
            private GameObject? _previewVisual;
            private GameObject? _avatarAnchor;
            private Camera? _avatarCamera;
            private RenderTexture? _avatarTexture;
            private Texture2D? _iconTexture;
            private float _iconRefreshAt = -1f;
            private bool _disposed;

            internal Session(
                PresentationWorkbenchDefinition definition,
                S1PlayerScripts.Player player,
                S1PlayerScripts.PlayerInventory inventory,
                S1PlayerScripts.PlayerCamera playerCamera,
                S1PlayerScripts.PlayerMovement movement)
            {
                Definition = definition;
                _player = player;
                _inventory = inventory;
                _playerCamera = playerCamera;
                _movement = movement;
                _previousCanLook = playerCamera.CanLook;
                _previousCanMove = movement.CanMove;
                _previousHotbarEnabled = inventory.HotbarEnabled;
                _previousEquippingEnabled = inventory.EquippingEnabled;
                _previousCursorLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _previousAvatarVisible =
                    ReflectionUtils.TryGetFieldOrProperty(
                        player,
                        "avatarVisibleToLocalPlayer") as bool? ?? false;
                _previousEquippable = inventory.Equippable?.gameObject;
                _previousEquippableActive =
                    _previousEquippable != null &&
                    _previousEquippable.activeSelf;

                _firstPerson = CreateState(definition.FirstPerson);
                _avatar = CreateState(definition.Avatar);
                _icon = CreateIconState(definition.Icon);
                _mode = GetFirstMode(definition);
                _view = new PresentationWorkbenchView(
                    definition,
                    SelectMode,
                    UpdateTransform,
                    ToggleFit,
                    UpdateCameraFill,
                    Reset,
                    Copy,
                    PresentationWorkbenchRuntime.Close);
            }

            internal PresentationWorkbenchDefinition Definition { get; }

            internal void Open()
            {
                _playerCamera.AddActiveUIElement(ActiveUiName);
                _playerCamera.SetCanLook(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _movement.CanMove = false;
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "HotbarEnabled",
                    false);
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "EquippingEnabled",
                    false);
                _previousEquippable?.SetActive(false);
                SelectMode(_mode);
            }

            internal void Tick()
            {
                if (_mode == PresentationWorkbenchMode.Icon &&
                    _iconRefreshAt >= 0f &&
                    Time.unscaledTime >= _iconRefreshAt)
                {
                    _iconRefreshAt = -1f;
                    CaptureIcon();
                }

                if (_mode == PresentationWorkbenchMode.Avatar &&
                    _avatarCamera != null)
                {
                    PositionAvatarCamera();
                }
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                DestroyPreview();
                DestroyIconTexture();
                _view.Dispose();
                if (_previousEquippable != null)
                    _previousEquippable.SetActive(_previousEquippableActive);
                _player.SetVisibleToLocalPlayer(_previousAvatarVisible);
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "HotbarEnabled",
                    _previousHotbarEnabled);
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "EquippingEnabled",
                    _previousEquippingEnabled);
                _movement.CanMove = _previousCanMove;
                _playerCamera.RemoveActiveUIElement(ActiveUiName);
                _playerCamera.SetCanLook(_previousCanLook);
                Cursor.lockState = _previousCursorLockMode;
                Cursor.visible = _previousCursorVisible;
            }

            private void SelectMode(PresentationWorkbenchMode mode)
            {
                if (!Supports(mode))
                    return;

                DestroyPreview();
                _mode = mode;
                switch (mode)
                {
                    case PresentationWorkbenchMode.FirstPerson:
                        CreateFirstPersonPreview();
                        break;
                    case PresentationWorkbenchMode.Avatar:
                        CreateAvatarPreview();
                        break;
                    case PresentationWorkbenchMode.Icon:
                        ScheduleIconCapture(immediate: true);
                        break;
                }

                PreviewState state = GetState(mode);
                _view.SetMode(
                    mode,
                    state.Current,
                    _icon?.FitToCamera ?? false,
                    _icon?.CameraFill ?? 0.72f);
            }

            private void CreateFirstPersonPreview()
            {
                PreviewState state = _firstPerson!;
                _previewVisual = CloneSource(state.Provider, "FirstPerson");
                _previewVisual.transform.SetParent(
                    _inventory.EquipContainer,
                    false);
                state.Current.ApplyTo(_previewVisual.transform);
                SetLayerRecursively(_previewVisual, "Viewmodel");
                DisablePhysics(_previewVisual);
                _previewVisual.SetActive(true);
                _view.SetPreview(null);
            }

            private void CreateAvatarPreview()
            {
                PreviewState state = _avatar!;
                Transform handContainer;
                Transform alignmentPoint;
                ResolveAvatarHand(
                    Definition.Avatar!.Hand,
                    out handContainer,
                    out alignmentPoint);

                _player.SetVisibleToLocalPlayer(true);
                _avatarAnchor = new GameObject("S1API Avatar Preview Anchor");
                _avatarAnchor.transform.SetParent(handContainer, false);
                _avatarAnchor.transform.SetPositionAndRotation(
                    alignmentPoint.position,
                    alignmentPoint.rotation);

                _previewVisual = CloneSource(state.Provider, "Avatar");
                _previewVisual.transform.SetParent(
                    _avatarAnchor.transform,
                    false);
                state.Current.ApplyTo(_previewVisual.transform);
                SetLayerRecursively(_previewVisual, "Player");
                DisablePhysics(_previewVisual);
                _previewVisual.SetActive(true);
                CreateAvatarCamera();
            }

            private void CreateAvatarCamera()
            {
                var cameraRoot =
                    new GameObject("S1API Presentation Workbench Camera");
                _avatarCamera = cameraRoot.AddComponent<Camera>();
                _avatarCamera.enabled = true;
                _avatarCamera.clearFlags = CameraClearFlags.SolidColor;
                _avatarCamera.backgroundColor =
                    new Color(0.025f, 0.03f, 0.04f, 1f);
                _avatarCamera.fieldOfView = 38f;
                _avatarCamera.nearClipPlane = 0.05f;
                _avatarCamera.farClipPlane = 20f;
                int playerLayer = LayerMask.NameToLayer("Player");
                _avatarCamera.cullingMask =
                    playerLayer >= 0 ? 1 << playerLayer : -1;
                _avatarTexture =
                    new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32)
                    {
                        name = "S1API Presentation Workbench Avatar",
                    };
                _avatarTexture.Create();
                _avatarCamera.targetTexture = _avatarTexture;
                PositionAvatarCamera();
                _view.SetPreview(_avatarTexture);
            }

            private void PositionAvatarCamera()
            {
                if (_avatarCamera == null)
                    return;

                Transform player = _player.transform;
                Vector3 target = player.position + Vector3.up * 1.05f;
                Vector3 position =
                    target + player.forward * 2.15f + Vector3.up * 0.1f;
                _avatarCamera.transform.position = position;
                _avatarCamera.transform.rotation =
                    Quaternion.LookRotation(target - position, Vector3.up);
            }

            private void ResolveAvatarHand(
                AvatarHand hand,
                out Transform handContainer,
                out Transform alignmentPoint)
            {
                object avatar =
                    ReflectionUtils.TryGetFieldOrProperty(_player, "Avatar") ??
                    throw new InvalidOperationException(
                        "The local player avatar is unavailable.");
                object animation =
                    ReflectionUtils.TryGetFieldOrProperty(avatar, "Animation") ??
                    throw new InvalidOperationException(
                        "The local player avatar animation rig is unavailable.");
                string handName =
                    hand == AvatarHand.Left ? "LeftHand" : "RightHand";
                handContainer =
                    ReflectionUtils.TryGetFieldOrProperty(
                        animation,
                        handName + "Container") as Transform ??
                    throw new InvalidOperationException(
                        $"{handName}Container is unavailable.");
                alignmentPoint =
                    ReflectionUtils.TryGetFieldOrProperty(
                        animation,
                        handName + "AlignmentPoint") as Transform ??
                    throw new InvalidOperationException(
                        $"{handName}AlignmentPoint is unavailable.");
            }

            private void UpdateTransform(
                Vector3 position,
                Vector3 rotation,
                Vector3 scale)
            {
                if (!IsFinite(position) ||
                    !IsFinite(rotation) ||
                    !IsFinite(scale))
                {
                    _view.SetStatus("Transform values must be finite.");
                    return;
                }

                var value =
                    new PresentationWorkbenchTransform(
                        position,
                        rotation,
                        scale);
                PreviewState state = GetState(_mode);
                state.Current = value;
                if (_previewVisual != null)
                    value.ApplyTo(_previewVisual.transform);
                if (_mode == PresentationWorkbenchMode.Icon)
                    ScheduleIconCapture(immediate: false);
                _view.SetStatus("Preview updated.");
            }

            private void ToggleFit()
            {
                if (_mode != PresentationWorkbenchMode.Icon || _icon == null)
                    return;

                _icon.FitToCamera = !_icon.FitToCamera;
                _view.SetMode(
                    _mode,
                    _icon.Current,
                    _icon.FitToCamera,
                    _icon.CameraFill);
                ScheduleIconCapture(immediate: false);
            }

            private void UpdateCameraFill(float value)
            {
                if (_mode != PresentationWorkbenchMode.Icon || _icon == null)
                    return;

                if (value <= 0f || value > 2f)
                {
                    _view.SetStatus(
                        "Camera fill must be greater than zero and at most 2.");
                    return;
                }

                _icon.CameraFill = value;
                ScheduleIconCapture(immediate: false);
                _view.SetStatus("Icon recapture scheduled.");
            }

            private void Reset()
            {
                PreviewState state = GetState(_mode);
                state.Current = state.Initial;
                if (_icon != null &&
                    _mode == PresentationWorkbenchMode.Icon)
                {
                    _icon.FitToCamera = _icon.InitialFitToCamera;
                    _icon.CameraFill = _icon.InitialCameraFill;
                    ScheduleIconCapture(immediate: false);
                }
                else if (_previewVisual != null)
                {
                    state.Current.ApplyTo(_previewVisual.transform);
                }

                _view.SetMode(
                    _mode,
                    state.Current,
                    _icon?.FitToCamera ?? false,
                    _icon?.CameraFill ?? 0.72f);
                _view.SetStatus("Initial values restored.");
            }

            private void Copy()
            {
                PreviewState state = GetState(_mode);
                string text =
                    _mode == PresentationWorkbenchMode.Icon && _icon != null
                        ? PresentationWorkbenchExporter.FormatIcon(
                            state.Current.LocalEulerAngles,
                            state.Current.LocalScale,
                            _icon.FitToCamera,
                            _icon.CameraFill,
                            _icon.Size)
                        : PresentationWorkbenchExporter.FormatTransform(
                            state.ExportKind,
                            state.Current.LocalPosition,
                            state.Current.LocalEulerAngles,
                            state.Current.LocalScale);
                GUIUtility.systemCopyBuffer = text;
                _view.SetStatus("Copied C# values to the clipboard.");
            }

            private void ScheduleIconCapture(bool immediate)
            {
                if (immediate)
                {
                    _iconRefreshAt = -1f;
                    CaptureIcon();
                }
                else
                {
                    _iconRefreshAt =
                        Time.unscaledTime + IconDebounceSeconds;
                    _view.SetStatus("Icon recapture scheduled.");
                }
            }

            private void CaptureIcon()
            {
                IconState state = _icon!;
                GameObject visual = CloneSource(state.Provider, "Icon");
                try
                {
                    state.Current.ApplyTo(visual.transform);
                    Texture2D? texture =
                        IconFactory.GenerateIcon(
                            visual.transform,
                            state.Size,
                            bakeSkinnedMeshes: true,
                            state.FitToCamera,
                            state.CameraFill);
                    if (texture == null)
                    {
                        _view.SetStatus(
                            "Icon capture is not ready. Try again after the scene renders.");
                        return;
                    }

                    DestroyIconTexture();
                    _iconTexture = texture;
                    _view.SetPreview(_iconTexture);
                    _view.SetStatus("Icon preview captured with the native rig.");
                }
                finally
                {
                    Object.Destroy(visual);
                }
            }

            private void DestroyPreview()
            {
                if (_previewVisual != null)
                    Object.Destroy(_previewVisual);
                if (_avatarAnchor != null)
                    Object.Destroy(_avatarAnchor);
                if (_avatarCamera != null)
                {
                    _avatarCamera.targetTexture = null;
                    Object.Destroy(_avatarCamera.gameObject);
                }
                if (_avatarTexture != null)
                {
                    _avatarTexture.Release();
                    Object.Destroy(_avatarTexture);
                }

                _previewVisual = null;
                _avatarAnchor = null;
                _avatarCamera = null;
                _avatarTexture = null;
                _view.SetPreview(null);
            }

            private void DestroyIconTexture()
            {
                if (_iconTexture != null)
                    Object.Destroy(_iconTexture);
                _iconTexture = null;
            }

            private bool Supports(PresentationWorkbenchMode mode) =>
                mode == PresentationWorkbenchMode.FirstPerson
                    ? _firstPerson != null
                    : mode == PresentationWorkbenchMode.Avatar
                        ? _avatar != null
                        : _icon != null;

            private PreviewState GetState(PresentationWorkbenchMode mode) =>
                mode == PresentationWorkbenchMode.FirstPerson
                    ? _firstPerson!
                    : mode == PresentationWorkbenchMode.Avatar
                        ? _avatar!
                        : _icon!;

            private static PreviewState? CreateState(
                PresentationWorkbenchDefinition.PreviewContext? context)
            {
                if (context == null)
                    return null;

                return new PreviewState(
                    context.Provider,
                    context.InitialTransform ??
                    CaptureAuthoredTransform(context.Provider),
                    context.ExportKind);
            }

            private static PreviewState? CreateState(
                PresentationWorkbenchDefinition.AvatarPreviewContext? context)
            {
                if (context == null)
                    return null;

                return new PreviewState(
                    context.Provider,
                    context.InitialTransform ??
                    CaptureAuthoredTransform(context.Provider),
                    context.ExportKind);
            }

            private static IconState? CreateIconState(
                PresentationWorkbenchDefinition.IconPreviewContext? context)
            {
                if (context == null)
                    return null;

                return new IconState(
                    context.Provider,
                    context.InitialTransform ??
                    CaptureAuthoredTransform(context.Provider),
                    context.Size,
                    context.FitToCamera,
                    context.CameraFill);
            }

            private static PresentationWorkbenchTransform
                CaptureAuthoredTransform(Func<GameObject?> provider)
            {
                GameObject source = GetSource(provider, "initial");
                return PresentationWorkbenchTransform.From(source.transform);
            }

            private static GameObject CloneSource(
                Func<GameObject?> provider,
                string context)
            {
                GameObject source = GetSource(provider, context);
                GameObject clone = Object.Instantiate(source);
                clone.SetActive(false);
                DisableBehaviours(clone);
                clone.name = $"S1API {context} Preview ({source.name})";
                return clone;
            }

            private static GameObject GetSource(
                Func<GameObject?> provider,
                string context)
            {
                GameObject? source;
                try
                {
                    source = provider();
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"{context} preview source provider threw an exception.",
                        exception);
                }

                return source ??
                       throw new InvalidOperationException(
                           $"{context} preview source provider returned null.");
            }

            private static PresentationWorkbenchMode GetFirstMode(
                PresentationWorkbenchDefinition definition)
            {
                if (definition.SupportsFirstPerson)
                    return PresentationWorkbenchMode.FirstPerson;
                if (definition.SupportsAvatar)
                    return PresentationWorkbenchMode.Avatar;
                return PresentationWorkbenchMode.Icon;
            }

            private static void SetLayerRecursively(
                GameObject root,
                string layerName)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer >= 0)
                {
                    S1DevUtilities.LayerUtility.SetLayerRecursively(root, layer);
                }
            }

            private static void DisablePhysics(GameObject root)
            {
                var colliders =
                    root.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }

                var rigidbodies =
                    root.GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    Rigidbody rigidbody = rigidbodies[i];
                    rigidbody.isKinematic = true;
                    rigidbody.detectCollisions = false;
                }
            }

            private static void DisableBehaviours(GameObject root)
            {
                var behaviours =
                    root.GetComponentsInChildren<Behaviour>(true);
                for (int index = 0; index < behaviours.Length; index++)
                    behaviours[index].enabled = false;
            }

            private static bool IsFinite(Vector3 value) =>
                IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

            private static bool IsFinite(float value) =>
                !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private class PreviewState
        {
            internal PreviewState(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform initial,
                PresentationWorkbenchExportKind exportKind)
            {
                Provider = provider;
                Initial = initial;
                Current = initial;
                ExportKind = exportKind;
            }

            internal Func<GameObject?> Provider { get; }

            internal PresentationWorkbenchTransform Initial { get; }

            internal PresentationWorkbenchTransform Current { get; set; }

            internal PresentationWorkbenchExportKind ExportKind { get; }
        }

        private sealed class IconState : PreviewState
        {
            internal IconState(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform initial,
                int size,
                bool fitToCamera,
                float cameraFill)
                : base(
                    provider,
                    initial,
                    PresentationWorkbenchExportKind.IconFactory)
            {
                Size = size;
                InitialFitToCamera = fitToCamera;
                FitToCamera = fitToCamera;
                InitialCameraFill = cameraFill;
                CameraFill = cameraFill;
            }

            internal int Size { get; }

            internal bool InitialFitToCamera { get; }

            internal bool FitToCamera { get; set; }

            internal float InitialCameraFill { get; }

            internal float CameraFill { get; set; }
        }
    }
}
