using System.Globalization;

namespace S1APIDocumentationCoverage;

internal static class Program
{
    private const double DefaultMinimumCoverage = 80d;
    private const int MissingItemDisplayLimit = 25;

    private static readonly HashSet<string> PublicApiItemTypes = new(StringComparer.Ordinal)
    {
        "Class",
        "Struct",
        "Interface",
        "Enum",
        "Delegate",
        "Constructor",
        "Method",
        "Property",
        "Field",
        "Event",
        "Operator"
    };

    public static int Main(string[] args)
    {
        if (!TryParseArguments(args, out Options? options, out string? error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return 2;
        }

        string apiDirectory = Path.GetFullPath(options.ApiDirectory);
        if (!Directory.Exists(apiDirectory))
        {
            Console.Error.WriteLine($"DocFX API directory does not exist: {apiDirectory}");
            return 2;
        }

        CoverageResult coverage = Analyze(apiDirectory);
        if (coverage.Total == 0)
        {
            Console.Error.WriteLine($"No DocFX managed-reference API items were found in {apiDirectory}.");
            return 2;
        }

        Console.WriteLine(
            $"Public API documentation coverage: {coverage.Percentage:F2}% " +
            $"({coverage.Documented:N0}/{coverage.Total:N0}, minimum {options.MinimumCoverage:F2}%).");

        if (coverage.Percentage + 0.000001d >= options.MinimumCoverage)
            return 0;

        Console.Error.WriteLine(
            $"Documentation coverage is below the required {options.MinimumCoverage:F2}% threshold.");
        foreach (string uid in coverage.Missing.Take(MissingItemDisplayLimit))
            Console.Error.WriteLine($"  - {uid}");

        if (coverage.Missing.Count > MissingItemDisplayLimit)
        {
            Console.Error.WriteLine(
                $"  ... and {coverage.Missing.Count - MissingItemDisplayLimit:N0} more undocumented items.");
        }

        return 1;
    }

    private static CoverageResult Analyze(string apiDirectory)
    {
        int total = 0;
        int documented = 0;
        var missing = new List<string>();

        foreach (string path in Directory.EnumerateFiles(apiDirectory, "*.yml", SearchOption.AllDirectories))
        {
            AnalyzeFile(path, ref total, ref documented, missing);
        }

        return new CoverageResult(total, documented, missing);
    }

    private static void AnalyzeFile(
        string path,
        ref int total,
        ref int documented,
        List<string> missing)
    {
        using var reader = new StreamReader(path);
        if (!string.Equals(reader.ReadLine(), "### YamlMime:ManagedReference", StringComparison.Ordinal))
            return;

        ApiItem? current = null;
        bool readingItems = false;
        while (reader.ReadLine() is { } line)
        {
            if (!readingItems)
            {
                readingItems = string.Equals(line, "items:", StringComparison.Ordinal);
                continue;
            }

            if (string.Equals(line, "references:", StringComparison.Ordinal))
            {
                AddResult(current, ref total, ref documented, missing);
                break;
            }

            const string uidPrefix = "- uid: ";
            if (line.StartsWith(uidPrefix, StringComparison.Ordinal))
            {
                AddResult(current, ref total, ref documented, missing);
                current = new ApiItem(line[uidPrefix.Length..].Trim());
                continue;
            }

            if (current == null)
                continue;

            const string typePrefix = "  type: ";
            if (line.StartsWith(typePrefix, StringComparison.Ordinal))
            {
                current.IsPublicApiItem = PublicApiItemTypes.Contains(line[typePrefix.Length..].Trim());
            }
            else if (line.StartsWith("  summary:", StringComparison.Ordinal))
            {
                current.HasSummary = true;
            }
        }

        AddResult(current, ref total, ref documented, missing);
    }

    private static void AddResult(
        ApiItem? item,
        ref int total,
        ref int documented,
        List<string> missing)
    {
        if (item == null || item.Counted || !item.IsPublicApiItem)
            return;

        item.Counted = true;
        total++;
        if (item.HasSummary)
            documented++;
        else
            missing.Add(item.Uid);
    }

    private static bool TryParseArguments(string[] args, out Options options, out string? error)
    {
        string apiDirectory = Path.Combine("S1API", "api");
        double minimumCoverage = DefaultMinimumCoverage;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            if (argument is "-h" or "--help")
            {
                options = new Options(apiDirectory, minimumCoverage);
                error = null;
                PrintUsage();
                Environment.Exit(0);
            }

            if (index + 1 >= args.Length)
            {
                options = new Options(apiDirectory, minimumCoverage);
                error = $"Missing value for {argument}.";
                return false;
            }

            string value = args[++index];
            switch (argument)
            {
                case "--api-directory":
                    apiDirectory = value;
                    break;
                case "--minimum":
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out minimumCoverage)
                        || minimumCoverage is < 0d or > 100d)
                    {
                        options = new Options(apiDirectory, DefaultMinimumCoverage);
                        error = $"Invalid documentation coverage threshold: {value}.";
                        return false;
                    }

                    break;
                default:
                    options = new Options(apiDirectory, minimumCoverage);
                    error = $"Unknown argument: {argument}.";
                    return false;
            }
        }

        options = new Options(apiDirectory, minimumCoverage);
        error = null;
        return true;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "Usage: dotnet run --project tools/S1APIDocumentationCoverage -- " +
            "[--api-directory S1API/api] [--minimum 80]");
    }

    private sealed record Options(string ApiDirectory, double MinimumCoverage);

    private sealed record CoverageResult(int Total, int Documented, IReadOnlyList<string> Missing)
    {
        internal double Percentage =>
            Total == 0 ? 0d : Documented * 100d / Total;
    }

    private sealed class ApiItem(string uid)
    {
        internal string Uid { get; } = uid;
        internal bool IsPublicApiItem { get; set; }
        internal bool HasSummary { get; set; }
        internal bool Counted { get; set; }
    }
}
