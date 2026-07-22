#if (IL2CPPMELON)
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Map = Il2CppScheduleOne.Map;
using S1MapUI = Il2CppScheduleOne.UI.Phone.Map;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#else
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Map = ScheduleOne.Map;
using S1MapUI = ScheduleOne.UI.Phone.Map;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

#if MONOMELON
using System.Reflection;
using HarmonyLib;
#endif

using System;
using System.Collections;
using MelonLoader;
using S1API.Logging;
using S1API.Map;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace S1API.Internal.Map
{
    internal sealed class MapPOIRuntime
    {
        private const string CustomIconName = "S1API.CustomIcon";
        private static readonly Log Logger = new Log("S1API.MapPOI");

        private readonly MapPOI _owner;
        private object? _initializationCoroutine;
        private GameObject? _root;
        private S1Map.POI? _nativePOI;
        private bool _disposed;

        internal bool IsReady => !_disposed && _root != null && _nativePOI != null;
        internal bool IsUIReady => IsReady && _nativePOI!.UI != null && _nativePOI.UISetup;
        internal bool CanContinue => !_disposed && (_initializationCoroutine != null || _root != null);
        internal RectTransform? UI => IsUIReady ? _nativePOI!.UI : null;

        internal MapPOIRuntime(MapPOI owner)
        {
            _owner = owner;
        }

        internal void Start()
        {
            if (_disposed || _initializationCoroutine != null || _root != null)
            {
                return;
            }

            _initializationCoroutine = MelonCoroutines.Start(InitializeWhenReady());
        }

        internal void ApplyLabel()
        {
            if (_nativePOI != null)
            {
                _nativePOI.SetMainText(_owner.Label);
            }
        }

        internal void ApplyIcon()
        {
            if (IsUIReady)
            {
                ApplyIconToCurrentUI();
            }
        }

        internal void ApplyLocation()
        {
            if (_root == null)
            {
                return;
            }

            Transform? target = _owner.Target;
            if (target != null)
            {
                _root.transform.SetParent(target, false);
                _root.transform.localPosition = Vector3.zero;
                _root.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _root.transform.SetParent(null, true);
                _root.transform.position = _owner.Position;
                _root.transform.rotation = Quaternion.identity;
            }

            UpdatePosition();
        }

        internal void ApplyTextVisibility()
        {
            if (_nativePOI == null)
            {
                return;
            }

            _nativePOI.MainTextVisibility = (S1Map.POI.TextShowMode)(int)_owner.TextVisibility;
            if (_nativePOI.UI != null)
            {
                Transform labelTransform = _nativePOI.UI.Find("MainLabel");
                Text? label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
                if (label != null)
                {
                    label.enabled = _owner.TextVisibility == MapPOITextVisibility.Always;
                }
            }
        }

        internal void ApplyRotation()
        {
            if (_nativePOI == null)
            {
                return;
            }

            _nativePOI.Rotate = _owner.RotateWithTarget;
            UpdatePosition();
        }

        internal void ApplyVisibility()
        {
            if (_root == null)
            {
                return;
            }

            _root.SetActive(_owner.IsVisible);
            if (_owner.IsVisible)
            {
                UpdatePosition();
            }
        }

        internal bool Focus(bool openMap)
        {
            if (!IsUIReady || !S1DevUtilities.PlayerSingleton<S1MapUI.MapApp>.InstanceExists)
            {
                return false;
            }

            S1MapUI.MapApp mapApp = S1DevUtilities.PlayerSingleton<S1MapUI.MapApp>.Instance;
            UpdatePosition();

            if (openMap)
            {
                bool previousSkipFocus = mapApp.SkipFocusPlayer;
                mapApp.SkipFocusPlayer = true;
                mapApp.SetOpen(true);
                mapApp.SkipFocusPlayer = previousSkipFocus;
            }

            mapApp.FocusPosition(_nativePOI!.UI.anchoredPosition);
            return true;
        }

        internal void Dispose(bool destroyRuntimeObject)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_initializationCoroutine != null)
            {
                try
                {
                    MelonCoroutines.Stop(_initializationCoroutine);
                }
                catch
                {
                    // The coroutine may already have completed between frames.
                }

                _initializationCoroutine = null;
            }

            if (destroyRuntimeObject && _root != null)
            {
                Object.Destroy(_root);
            }

            _nativePOI = null;
            _root = null;
        }

        private IEnumerator InitializeWhenReady()
        {
            while (!_disposed)
            {
                if (TryGetNativeUIPrefab(out GameObject? uiPrefab))
                {
                    try
                    {
                        CreateNativeMarker(uiPrefab!);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to create map POI '{_owner.Id}': {ex}");
                    }

                    _initializationCoroutine = null;
                    yield break;
                }

                yield return null;
            }

            _initializationCoroutine = null;
        }

        private void CreateNativeMarker(GameObject uiPrefab)
        {
            _root = new GameObject($"S1API.MapPOI.{_owner.Id}");
            _root.SetActive(false);
            ApplyLocation();

            _nativePOI = _root.AddComponent<S1Map.POI>();
            _nativePOI.onUICreated = new UnityEvent();
#if IL2CPPMELON
            _nativePOI.onUICreated.AddListener((UnityAction)ApplyUIState);
#else
            _nativePOI.onUICreated.AddListener(new UnityAction(ApplyUIState));
#endif
            _nativePOI.AutoUpdatePosition = true;
            _nativePOI.Rotate = _owner.RotateWithTarget;
            _nativePOI.MainTextVisibility = (S1Map.POI.TextShowMode)(int)_owner.TextVisibility;
            _nativePOI.DefaultMainText = _owner.Label;
            SetNativeUIPrefab(_nativePOI, uiPrefab);
            _nativePOI.SetMainText(_owner.Label);

            _root.SetActive(_owner.IsVisible);
            if (_owner.IsVisible)
            {
                UpdatePosition();
            }
        }

        private void ApplyUIState()
        {
            ApplyLabel();
            ApplyTextVisibility();
            ApplyIconToCurrentUI();
            UpdatePosition();
        }

        private void ApplyIconToCurrentUI()
        {
            if (_nativePOI?.IconContainer == null)
            {
                return;
            }

            RectTransform iconContainer = _nativePOI.IconContainer;
            Transform? customIconTransform = iconContainer.Find(CustomIconName);

            for (int i = 0; i < iconContainer.childCount; i++)
            {
                Transform child = iconContainer.GetChild(i);
                if (child.name != CustomIconName)
                {
                    child.gameObject.SetActive(_owner.Icon == null);
                }
            }

            if (_owner.Icon == null)
            {
                if (customIconTransform != null)
                {
                    customIconTransform.gameObject.SetActive(false);
                    Object.Destroy(customIconTransform.gameObject);
                }

                return;
            }

            Image iconImage;
            if (customIconTransform == null)
            {
                var iconObject = new GameObject(CustomIconName);
                RectTransform rect = iconObject.AddComponent<RectTransform>();
                rect.SetParent(iconContainer, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(26f, 26f);
                iconImage = iconObject.AddComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            else
            {
                customIconTransform.gameObject.SetActive(true);
                iconImage = customIconTransform.GetComponent<Image>();
            }

            iconImage.sprite = _owner.Icon;
            iconImage.color = Color.white;
        }

        private void UpdatePosition()
        {
            if (_nativePOI != null && _nativePOI.UI != null)
            {
                _nativePOI.UpdatePosition();
            }
        }

        private static bool TryGetNativeUIPrefab(out GameObject? uiPrefab)
        {
            uiPrefab = null;

            try
            {
                S1PlayerScripts.Player player = S1PlayerScripts.Player.Local;
                if (player == null || player.PoI == null)
                {
                    return false;
                }

#if MONOMELON
                FieldInfo? field = AccessTools.Field(typeof(S1Map.POI), "UIPrefab");
                uiPrefab = field?.GetValue(player.PoI) as GameObject;
#else
                uiPrefab = player.PoI.UIPrefab;
#endif
                return uiPrefab != null;
            }
            catch
            {
                return false;
            }
        }

        private static void SetNativeUIPrefab(S1Map.POI poi, GameObject uiPrefab)
        {
#if MONOMELON
            FieldInfo? field = AccessTools.Field(typeof(S1Map.POI), "UIPrefab");
            if (field == null)
            {
                throw new MissingFieldException(typeof(S1Map.POI).FullName, "UIPrefab");
            }

            field.SetValue(poi, uiPrefab);
#else
            poi.UIPrefab = uiPrefab;
#endif
        }
    }
}
