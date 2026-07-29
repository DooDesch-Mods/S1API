using System;
using S1API.Logging;
using UnityEngine;
#if IL2CPPMELON
using Il2CppInterop.Runtime;
#endif

namespace S1API.Internal.Diagnostics
{
    /// <summary>
    /// INTERNAL: Hooks Unity's threaded log callback to emit detailed stack traces for NullReferenceException logs.
    /// </summary>
    internal static class UnityExceptionTraceHook
    {
        private const string KnownBaseGameNullReferenceAdvisory =
            "[Unity] Note: this NullReferenceException is commonly observed in the native/base game and is usually not caused by a mod. The original stack trace is retained for diagnosis.";

        private static readonly Log Logger = new Log("S1API.UnityExceptionTrace");
        private static readonly object Sync = new object();
#if IL2CPPMELON
        private static readonly System.Action<string, string, LogType> ManagedCallback = OnUnityLogMessageReceived;
        private static readonly Application.LogCallback Callback = DelegateSupport.ConvertDelegate<Application.LogCallback>(ManagedCallback) ?? ManagedCallback;
#endif

        private static string? _lastExceptionSignature;
        private static DateTime _lastExceptionAtUtc;
        private static bool _installed;

        internal static void Install()
        {
            if (_installed)
            {
                return;
            }

#if IL2CPPMELON
            Application.add_logMessageReceivedThreaded(Callback);
#else
            Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
#endif
            _installed = true;
        }

        internal static void Remove()
        {
            if (!_installed)
            {
                return;
            }

#if IL2CPPMELON
            _installed = false;
            return;
#else
            try
            {
                Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
            }
            catch (Exception ex)
            {
                Logger.Warning($"[Unity] Failed to remove threaded log callback during shutdown: {ex.Message}");
            }

            _installed = false;
#endif
        }

        private static void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }

            if (!LooksLikeNullReference(condition, stackTrace))
            {
                return;
            }

            string signature = string.Concat(condition ?? string.Empty, "\n", stackTrace ?? string.Empty);
            if (ShouldSuppressDuplicate(signature))
            {
                return;
            }

            Logger.Error($"[Unity] {condition}");

            if (GetKnownBaseGameNullReferenceAdvisory(stackTrace) is not null)
            {
                Logger.Warning(KnownBaseGameNullReferenceAdvisory);
            }

            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                Logger.Error($"[Unity] stack trace:\n{stackTrace}");
            }
        }

        private static bool LooksLikeNullReference(string condition, string stackTrace)
        {
            string haystack = string.Concat(condition ?? string.Empty, "\n", stackTrace ?? string.Empty);
            return haystack.Contains("NullReferenceException", StringComparison.Ordinal);
        }

        internal static string? GetKnownBaseGameNullReferenceAdvisory(string? stackTrace)
        {
            if (string.IsNullOrWhiteSpace(stackTrace))
            {
                return null;
            }

            return stackTrace.Contains("ScheduleOne.Weather.EnvironmentManager.GetWeatherProfileFromPosition", StringComparison.Ordinal)
                || stackTrace.Contains("ScheduleOne.NPCs.NPCMovement+<FaceDirection_Process>", StringComparison.Ordinal)
                || stackTrace.Contains("ScheduleOne.Configuration.ConfigurationServiceNetworker.OnDestroy", StringComparison.Ordinal)
                || stackTrace.Contains("ScheduleOne.UI.PauseMenu.OnDestroy", StringComparison.Ordinal)
                ? KnownBaseGameNullReferenceAdvisory
                : null;
        }

        private static bool ShouldSuppressDuplicate(string signature)
        {
            lock (Sync)
            {
                DateTime now = DateTime.UtcNow;
                if (signature == _lastExceptionSignature && (now - _lastExceptionAtUtc).TotalSeconds < 1)
                {
                    return true;
                }

                _lastExceptionSignature = signature;
                _lastExceptionAtUtc = now;
                return false;
            }
        }
    }
}
