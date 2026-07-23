using System.Collections;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using S1API.Cutscenes;
using UnityEngine;

#if IL2CPP
using S1BlackOverlay = Il2CppScheduleOne.UI.BlackOverlay;
using S1BlackOverlaySingleton = Il2CppScheduleOne.DevUtilities.Singleton<Il2CppScheduleOne.UI.BlackOverlay>;
using S1DateTime = Il2CppSystem.DateTime;
using S1DateTimeData = Il2CppScheduleOne.Persistence.Datas.DateTimeData;
using S1GameInput = Il2CppScheduleOne.GameInput;
using S1GameInputSingleton = Il2CppScheduleOne.DevUtilities.Singleton<Il2CppScheduleOne.GameInput>;
using S1LoadManager = Il2CppScheduleOne.Persistence.LoadManager;
using S1MetaData = Il2CppScheduleOne.Persistence.Datas.MetaData;
using S1PlayerCamera = Il2CppScheduleOne.PlayerScripts.PlayerCamera;
using S1PlayerCameraSingleton = Il2CppScheduleOne.DevUtilities.PlayerSingleton<Il2CppScheduleOne.PlayerScripts.PlayerCamera>;
using S1PlayerMovement = Il2CppScheduleOne.PlayerScripts.PlayerMovement;
using S1PlayerMovementSingleton = Il2CppScheduleOne.DevUtilities.PlayerSingleton<Il2CppScheduleOne.PlayerScripts.PlayerMovement>;
using S1SaveInfo = Il2CppScheduleOne.Persistence.SaveInfo;
using S1SceneState = Il2CppScheduleOne.SceneState;
using S1Settings = Il2CppScheduleOne.DevUtilities.Settings;
using S1SettingsSingleton = Il2CppScheduleOne.DevUtilities.Singleton<Il2CppScheduleOne.DevUtilities.Settings>;
#else
using S1BlackOverlay = ScheduleOne.UI.BlackOverlay;
using S1BlackOverlaySingleton = ScheduleOne.DevUtilities.Singleton<ScheduleOne.UI.BlackOverlay>;
using S1DateTime = System.DateTime;
using S1DateTimeData = ScheduleOne.Persistence.Datas.DateTimeData;
using S1GameInput = ScheduleOne.GameInput;
using S1GameInputSingleton = ScheduleOne.DevUtilities.Singleton<ScheduleOne.GameInput>;
using S1LoadManager = ScheduleOne.Persistence.LoadManager;
using S1MetaData = ScheduleOne.Persistence.Datas.MetaData;
using S1PlayerCamera = ScheduleOne.PlayerScripts.PlayerCamera;
using S1PlayerCameraSingleton = ScheduleOne.DevUtilities.PlayerSingleton<ScheduleOne.PlayerScripts.PlayerCamera>;
using S1PlayerMovement = ScheduleOne.PlayerScripts.PlayerMovement;
using S1PlayerMovementSingleton = ScheduleOne.DevUtilities.PlayerSingleton<ScheduleOne.PlayerScripts.PlayerMovement>;
using S1SaveInfo = ScheduleOne.Persistence.SaveInfo;
using S1SceneState = ScheduleOne.SceneState;
using S1Settings = ScheduleOne.DevUtilities.Settings;
using S1SettingsSingleton = ScheduleOne.DevUtilities.Singleton<ScheduleOne.DevUtilities.Settings>;
#endif

[assembly: MelonInfo(typeof(CutsceneSmoke.Core), "S1API Cutscene Smoke", "0.1.0", "ifBars")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace CutsceneSmoke;

public sealed class Core : MelonMod
{
    private const string ResultFileName = "result.txt";
    private const string ScreenshotFileName = "cutscene-title-skip.png";
    private bool _started;
    private bool _completed;
    private int _baselineActiveUiCount;
    private bool _baselineCanMove;
    private float _baselineFov;
    private string _baselineStateName = string.Empty;

    public override void OnInitializeMelon()
    {
        if (!IsSmokeEnabled())
        {
            return;
        }

        PrepareResultDirectory();
        LoggerInstance.Msg("S1API cutscene smoke enabled.");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (!IsSmokeEnabled() || _completed)
        {
            return;
        }

        if (sceneName == "Menu" && !_started)
        {
            _started = true;
            MelonCoroutines.Start(StartGameFromMenu());
        }
        else if (sceneName == "Main")
        {
            MelonCoroutines.Start(RunScenarios());
        }
    }

    private IEnumerator StartGameFromMenu()
    {
        yield return new WaitForSecondsRealtime(1f);

        try
        {
            string savePath = PrepareDisposableSave();
            var metadata = new S1MetaData(
                (S1DateTimeData?)null,
                (S1DateTimeData?)null,
                Application.version,
                Application.version,
                playTutorial: false);
            var saveInfo = new S1SaveInfo(
                savePath,
                -1,
                "S1API Cutscene Smoke",
                GetNow(),
                GetNow(),
                0f,
                Application.version,
                metadata);

            LoggerInstance.Msg($"Starting disposable save: {savePath}");
            S1LoadManager.Instance.StartGame(
                saveInfo,
                allowLoadStacking: false,
                allowSaveBackup: false);
        }
        catch (Exception ex)
        {
            Fail("Failed to start the disposable game flow", ex);
        }
    }

    private IEnumerator RunScenarios()
    {
        while (S1LoadManager.Instance == null ||
               !S1LoadManager.Instance.IsGameLoaded ||
               !S1PlayerCameraSingleton.InstanceExists ||
               !S1PlayerMovementSingleton.InstanceExists ||
               !S1BlackOverlaySingleton.InstanceExists ||
               !S1GameInputSingleton.InstanceExists ||
               !S1SettingsSingleton.InstanceExists)
        {
            yield return null;
        }

        float readinessDeadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < readinessDeadline)
        {
            S1PlayerCamera playerCamera = S1PlayerCameraSingleton.Instance;
            if (S1SceneState.Current != null &&
                S1SceneState.ActiveState != null &&
                playerCamera.ActiveUIElementCount == 0 &&
                !S1BlackOverlaySingleton.Instance.isShown)
            {
                break;
            }

            yield return null;
        }

        if (S1SceneState.Current == null ||
            S1SceneState.ActiveState == null ||
            S1PlayerCameraSingleton.Instance.ActiveUIElementCount != 0 ||
            S1BlackOverlaySingleton.Instance.isShown)
        {
            Fail(
                "Player input and presentation state did not become ready before cutscene testing",
                new TimeoutException(
                    "The 15 second gameplay-readiness gate expired: " +
                    $"CanMove={S1PlayerMovementSingleton.Instance.CanMove}, " +
                    $"CanLook={S1PlayerCameraSingleton.Instance.CanLook}, " +
                    $"ActiveUI={S1PlayerCameraSingleton.Instance.ActiveUIElementCount}, " +
                    $"OverlayShown={S1BlackOverlaySingleton.Instance.isShown}, " +
                    $"SceneState={S1SceneState.ActiveState?.name ?? "<null>"}."));
            yield break;
        }

        var stack = new Stack<IEnumerator>();
        stack.Push(RunScenarioSequence());
        while (stack.Count > 0)
        {
            IEnumerator currentRoutine = stack.Peek();
            bool movedNext;
            object? current;
            try
            {
                movedNext = currentRoutine.MoveNext();
                current = movedNext ? currentRoutine.Current : null;
            }
            catch (Exception ex)
            {
                CleanupAfterFailure();
                Fail("Cutscene smoke scenario failed", ex);
                yield break;
            }

            if (!movedNext)
            {
                stack.Pop();
            }
            else if (current is IEnumerator nestedRoutine)
            {
                stack.Push(nestedRoutine);
            }
            else
            {
                yield return current;
            }
        }
    }

    private IEnumerator RunScenarioSequence()
    {
        CapturePlayerBaseline();
        yield return RunCompletionAndPresentation();
        yield return RunProgrammaticSkip();
        yield return RunProgrammaticStop();
        yield return RunCallbackFailure();
        yield return RunHoldToSkip();
        yield return RunFadeOwnership();

        Require(CutsceneManager.Active == null, "An active cutscene remained after all scenarios.");
        AssertPlayerRestored("final");

        string screenshotPath = Path.Combine(GetResultDirectory(), ScreenshotFileName);
        Require(
            File.Exists(screenshotPath) && new FileInfo(screenshotPath).Length > 0,
            $"The presentation screenshot was not written: {screenshotPath}");

        Pass();
    }

    private IEnumerator RunCompletionAndPresentation()
    {
        S1PlayerCamera playerCamera = S1PlayerCameraSingleton.Instance;
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;
        Vector3 endPosition = startPosition + playerCamera.transform.right * 2f + Vector3.up;
        float lastProgress = -1f;
        int endedCount = 0;
        bool endedWithoutActiveHandle = false;
        bool reachedEndPose = false;
        CutsceneEndReason? reason = null;

        using CutsceneHandle overlap = CreateFixedCutscene("cutscene-smoke:overlap", 1f);
        using CutsceneHandle handle = new CutsceneBuilder("cutscene-smoke:complete")
            .WithName("S1API completion smoke")
            .WithDuration(1.5f)
            .WithFov(60f)
            .WithInitialCamera(startPosition, startRotation)
            .WithCameraUpdate(frame =>
            {
                Require(frame.Progress >= lastProgress, "Normalized progress moved backwards.");
                lastProgress = frame.Progress;
                Vector3 position = Vector3.Lerp(startPosition, endPosition, frame.Progress);
                frame.Camera.SetPositionAndRotation(position, startRotation);
                reachedEndPose = frame.Progress >= 1f && Vector3.Distance(frame.Camera.Position, endPosition) < 0.01f;
            })
            .WithFadeIn(0.2f)
            .WithFadeOut(0.25f)
            .WithHoldToSkip()
            .WithTitleCard("S1API CUTSCENE SMOKE", 1.2f)
            .OnEnded(endReason =>
            {
                endedCount++;
                reason = endReason;
                endedWithoutActiveHandle = CutsceneManager.Active == null;
            })
            .Build();

        Require(handle.Play(), "The completion cutscene did not start.");
        Require(!overlap.Play(), "Overlapping cutscene playback was accepted.");
        yield return new WaitForSecondsRealtime(0.55f);
        ScreenCapture.CaptureScreenshot(Path.Combine(GetResultDirectory(), ScreenshotFileName));

        yield return WaitForEnd(handle, 4f);
        yield return new WaitForSecondsRealtime(0.6f);

        Require(reason == CutsceneEndReason.Completed, $"Completion reason was {reason}.");
        Require(endedCount == 1, $"Completion callback count was {endedCount}.");
        Require(endedWithoutActiveHandle, "Active was not cleared before the completion callback.");
        Require(lastProgress >= 1f, $"Completion progress ended at {lastProgress}.");
        Require(reachedEndPose, "The camera callback did not receive the normalized end pose.");
        Require(CutsceneManager.Active == null, "Active remained set after completion.");
        Require(!S1BlackOverlaySingleton.Instance.isShown, "S1API-owned fade overlay remained shown.");
        AssertPlayerRestored("completion");
    }

    private IEnumerator RunProgrammaticSkip()
    {
        CutsceneEndReason? reason = null;
        int endedCount = 0;
        using CutsceneHandle handle = CreateFixedCutscene(
            "cutscene-smoke:skip",
            5f,
            endReason =>
            {
                reason = endReason;
                endedCount++;
            });

        Require(handle.Play(), "The programmatic-skip cutscene did not start.");
        yield return new WaitForSecondsRealtime(0.15f);
        Require(handle.Skip(), "The first programmatic Skip() did not end playback.");
        Require(!handle.Skip(), "A repeated Skip() reported another end.");
        Require(!handle.Stop(), "Stop() reported an end after Skip() already ended playback.");
        yield return new WaitForSecondsRealtime(0.4f);
        Require(reason == CutsceneEndReason.Skipped, $"Skip reason was {reason}.");
        Require(endedCount == 1, $"Skip callback count was {endedCount}.");
        AssertPlayerRestored("programmatic skip");
    }

    private IEnumerator RunProgrammaticStop()
    {
        CutsceneEndReason? reason = null;
        int endedCount = 0;
        using CutsceneHandle handle = CreateFixedCutscene(
            "cutscene-smoke:stop",
            5f,
            endReason =>
            {
                reason = endReason;
                endedCount++;
            });

        Require(handle.Play(), "The programmatic-stop cutscene did not start.");
        yield return new WaitForSecondsRealtime(0.15f);
        Require(handle.Stop(), "The first Stop() did not end playback.");
        Require(!handle.Stop(), "A repeated Stop() reported another end.");
        Require(!handle.Skip(), "Skip() reported an end after Stop() already ended playback.");
        yield return new WaitForSecondsRealtime(0.4f);
        Require(reason == CutsceneEndReason.Stopped, $"Stop reason was {reason}.");
        Require(endedCount == 1, $"Stop callback count was {endedCount}.");
        AssertPlayerRestored("programmatic stop");
    }

    private IEnumerator RunCallbackFailure()
    {
        CutsceneEndReason? reason = null;
        int endedCount = 0;
        S1PlayerCamera playerCamera = S1PlayerCameraSingleton.Instance;
        using CutsceneHandle handle = new CutsceneBuilder("cutscene-smoke:failure")
            .WithDuration(2f)
            .WithInitialCamera(playerCamera.transform.position, playerCamera.transform.rotation)
            .WithCameraUpdate(frame =>
            {
                if (frame.Progress >= 0.1f)
                {
                    throw new InvalidOperationException("Deliberate cutscene smoke callback failure.");
                }
            })
            .OnEnded(endReason =>
            {
                reason = endReason;
                endedCount++;
            })
            .Build();

        Require(handle.Play(), "The callback-failure cutscene did not start.");
        yield return WaitForEnd(handle, 3f);
        yield return new WaitForSecondsRealtime(0.4f);
        Require(reason == CutsceneEndReason.Failed, $"Callback-failure reason was {reason}.");
        Require(endedCount == 1, $"Failure callback count was {endedCount}.");
        Require(CutsceneManager.Active == null, "Active remained set after callback failure.");
        AssertPlayerRestored("callback failure");
    }

    private IEnumerator RunHoldToSkip()
    {
        CutsceneEndReason? reason = null;
        using CutsceneHandle handle = CreateFixedCutscene(
            "cutscene-smoke:hold",
            5f,
            endReason => reason = endReason,
            builder => builder.WithHoldToSkip(0.5f));

        Require(handle.Play(), "The hold-to-skip cutscene did not start.");
        float holdStartedAt = Time.realtimeSinceStartup;
        SetPrimaryClickHeld(true);
        yield return new WaitForSecondsRealtime(0.25f);
        Require(handle.IsPlaying, "Hold-to-skip fired before the configured duration.");
        yield return WaitForEnd(handle, 2f);
        float holdDuration = Time.realtimeSinceStartup - holdStartedAt;
        SetPrimaryClickHeld(false);
        yield return new WaitForSecondsRealtime(0.4f);

        Require(reason == CutsceneEndReason.Skipped, $"Hold-to-skip reason was {reason}.");
        Require(holdDuration >= 0.45f, $"Hold-to-skip fired too early at {holdDuration:0.000}s.");
        Require(holdDuration < 1.5f, $"Hold-to-skip fired too late at {holdDuration:0.000}s.");
        AssertPlayerRestored("hold-to-skip");
    }

    private IEnumerator RunFadeOwnership()
    {
        S1BlackOverlay overlay = S1BlackOverlaySingleton.Instance;
        overlay.Open(0f);
        yield return null;
        Require(overlay.isShown, "Could not establish a pre-owned black overlay.");

        using CutsceneHandle handle = CreateFixedCutscene(
            "cutscene-smoke:fade-ownership",
            0.6f,
            configure: builder => builder.WithFadeOut(0.25f));

        Require(handle.Play(), "The fade-ownership cutscene did not start.");
        yield return WaitForEnd(handle, 2f);
        Require(overlay.isShown, "S1API closed a black overlay it did not own.");
        overlay.Close(0f);
        yield return new WaitForSecondsRealtime(0.6f);
        AssertPlayerRestored("fade ownership");
    }

    private static void CleanupAfterFailure()
    {
        try
        {
            SetPrimaryClickHeld(false);
        }
        catch
        {
        }

        try
        {
            if (S1BlackOverlaySingleton.InstanceExists && S1BlackOverlaySingleton.Instance.isShown)
            {
                S1BlackOverlaySingleton.Instance.Close(0f);
            }
        }
        catch
        {
        }

        CutsceneManager.StopActive();
    }

    private static CutsceneHandle CreateFixedCutscene(
        string id,
        float duration,
        Action<CutsceneEndReason>? ended = null,
        Action<CutsceneBuilder>? configure = null)
    {
        S1PlayerCamera playerCamera = S1PlayerCameraSingleton.Instance;
        var builder = new CutsceneBuilder(id)
            .WithDuration(duration)
            .WithFov(60f)
            .WithInitialCamera(playerCamera.transform.position, playerCamera.transform.rotation);
        configure?.Invoke(builder);
        if (ended != null)
        {
            builder.OnEnded(ended);
        }

        return builder.Build();
    }

    private static IEnumerator WaitForEnd(CutsceneHandle handle, float timeoutSeconds)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (handle.IsPlaying && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Require(!handle.IsPlaying, $"Cutscene '{handle.Id}' did not end within {timeoutSeconds:0.0}s.");
    }

    private void CapturePlayerBaseline()
    {
        S1PlayerCamera playerCamera = S1PlayerCameraSingleton.Instance;
        S1PlayerMovement playerMovement = S1PlayerMovementSingleton.Instance;
        _baselineActiveUiCount = playerCamera.ActiveUIElementCount;
        _baselineCanMove = playerMovement.CanMove;
        S1Settings settings = S1SettingsSingleton.Instance;
        _baselineFov = settings.CameraFOV;
        _baselineStateName = S1SceneState.ActiveState?.name
            ?? throw new InvalidOperationException("No native active scene state was available.");
    }

    private void AssertPlayerRestored(string scenario)
    {
        S1PlayerCamera playerCamera = S1PlayerCameraSingleton.Instance;
        S1PlayerMovement playerMovement = S1PlayerMovementSingleton.Instance;
        Require(
            playerCamera.ActiveUIElementCount == _baselineActiveUiCount,
            $"{scenario}: active UI count was {playerCamera.ActiveUIElementCount}, expected {_baselineActiveUiCount}.");
        Require(playerCamera.CanLook, $"{scenario}: camera look was not usable after cleanup.");
        Require(
            playerMovement.CanMove == _baselineCanMove,
            $"{scenario}: PlayerMovement.CanMove changed from {_baselineCanMove} to {playerMovement.CanMove}.");
        string activeStateName = S1SceneState.ActiveState?.name ?? "<null>";
        Require(
            string.Equals(activeStateName, _baselineStateName, StringComparison.Ordinal),
            $"{scenario}: active state was '{activeStateName}', expected '{_baselineStateName}'.");
        Require(
            Math.Abs(playerCamera.Camera.fieldOfView - _baselineFov) <= 2f,
            $"{scenario}: FOV was {playerCamera.Camera.fieldOfView:0.00}, expected approximately {_baselineFov:0.00}.");
    }

    private static void SetPrimaryClickHeld(bool held)
    {
        if (!S1GameInputSingleton.InstanceExists ||
            S1GameInput.GetButton(S1GameInput.ButtonCode.PrimaryClick) == held)
        {
            return;
        }

        MethodInfo method = AccessTools.Method(typeof(S1GameInput), "OnPrimaryClick")
            ?? throw new MissingMethodException(typeof(S1GameInput).FullName, "OnPrimaryClick");
        method.Invoke(S1GameInputSingleton.Instance, null);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void Pass()
    {
        _completed = true;
        string result =
            $"PASS|Backend={GetBackend()}|Completed=True|EndPose=True|OverlapRejected=True|" +
            "ProgrammaticSkip=True|IdempotentStopSkip=True|FailureRecovered=True|" +
            "HoldTiming=True|FadeOwnership=True|PlayerRestored=True|Screenshot=True";
        WriteResult(result);
        LoggerInstance.Msg(result);
        QuitIfRequested();
    }

    private void Fail(string message, Exception ex)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        string result =
            $"FAIL|Backend={GetBackend()}|{message}|{ex.GetType().Name}|{ex.Message}";
        WriteResult(result);
        LoggerInstance.Error(result);
        LoggerInstance.Error(ex.ToString());
        QuitIfRequested();
    }

    private static string GetBackend()
    {
#if IL2CPP
        return "Il2Cpp";
#else
        return "Mono";
#endif
    }

    private static bool IsSmokeEnabled() =>
        Environment.GetCommandLineArgs().Any(
            argument => string.Equals(
                argument,
                "--s1api-cutscene-smoke",
                StringComparison.OrdinalIgnoreCase));

    private static bool ShouldExit() =>
        Environment.GetCommandLineArgs().Any(
            argument => string.Equals(
                argument,
                "--s1api-cutscene-smoke-exit",
                StringComparison.OrdinalIgnoreCase));

    private static S1DateTime GetNow()
    {
#if IL2CPP
        return new S1DateTime(DateTime.Now.Ticks);
#else
        return DateTime.Now;
#endif
    }

    private static string GetResultDirectory()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (string.Equals(
                    arguments[i],
                    "--s1api-cutscene-smoke-dir",
                    StringComparison.OrdinalIgnoreCase))
            {
                return arguments[i + 1];
            }
        }

        return Path.Combine(Path.GetTempPath(), "S1API-CutsceneSmoke");
    }

    private static void PrepareResultDirectory()
    {
        string resultDirectory = GetResultDirectory();
        Directory.CreateDirectory(resultDirectory);
        foreach (string fileName in new[] { ResultFileName, ScreenshotFileName })
        {
            string path = Path.Combine(resultDirectory, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static string PrepareDisposableSave()
    {
        string savePath = Path.Combine(GetResultDirectory(), "SaveGame_CutsceneSmoke");
        string defaultSavePath = Path.Combine(Application.streamingAssetsPath, "DefaultSave");
        if (!Directory.Exists(defaultSavePath))
        {
            throw new DirectoryNotFoundException($"Default save folder not found: {defaultSavePath}");
        }

        if (Directory.Exists(savePath))
        {
            Directory.Delete(savePath, recursive: true);
        }

        CopyDirectory(defaultSavePath, savePath);
        File.WriteAllText(
            Path.Combine(savePath, "Game.json"),
            "{\"DataType\":\"GameData\",\"DataVersion\":0,\"GameVersion\":\"" + Application.version +
            "\",\"OrganisationName\":\"S1API Cutscene Smoke\",\"Seed\":9631,\"Settings\":{\"ConsoleEnabled\":false}}");
        File.WriteAllText(
            Path.Combine(savePath, "Metadata.json"),
            "{\"DataType\":\"MetaData\",\"DataVersion\":0,\"GameVersion\":\"" + Application.version +
            "\",\"CreationDate\":null,\"LastPlayedDate\":null,\"CreationVersion\":\"" + Application.version +
            "\",\"LastSaveVersion\":\"" + Application.version + "\",\"PlayTutorial\":false}");
        return savePath;
    }

    private static void CopyDirectory(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(targetPath);
        foreach (string directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourcePath, targetPath));
        }

        foreach (string file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(file, file.Replace(sourcePath, targetPath), overwrite: true);
            }
        }
    }

    private static void WriteResult(string result)
    {
        File.WriteAllText(Path.Combine(GetResultDirectory(), ResultFileName), result);
    }

    private static void QuitIfRequested()
    {
        if (ShouldExit())
        {
            Application.Quit();
        }
    }
}
