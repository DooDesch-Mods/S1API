using System.Collections;
using MelonLoader;
using S1API.Lifecycle;
using S1API.Products;
using ProductProperty = S1API.Properties.Property;
using UnityEngine;

#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes;
using S1LoadManager = Il2CppScheduleOne.Persistence.LoadManager;
using S1SaveManager = Il2CppScheduleOne.Persistence.SaveManager;
using S1SaveInfo = Il2CppScheduleOne.Persistence.SaveInfo;
using S1MetaData = Il2CppScheduleOne.Persistence.Datas.MetaData;
using S1DateTimeData = Il2CppScheduleOne.Persistence.Datas.DateTimeData;
using S1DateTime = Il2CppSystem.DateTime;
using S1Lobby = Il2CppScheduleOne.Networking.Lobby;
using S1NativeProduct = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
#elif MONOMELON
using S1LoadManager = ScheduleOne.Persistence.LoadManager;
using S1SaveManager = ScheduleOne.Persistence.SaveManager;
using S1SaveInfo = ScheduleOne.Persistence.SaveInfo;
using S1MetaData = ScheduleOne.Persistence.Datas.MetaData;
using S1DateTimeData = ScheduleOne.Persistence.Datas.DateTimeData;
using S1DateTime = System.DateTime;
using S1Lobby = ScheduleOne.Networking.Lobby;
using S1NativeProduct = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
#endif

[assembly: MelonInfo(typeof(S1API.WeedVariantSmoke.Core), "S1API Weed Variant Smoke", "0.1.0", "ifBars")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace S1API.WeedVariantSmoke;

public sealed class Core : MelonMod
{
    private static readonly Color32 MainColor = new Color32(104, 156, 82, 255);
    private static readonly Color32 SecondaryColor = new Color32(153, 108, 189, 255);
    private static readonly Color32 LeafColor = new Color32(63, 124, 54, 255);
    private static readonly Color32 StemColor = new Color32(96, 74, 48, 255);

    private bool _enabled;
    private bool _started;
    private bool _completed;
    private bool _hostObservedPeerBeforeLoad;
    private DateTime _deadlineUtc;
    private string _role = "host";
    private string _token = string.Empty;
    private string _outputDirectory = string.Empty;
    private string _savePath = string.Empty;

    private string ProductId => $"s1api.smoke:{_token}";
    private string ProductName => $"S1API Weed Smoke {_token.Substring(0, Math.Min(8, _token.Length))}";
    private string ResultPath => Path.Combine(_outputDirectory, $"result-{_role}.txt");

    public override void OnInitializeMelon()
    {
        _enabled = HasArgument("--s1api-weed-smoke");
        if (!_enabled)
            return;

        _role = GetArgument("--s1api-weed-smoke-role")?.Trim().ToLowerInvariant() ?? "host";
        if (_role == "creator")
            _role = "host";
        _token = GetArgument("--s1api-weed-smoke-token")?.Trim() ?? Guid.NewGuid().ToString("N");
        _outputDirectory = GetArgument("--s1api-weed-smoke-dir") ??
            Path.Combine(Path.GetTempPath(), "S1API.WeedVariantSmoke");
        _savePath = GetArgument("--s1api-weed-smoke-save") ??
            Path.Combine(_outputDirectory, "SaveGame_WeedVariantSmoke");

        if (!int.TryParse(GetArgument("--s1api-weed-smoke-timeout"), out var timeoutSeconds))
            timeoutSeconds = 150;

        _deadlineUtc = DateTime.UtcNow.AddSeconds(Math.Max(timeoutSeconds, 30));
        Directory.CreateDirectory(_outputDirectory);
        if (File.Exists(ResultPath))
            File.Delete(ResultPath);

        LoggerInstance.Msg(
            $"Weed smoke enabled. Runtime={RuntimeName}|Role={_role}|Token={_token}|Output={_outputDirectory}");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (!_enabled || _completed)
            return;

        LoggerInstance.Msg($"Weed smoke scene. Runtime={RuntimeName}|Role={_role}|Scene={sceneName}");
        if (sceneName == "Menu" && !_started)
        {
            _started = true;
            if (_role == "host" || _role == "reload")
                MelonCoroutines.Start(StartGameFromMenu());
        }

        if (sceneName == "Main")
            MelonCoroutines.Start(RunInMainScene());
    }

    public override void OnUpdate()
    {
        if (!_enabled || _completed || DateTime.UtcNow < _deadlineUtc)
            return;

        Fail($"Timed out. Runtime={RuntimeName}|Role={_role}|Scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    private IEnumerator StartGameFromMenu()
    {
        yield return new WaitForSecondsRealtime(1f);

        if (_role == "host")
        {
            while (S1Lobby.Instance == null ||
                   !S1Lobby.Instance.IsInLobby ||
                   S1Lobby.Instance.PlayerCount < 2)
            {
                if (DateTime.UtcNow >= _deadlineUtc)
                {
                    Fail("Host did not observe another lobby member before loading the save.");
                    yield break;
                }

                yield return null;
            }

            _hostObservedPeerBeforeLoad = true;
            LoggerInstance.Msg(
                $"Weed smoke join-before-load gate passed. LobbyPlayers={S1Lobby.Instance.PlayerCount}");
        }

        try
        {
            if (_role == "host")
                PrepareDisposableSave();
            else if (!Directory.Exists(_savePath))
                throw new DirectoryNotFoundException($"Reload save not found: {_savePath}");

            var metadata = new S1MetaData(
                (S1DateTimeData?)null,
                (S1DateTimeData?)null,
                Application.version,
                Application.version,
                playTutorial: false);
            var saveInfo = new S1SaveInfo(
                _savePath,
                -1,
                "S1API Weed Variant Smoke",
                GetNow(),
                GetNow(),
                0f,
                Application.version,
                metadata);

            LoggerInstance.Msg(
                $"Weed smoke starting native load. Role={_role}|Save={_savePath}");
            S1LoadManager.Instance.StartGame(
                saveInfo,
                allowLoadStacking: false,
                allowSaveBackup: false);
        }
        catch (Exception exception)
        {
            Fail("Failed to start the native save flow.", exception);
        }
    }

    private IEnumerator RunInMainScene()
    {
        while (S1LoadManager.Instance == null || !S1LoadManager.Instance.IsGameLoaded)
            yield return null;

        if (_role == "host")
        {
            yield return RunHostCreation();
            yield break;
        }

        yield return ObserveNativeProduct();
    }

    private IEnumerator RunHostCreation()
    {
        yield return new WaitForSeconds(1f);

        try
        {
            var created = WeedItemCreator
                .CreateBuilder(ProductId)
                .WithName(ProductName)
                .WithProperties(ProductProperty.Calming, ProductProperty.Munchies)
                .WithAppearance(new WeedAppearanceSettings(
                    MainColor,
                    SecondaryColor,
                    LeafColor,
                    StemColor))
                .Build();

            Require(created is WeedDefinition, "Build did not return WeedDefinition.");

            var duplicate = WeedItemCreator
                .CreateBuilder(ProductId.ToUpperInvariant())
                .WithName("Ignored Duplicate Name")
                .WithProperty(ProductProperty.Euphoric)
                .Build();
            Require(duplicate.ID == created.ID, "Case-insensitive duplicate did not resolve the existing definition.");
            Require(duplicate.Name == ProductName, "Duplicate creation replaced the first native configuration.");
        }
        catch (Exception exception)
        {
            Fail("Host creation failed.", exception);
            yield break;
        }

        while (!TryValidateProduct(out _))
        {
            if (DateTime.UtcNow >= _deadlineUtc)
            {
                Fail("Host product did not reach its complete native state.");
                yield break;
            }

            yield return null;
        }

        try
        {
            GameLifecycle.OnSaveComplete += OnHostSaveComplete;
            S1SaveManager.Instance.Save();
        }
        catch (Exception exception)
        {
            Fail("Failed to save the host product.", exception);
        }
    }

    private IEnumerator ObserveNativeProduct()
    {
        var evidence = string.Empty;
        while (!TryValidateProduct(out evidence))
        {
            if (DateTime.UtcNow >= _deadlineUtc)
            {
                Fail($"Role '{_role}' did not observe the native weed product.");
                yield break;
            }

            yield return null;
        }

        var lobbyPlayers = S1Lobby.Instance?.PlayerCount ?? 0;
        var lateJoin = _role == "latejoin";
        var restored = _role == "reload";
        Pass(
            $"{evidence}|LobbyPlayers={lobbyPlayers}|LateJoin={lateJoin}|RestoredWithoutBuild={restored}");
    }

    private void OnHostSaveComplete()
    {
        GameLifecycle.OnSaveComplete -= OnHostSaveComplete;
        if (!TryValidateProduct(out var evidence))
        {
            Fail("Product validation failed after the native save completed.");
            return;
        }

        Pass(
            $"{evidence}|JoinedBeforeLoad={_hostObservedPeerBeforeLoad}|Saved=True|Save={_savePath}");
    }

    private bool TryValidateProduct(out string evidence)
    {
        evidence = string.Empty;
        if (S1Registry.Instance == null ||
            S1NativeProduct.ProductManager.Instance == null ||
            !S1Registry.ItemExists(ProductId))
        {
            return false;
        }

        WeedDefinition? product = ProductManager.DiscoveredProducts
            .OfType<WeedDefinition>()
            .FirstOrDefault(definition =>
                string.Equals(definition.ID, ProductId, StringComparison.OrdinalIgnoreCase));
        if (product == null || product.Icon == null || product.Name != ProductName)
            return false;

        var propertyIds = product.Properties
            .Select(property => property.ID)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!propertyIds.SetEquals(new[] { ProductProperty.Calming.ID, ProductProperty.Munchies.ID }))
            return false;

        var allProducts = S1NativeProduct.ProductManager.Instance.AllProducts;
        var nativeProduct = default(S1NativeProduct.WeedDefinition);
        for (var i = 0; i < allProducts.Count; i++)
        {
            var candidate = allProducts[i];
            if (candidate != null &&
                string.Equals(candidate.ID, ProductId, StringComparison.OrdinalIgnoreCase))
            {
#if IL2CPPMELON
                nativeProduct = candidate.TryCast<S1NativeProduct.WeedDefinition>();
#else
                nativeProduct = candidate as S1NativeProduct.WeedDefinition;
#endif
                break;
            }
        }

        if (nativeProduct == null ||
            !ColorsMatch(nativeProduct.MainMat.color, MainColor) ||
            !ColorsMatch(nativeProduct.SecondaryMat.color, SecondaryColor) ||
            !ColorsMatch(nativeProduct.LeafMat.color, LeafColor) ||
            !ColorsMatch(nativeProduct.StemMat.color, StemColor))
        {
            return false;
        }

        var price = ProductManager.GetPrice(product);
        if (price <= 0f)
            return false;

        evidence =
            $"Runtime={RuntimeName}|Role={_role}|Scene=Main|Token={_token}|Id={product.ID}|" +
            $"Name={product.Name}|Properties={string.Join(",", propertyIds.OrderBy(value => value))}|" +
            $"Registry=True|AllProducts=True|Discovered=True|Icon=True|Appearance=True|Price={price:0.##}";
        return true;
    }

    private static bool ColorsMatch(Color actual, Color32 expected)
    {
        var actual32 = (Color32)actual;
        return actual32.r == expected.r &&
               actual32.g == expected.g &&
               actual32.b == expected.b &&
               actual32.a == expected.a;
    }

    private void PrepareDisposableSave()
    {
        var defaultSavePath = Path.Combine(Application.streamingAssetsPath, "DefaultSave");
        if (!Directory.Exists(defaultSavePath))
            throw new DirectoryNotFoundException($"Default save folder not found: {defaultSavePath}");

        if (Directory.Exists(_savePath))
            Directory.Delete(_savePath, recursive: true);

        CopyDirectory(defaultSavePath, _savePath);
        File.WriteAllText(
            Path.Combine(_savePath, "Game.json"),
            "{\"DataType\":\"GameData\",\"DataVersion\":0,\"GameVersion\":\"" + Application.version +
            "\",\"OrganisationName\":\"S1API Weed Variant Smoke\",\"Seed\":9531,\"Settings\":{\"ConsoleEnabled\":false}}");
        File.WriteAllText(
            Path.Combine(_savePath, "Metadata.json"),
            "{\"DataType\":\"MetaData\",\"DataVersion\":0,\"GameVersion\":\"" + Application.version +
            "\",\"CreationDate\":null,\"LastPlayedDate\":null,\"CreationVersion\":\"" + Application.version +
            "\",\"LastSaveVersion\":\"" + Application.version + "\",\"PlayTutorial\":false}");
    }

    private static void CopyDirectory(string sourcePath, string targetPath)
    {
        var normalizedSource = Path.GetFullPath(sourcePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedTarget = Path.GetFullPath(targetPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        Directory.CreateDirectory(normalizedTarget);
        foreach (var directory in Directory.GetDirectories(normalizedSource, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(MapChildPath(directory, normalizedSource, normalizedTarget));

        foreach (var file in Directory.GetFiles(normalizedSource, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(
                    file,
                    MapChildPath(file, normalizedSource, normalizedTarget),
                    overwrite: true);
            }
        }
    }

    private static string MapChildPath(
        string childPath,
        string normalizedSource,
        string normalizedTarget)
    {
        var relativePath = childPath
            .Substring(normalizedSource.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(normalizedTarget, relativePath);
    }

    private void Pass(string evidence)
    {
        _completed = true;
        var result = $"PASS|{evidence}";
        File.WriteAllText(ResultPath, result);
        LoggerInstance.Msg(result);
        QuitIfRequested();
    }

    private void Fail(string message, Exception? exception = null)
    {
        _completed = true;
        GameLifecycle.OnSaveComplete -= OnHostSaveComplete;
        var result = $"FAIL|Runtime={RuntimeName}|Role={_role}|{message}";
        if (exception != null)
            result += $"|{exception.GetType().Name}|{exception.Message}";

        File.WriteAllText(ResultPath, result);
        LoggerInstance.Error(result);
        if (exception != null)
            LoggerInstance.Error(exception.ToString());
        QuitIfRequested();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static bool HasArgument(string name)
    {
        return Environment.GetCommandLineArgs()
            .Any(argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetArgument(string name)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (var i = 0; i < arguments.Length - 1; i++)
        {
            if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                return arguments[i + 1];
        }

        return null;
    }

    private static void QuitIfRequested()
    {
        if (HasArgument("--s1api-weed-smoke-exit"))
            Application.Quit();
    }

    private static S1DateTime GetNow()
    {
#if IL2CPPMELON
        return new S1DateTime(DateTime.Now.Ticks);
#else
        return DateTime.Now;
#endif
    }

    private static string RuntimeName
    {
        get
        {
#if IL2CPPMELON
            return "Il2CppMelon";
#else
            return "MonoMelon";
#endif
        }
    }
}
