#if IL2CPPMELON
using S1BlackOverlay = Il2CppScheduleOne.UI.BlackOverlay;
using S1GameInput = Il2CppScheduleOne.GameInput;
using S1Singleton = Il2CppScheduleOne.DevUtilities.Singleton<Il2CppScheduleOne.UI.BlackOverlay>;
#elif MONOMELON
using S1BlackOverlay = ScheduleOne.UI.BlackOverlay;
using S1GameInput = ScheduleOne.GameInput;
using S1Singleton = ScheduleOne.DevUtilities.Singleton<ScheduleOne.UI.BlackOverlay>;
#endif

using System;
using S1API.Cutscenes;
using UnityEngine;

namespace S1API.Internal.Cutscenes
{
    internal sealed class CutscenePresentationRuntime
    {
        private readonly CutsceneConfiguration _configuration;
        private float _holdElapsed;
        private float _titleAlpha;
        private bool _fadeInStarted;
        private bool _fadeOutStarted;
        private bool _ownsBlackOverlay;
        private GUIStyle? _titleStyle;
        private GUIStyle? _skipStyle;

        internal CutscenePresentationRuntime(CutsceneConfiguration configuration)
        {
            _configuration = configuration;
        }

        internal void Begin()
        {
            if (_configuration.FadeInDuration > 0f && TryOpenBlackOverlay(0f))
            {
                if (_configuration.FadeInDelay <= 0f)
                {
                    _fadeInStarted = true;
                    CloseOwnedBlackOverlay(_configuration.FadeInDuration);
                }
            }
        }

        internal bool Tick(float deltaTime, float elapsed)
        {
            UpdateFadeIn(elapsed);
            UpdateTitle(elapsed);
            UpdateFadeOut(elapsed);

            if (_configuration.HoldToSkipDuration <= 0f)
            {
                return false;
            }

            if (!IsSkipHeld())
            {
                _holdElapsed = 0f;
                return false;
            }

            _holdElapsed = Math.Min(
                _configuration.HoldToSkipDuration,
                _holdElapsed + Math.Max(0f, deltaTime));
            return _holdElapsed >= _configuration.HoldToSkipDuration;
        }

        internal void Draw()
        {
            if (_titleAlpha <= 0f && _configuration.HoldToSkipDuration <= 0f)
            {
                return;
            }

            EnsureStyles();
            Color previousColor = GUI.color;
            float width = Screen.width;
            float height = Screen.height;

            if (_titleAlpha > 0f && _configuration.TitleCardText != null)
            {
                GUI.color = new Color(1f, 1f, 1f, _titleAlpha);
                GUI.Label(
                    new Rect(0f, height * 0.44f, width, 64f),
                    _configuration.TitleCardText,
                    _titleStyle);
            }

            if (_configuration.HoldToSkipDuration > 0f)
            {
                float progress = Math.Min(1f, _holdElapsed / _configuration.HoldToSkipDuration);
                var promptRect = new Rect(width - 310f, height - 82f, 270f, 28f);
                GUI.color = Color.white;
                GUI.Label(promptRect, "HOLD PRIMARY CLICK TO SKIP", _skipStyle);
                const int progressSegments = 24;
                int filledSegments = (int)Math.Round(progress * progressSegments);
                string progressBar =
                    "[" +
                    new string('#', filledSegments) +
                    new string('-', progressSegments - filledSegments) +
                    "]";
                GUI.Label(
                    new Rect(promptRect.x, promptRect.yMax + 2f, promptRect.width, 20f),
                    progressBar,
                    _skipStyle);
            }

            GUI.color = previousColor;
        }

        internal void End(CutsceneEndReason reason)
        {
            float releaseDuration =
                reason == CutsceneEndReason.Completed ? _configuration.FadeOutDuration : 0f;
            CloseOwnedBlackOverlay(releaseDuration);
            _holdElapsed = 0f;
            _titleAlpha = 0f;
        }

        private void UpdateTitle(float elapsed)
        {
            if (_configuration.TitleCardText == null || elapsed >= _configuration.TitleCardDuration)
            {
                _titleAlpha = 0f;
                return;
            }

            float fadeDuration = Math.Min(0.35f, _configuration.TitleCardDuration / 3f);
            float fadeIn = Math.Min(1f, elapsed / fadeDuration);
            float fadeOut = Math.Min(
                1f,
                (_configuration.TitleCardDuration - elapsed) / fadeDuration);
            _titleAlpha = Math.Min(fadeIn, fadeOut);
        }

        private void UpdateFadeIn(float elapsed)
        {
            if (_fadeInStarted ||
                _configuration.FadeInDuration <= 0f ||
                elapsed < _configuration.FadeInDelay)
            {
                return;
            }

            _fadeInStarted = true;
            CloseOwnedBlackOverlay(_configuration.FadeInDuration);
        }

        private void UpdateFadeOut(float elapsed)
        {
            if (_fadeOutStarted ||
                _configuration.FadeOutDuration <= 0f ||
                elapsed < _configuration.Duration - _configuration.FadeOutDuration)
            {
                return;
            }

            _fadeOutStarted = true;
            TryOpenBlackOverlay(_configuration.FadeOutDuration);
        }

        private bool TryOpenBlackOverlay(float duration)
        {
            try
            {
                if (!S1Singleton.InstanceExists)
                {
                    return false;
                }

                S1BlackOverlay overlay = S1Singleton.Instance;
                if (overlay.isShown)
                {
                    return false;
                }

                _ownsBlackOverlay = true;
                overlay.Open(duration);
                return true;
            }
            catch
            {
                _ownsBlackOverlay = false;
                return false;
            }
        }

        private void CloseOwnedBlackOverlay(float duration)
        {
            if (!_ownsBlackOverlay)
            {
                return;
            }

            try
            {
                if (S1Singleton.InstanceExists)
                {
                    S1Singleton.Instance.Close(duration);
                }
            }
            catch
            {
                // Presentation cleanup must not prevent native cutscene cleanup.
            }
            finally
            {
                _ownsBlackOverlay = false;
            }
        }

        private static bool IsSkipHeld()
        {
            try
            {
                return S1GameInput.GetButton(S1GameInput.ButtonCode.PrimaryClick);
            }
            catch
            {
                return false;
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 32,
                fontStyle = FontStyle.Bold
            };
            _titleStyle.normal.textColor = Color.white;

            _skipStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            _skipStyle.normal.textColor = Color.white;
        }
    }
}
