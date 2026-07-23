using System.Collections;
using System.Reflection;
using MelonLoader;
using S1API.Internal.Products;
using S1API.Lifecycle;
using UnityEngine;

#if IL2CPPMELON
using S1DateTime = Il2CppSystem.DateTime;
using S1DateTimeData = Il2CppScheduleOne.Persistence.Datas.DateTimeData;
using S1LoadManager = Il2CppScheduleOne.Persistence.LoadManager;
using S1MetaData = Il2CppScheduleOne.Persistence.Datas.MetaData;
using S1NativeProduct = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
using S1SaveInfo = Il2CppScheduleOne.Persistence.SaveInfo;
using S1SaveManager = Il2CppScheduleOne.Persistence.SaveManager;
#elif MONOMELON
using S1DateTime = System.DateTime;
using S1DateTimeData = ScheduleOne.Persistence.Datas.DateTimeData;
using S1LoadManager = ScheduleOne.Persistence.LoadManager;
using S1MetaData = ScheduleOne.Persistence.Datas.MetaData;
using S1NativeProduct = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
using S1SaveInfo = ScheduleOne.Persistence.SaveInfo;
using S1SaveManager = ScheduleOne.Persistence.SaveManager;
#endif

[assembly: MelonInfo(
    typeof(S1API.CustomProductLifecycleSmoke.Core),
    "S1API Custom Product Lifecycle Smoke",
    "0.1.0",
    "ifBars")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace S1API.CustomProductLifecycleSmoke;

public sealed class Core : MelonMod
{
    private const float InitialPrice = 143f;
    private const float SavedPrice = 287f;
    private const int RequiredMainLoads = 3;

    private bool _enabled;
    private bool _started;
    private bool _completed;
    private bool _savePending;
    private int _mainLoadCount;
    private int _preLoadRestorationCount;
    private DateTime _deadlineUtc;
    private string _token = string.Empty;
    private string _outputDirectory = string.Empty;
    private string _savePath = string.Empty;
    private S1SaveInfo? _saveInfo;
    private S1NativeProduct.ProductDefinition? _definition;

    private string ProductId =>
        $"s1api.lifecycle-smoke:{_token}";

    private string ProductName =>
        $"S1API Lifecycle Smoke {_token.Substring(0, Math.Min(8, _token.Length))}";

    private string ResultPath =>
        Path.Combine(_outputDirectory, "result.txt");

    public override void OnInitializeMelon()
    {
        _enabled = HasArgument("--s1api-product-lifecycle-smoke");
        if (!_enabled)
            return;

        _token = GetArgument("--s1api-product-lifecycle-token")?.Trim() ??
            Guid.NewGuid().ToString("N");
        _outputDirectory = GetArgument("--s1api-product-lifecycle-dir") ??
            Path.Combine(Path.GetTempPath(), "S1API.CustomProductLifecycleSmoke");
        _savePath = GetArgument("--s1api-product-lifecycle-save") ??
            Path.Combine(_outputDirectory, "SaveGame_CustomProductLifecycleSmoke");

        if (!int.TryParse(
                GetArgument("--s1api-product-lifecycle-timeout"),
                out int timeoutSeconds))
        {
            timeoutSeconds = 180;
        }

        _deadlineUtc = DateTime.UtcNow.AddSeconds(Math.Max(timeoutSeconds, 45));
        Directory.CreateDirectory(_outputDirectory);
        if (File.Exists(ResultPath))
            File.Delete(ResultPath);

        LoggerInstance.Msg(
            $"Lifecycle smoke enabled. Runtime={RuntimeName}|Token={_token}|" +
            $"Output={_outputDirectory}");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (!_enabled || _completed)
            return;

        LoggerInstance.Msg(
            $"Lifecycle smoke scene. Runtime={RuntimeName}|Scene={sceneName}|" +
            $"MainLoads={_mainLoadCount}");

        if (sceneName == "Menu" && !_started)
        {
            _started = true;
            MelonCoroutines.Start(StartDisposableSave());
        }
        else if (sceneName == "Main")
        {
            _mainLoadCount++;
            MelonCoroutines.Start(RunMainLoad());
        }
    }

    public override void OnUpdate()
    {
        if (!_enabled || _completed || DateTime.UtcNow < _deadlineUtc)
            return;

        Fail(
            $"Timed out. Runtime={RuntimeName}|MainLoads={_mainLoadCount}|" +
            $"PreLoadRestorations={_preLoadRestorationCount}|" +
            $"Scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    private IEnumerator StartDisposableSave()
    {
        yield return new WaitForSecondsRealtime(1f);

        try
        {
            PrepareDisposableSave();
            _saveInfo = CreateSaveInfo();
            S1LoadManager.Instance.StartGame(
                _saveInfo,
                allowLoadStacking: false,
                allowSaveBackup: false);
        }
        catch (Exception exception)
        {
            Fail("Failed to start the disposable save.", exception);
        }
    }

    private IEnumerator RunMainLoad()
    {
        while (S1LoadManager.Instance == null ||
               S1LoadManager.Instance.IsLoading ||
               !S1LoadManager.Instance.IsGameLoaded)
        {
            if (DateTime.UtcNow >= _deadlineUtc)
            {
                Fail("The native load did not reach a completed Main scene.");
                yield break;
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        try
        {
            if (_mainLoadCount == 1)
                RegisterOnce();

            ValidateCurrentScene();
        }
        catch (Exception exception)
        {
            Fail(
                $"Lifecycle validation failed on Main load {_mainLoadCount}.",
                exception);
            yield break;
        }

        if (_mainLoadCount < RequiredMainLoads)
        {
            BeginSaveAndReload();
            yield break;
        }

        Pass(
            $"Runtime={RuntimeName}|Scene=Main|Token={_token}|Id={ProductId}|" +
            $"MainLoads={_mainLoadCount}|PreLoadRestorations={_preLoadRestorationCount}|" +
            $"Registry=True|AllProducts=1|ProductNames=1|Price={SavedPrice:0.##}|" +
            "CreatedProducts=0|RestoredWithoutRegister=True");
    }

    private void RegisterOnce()
    {
        var productManager = S1NativeProduct.ProductManager.Instance ??
            throw new InvalidOperationException("ProductManager is unavailable.");
        var template = productManager.DefaultWeed ??
            throw new InvalidOperationException("ProductManager.DefaultWeed is unavailable.");

        _definition = UnityEngine.Object.Instantiate(template);
        _definition.name = ProductName;
        _definition.Name = ProductName;
        _definition.Description = "S1API custom product lifecycle smoke definition.";
        _definition.ID = ProductId;
        _definition.BasePrice = InitialPrice;
        _definition.MarketValue = InitialPrice;

        S1NativeProduct.ProductDefinition registered =
            CustomProductDefinitionRegistry.Register(
                "s1api.lifecycle-smoke",
                ProductId,
                ProductName,
                InitialPrice,
                _definition);
        Require(
            AreSameDefinition(registered, _definition),
            "Initial registration did not retain the supplied definition.");

        S1NativeProduct.ProductDefinition repeated =
            CustomProductDefinitionRegistry.Register(
                "S1API.LIFECYCLE-SMOKE",
                ProductId.ToUpperInvariant(),
                "Ignored Repeat Name",
                999f,
                _definition);
        Require(
            AreSameDefinition(repeated, _definition),
            "Repeated registration did not return the original definition.");

        GameLifecycle.OnPreLoad += RecordPreLoadRestoration;
        productManager.SetPrice(null, ProductId, SavedPrice);
    }

    private void RecordPreLoadRestoration()
    {
        try
        {
            if (TryGetRegisteredProduct(out S1NativeProduct.ProductDefinition? definition) &&
                AreSameDefinition(definition, _definition) &&
                CountAllProducts() == 1)
            {
                _preLoadRestorationCount++;
                LoggerInstance.Msg(
                    $"Lifecycle smoke pre-load restoration. Count={_preLoadRestorationCount}");
            }
        }
        catch (Exception exception)
        {
            LoggerInstance.Error(
                $"Lifecycle smoke pre-load check failed: {exception}");
        }
    }

    private void ValidateCurrentScene()
    {
        Require(
            TryGetRegisteredProduct(out S1NativeProduct.ProductDefinition? registered),
            "The custom product is missing from ScheduleOne.Registry.");
        Require(
            AreSameDefinition(registered, _definition),
            "The registry resolved a different product definition.");
        Require(
            CountAllProducts() == 1,
            "ProductManager.AllProducts did not contain exactly one custom entry.");
        Require(
            CountProductNames() == 1,
            "ProductManager.ProductNames did not contain exactly one custom name.");
        Require(
            CountCreatedProducts() == 0,
            "The generic custom definition entered vanilla createdProducts.");

        float actualPrice = S1NativeProduct.ProductManager.Instance.GetPrice(registered);
        Require(
            Math.Abs(actualPrice - SavedPrice) < 0.01f,
            $"Expected price {SavedPrice}, got {actualPrice}.");

        if (_mainLoadCount > 1)
        {
            Require(
                _preLoadRestorationCount >= _mainLoadCount - 1,
                "The registry was not restored during pre-load before save data resolved.");
        }
    }

    private void BeginSaveAndReload()
    {
        if (_savePending)
            throw new InvalidOperationException("A save/reload cycle is already pending.");

        _savePending = true;
        GameLifecycle.OnSaveComplete += OnSaveComplete;
        S1SaveManager.Instance.Save();
    }

    private void OnSaveComplete()
    {
        GameLifecycle.OnSaveComplete -= OnSaveComplete;
        MelonCoroutines.Start(ReloadAfterSave());
    }

    private IEnumerator ReloadAfterSave()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        _savePending = false;

        if (_saveInfo == null)
        {
            Fail("SaveInfo was unavailable for the repeat-load cycle.");
            yield break;
        }

        S1LoadManager.Instance.ExitToMenu(
            _saveInfo,
            null,
            preventLeaveLobby: true);
    }

    private bool TryGetRegisteredProduct(
        out S1NativeProduct.ProductDefinition? definition)
    {
        definition = null;
        if (S1Registry.Instance == null || !S1Registry.ItemExists(ProductId))
            return false;

        var item = S1Registry.GetItem(ProductId);
#if IL2CPPMELON
        definition = item?.TryCast<S1NativeProduct.ProductDefinition>();
#elif MONOMELON
        definition = item as S1NativeProduct.ProductDefinition;
#endif
        return definition != null;
    }

    private int CountAllProducts()
    {
        var allProducts = S1NativeProduct.ProductManager.Instance?.AllProducts;
        if (allProducts == null)
            return 0;

        int count = 0;
        for (int index = 0; index < allProducts.Count; index++)
        {
            var definition = allProducts[index];
            if (definition != null &&
                string.Equals(
                    definition.ID,
                    ProductId,
                    StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private int CountProductNames()
    {
        var names = S1NativeProduct.ProductManager.Instance?.ProductNames;
        if (names == null)
            return 0;

        int count = 0;
        for (int index = 0; index < names.Count; index++)
        {
            if (string.Equals(names[index], ProductName, StringComparison.Ordinal))
                count++;
        }

        return count;
    }

    private int CountCreatedProducts()
    {
        var productManager = S1NativeProduct.ProductManager.Instance ??
            throw new InvalidOperationException("ProductManager is unavailable.");

#if IL2CPPMELON
        var createdProducts = productManager.createdProducts;
#elif MONOMELON
        FieldInfo field = typeof(S1NativeProduct.ProductManager).GetField(
                "createdProducts",
                BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new MissingFieldException(
                typeof(S1NativeProduct.ProductManager).FullName,
                "createdProducts");
        var createdProducts =
            field.GetValue(productManager) as
                System.Collections.Generic.List<S1NativeProduct.ProductDefinition>;
#endif

        if (createdProducts == null)
            throw new InvalidOperationException("createdProducts is unavailable.");

        int count = 0;
        for (int index = 0; index < createdProducts.Count; index++)
        {
            var definition = createdProducts[index];
            if (definition != null &&
                string.Equals(
                    definition.ID,
                    ProductId,
                    StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static bool AreSameDefinition(
        S1NativeProduct.ProductDefinition? left,
        S1NativeProduct.ProductDefinition? right)
    {
        return ReferenceEquals(left, right) ||
               (left != null && right != null && left == right);
    }

    private S1SaveInfo CreateSaveInfo()
    {
        var metadata = new S1MetaData(
            (S1DateTimeData?)null,
            (S1DateTimeData?)null,
            Application.version,
            Application.version,
            playTutorial: false);
        return new S1SaveInfo(
            _savePath,
            -1,
            "S1API Custom Product Lifecycle Smoke",
            GetNow(),
            GetNow(),
            0f,
            Application.version,
            metadata);
    }

    private void PrepareDisposableSave()
    {
        string defaultSavePath =
            Path.Combine(Application.streamingAssetsPath, "DefaultSave");
        if (!Directory.Exists(defaultSavePath))
        {
            throw new DirectoryNotFoundException(
                $"Default save folder not found: {defaultSavePath}");
        }

        if (Directory.Exists(_savePath))
            Directory.Delete(_savePath, recursive: true);

        CopyDirectory(defaultSavePath, _savePath);
        File.WriteAllText(
            Path.Combine(_savePath, "Game.json"),
            "{\"DataType\":\"GameData\",\"DataVersion\":0,\"GameVersion\":\"" +
            Application.version +
            "\",\"OrganisationName\":\"S1API Custom Product Lifecycle Smoke\"," +
            "\"Seed\":9901,\"Settings\":{\"ConsoleEnabled\":false}}");
        File.WriteAllText(
            Path.Combine(_savePath, "Metadata.json"),
            "{\"DataType\":\"MetaData\",\"DataVersion\":0,\"GameVersion\":\"" +
            Application.version +
            "\",\"CreationDate\":null,\"LastPlayedDate\":null,\"CreationVersion\":\"" +
            Application.version + "\",\"LastSaveVersion\":\"" +
            Application.version + "\",\"PlayTutorial\":false}");
    }

    private static void CopyDirectory(string sourcePath, string targetPath)
    {
        string normalizedSource = Path.GetFullPath(sourcePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedTarget = Path.GetFullPath(targetPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        Directory.CreateDirectory(normalizedTarget);
        foreach (string directory in Directory.GetDirectories(
                     normalizedSource,
                     "*",
                     SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(
                MapChildPath(directory, normalizedSource, normalizedTarget));
        }

        foreach (string file in Directory.GetFiles(
                     normalizedSource,
                     "*.*",
                     SearchOption.AllDirectories))
        {
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            File.Copy(
                file,
                MapChildPath(file, normalizedSource, normalizedTarget),
                overwrite: true);
        }
    }

    private static string MapChildPath(
        string childPath,
        string sourceRoot,
        string targetRoot)
    {
        string normalizedChild = Path.GetFullPath(childPath);
        string prefix = sourceRoot + Path.DirectorySeparatorChar;
        if (!normalizedChild.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Path '{normalizedChild}' is outside source root '{sourceRoot}'.");
        }

        string relativePath = normalizedChild.Substring(prefix.Length);
        return Path.Combine(targetRoot, relativePath);
    }

    private static S1DateTime GetNow()
    {
#if IL2CPPMELON
        return S1DateTime.Now;
#elif MONOMELON
        return S1DateTime.Now;
#endif
    }

    private void Pass(string evidence)
    {
        if (_completed)
            return;

        _completed = true;
        GameLifecycle.OnPreLoad -= RecordPreLoadRestoration;
        GameLifecycle.OnSaveComplete -= OnSaveComplete;
        string result = $"PASS|{evidence}";
        File.WriteAllText(ResultPath, result);
        LoggerInstance.Msg(result);
    }

    private void Fail(string reason, Exception? exception = null)
    {
        if (_completed)
            return;

        _completed = true;
        GameLifecycle.OnPreLoad -= RecordPreLoadRestoration;
        GameLifecycle.OnSaveComplete -= OnSaveComplete;
        string detail = exception == null ? reason : $"{reason} {exception}";
        string result =
            $"FAIL|Runtime={RuntimeName}|MainLoads={_mainLoadCount}|" +
            $"PreLoadRestorations={_preLoadRestorationCount}|Reason={detail}";
        File.WriteAllText(ResultPath, result);
        LoggerInstance.Error(result);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static bool HasArgument(string argument)
    {
        return Environment.GetCommandLineArgs()
            .Any(value => string.Equals(
                value,
                argument,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetArgument(string argument)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(
                    arguments[index],
                    argument,
                    StringComparison.OrdinalIgnoreCase))
            {
                return arguments[index + 1];
            }
        }

        return null;
    }

    private static string RuntimeName
    {
        get
        {
#if IL2CPPMELON
            return "Il2CppMelon";
#elif MONOMELON
            return "MonoMelon";
#endif
        }
    }
}
