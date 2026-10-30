using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

/// <summary>
/// Helper for loading acceptance test data CSV files and converting them to CurveSegment arrays.
/// </summary>
public static class AcceptanceTestHelpers
{
    /// <summary>
    /// Loads CurveSegment array for a given curve fitter step prefix by finding all CSV files in TestData matching [PREFIX]_[SEGMENT_TYPE].csv.
    /// Expected columns: Time, ExperimentalStrain, ExperimentalStress
    /// </summary>
    /// <param name="prefix">Prefix folder name under TestData.</param>
    /// <param name="useSharedFiles">Whether to load shared test data files first.</param>
    /// <returns>An array of loaded curve segments.</returns>
    public static async Task<CurveSegment[]> LoadSegmentsForPrefixAsync(string prefix, bool useSharedFiles = true)
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await LoadSegmentGroupsForPrefixAsync(prefix, useSharedFiles);
        return groups.Count > 0 ? groups[0].Segments : [];
    }

    /// <summary>
    /// Loads groups of <see cref="CurveSegment"/> arrays for a given curve fitter step prefix and optional shared test data.
    /// If <paramref name="useSharedFiles"/> is <c>true</c>, all groups from the shared folder ("Shared") under TestData
    /// are loaded first. Then, all groups from the prefix-specific folder under TestData are loaded.
    /// </summary>
    /// <param name="prefix">Prefix folder name under TestData.</param>
    /// <param name="useSharedFiles">Whether to load shared test data files first.</param>
    /// <returns>A list of tuples containing the group name and its loaded curve segments.</returns>
    public static async Task<IReadOnlyList<(string GroupName, CurveSegment[] Segments)>> LoadSegmentGroupsForPrefixAsync(string prefix, bool useSharedFiles = true)
    {
        string? testDataFolder = ResolveTestDataDirectory();
        if (testDataFolder == null)
        {
            return [];
        }

        List<(string GroupName, CurveSegment[] Segments)> groups = [];

        // 1. First, search for shared files across all acceptance tests if enabled
        if (useSharedFiles)
        {
            string sharedFolder = Path.Combine(testDataFolder, "Shared");
            if (Directory.Exists(sharedFolder))
            {
                IReadOnlyList<(string GroupName, CurveSegment[] Segments)> sharedGroups = await LoadDirectoryGroupsAsync(sharedFolder, "Shared");
                groups.AddRange(sharedGroups);
            }
        }

        // 2. Then, fetch prefix-specific files
        string prefixFolder = Path.Combine(testDataFolder, prefix);
        if (!Directory.Exists(prefixFolder))
        {
            prefixFolder = Path.Combine(testDataFolder, $"{prefix}CurveFitterStep");
        }

        if (Directory.Exists(prefixFolder))
        {
            IReadOnlyList<(string GroupName, CurveSegment[] Segments)> prefixGroups = await LoadDirectoryGroupsAsync(prefixFolder, prefix);
            groups.AddRange(prefixGroups);
        }

        return groups;
    }

    private static async Task<IReadOnlyList<(string GroupName, CurveSegment[] Segments)>> LoadDirectoryGroupsAsync(string folderPath, string defaultGroupName)
    {
        List<(string GroupName, CurveSegment[] Segments)> groups = [];

        string[] subDirectories = Directory.GetDirectories(folderPath);
        if (subDirectories.Length > 0)
        {
            Array.Sort(subDirectories);
            foreach (string subDir in subDirectories)
            {
                string groupName = Path.GetFileName(subDir);
                CurveSegment[] segments = await LoadSegmentsFromDirectoryAsync(subDir);
                if (segments.Length > 0)
                {
                    groups.Add((groupName, segments));
                }
            }
        }

        CurveSegment[] rootSegments = await LoadSegmentsFromDirectoryAsync(folderPath);
        if (rootSegments.Length > 0)
        {
            groups.Add((defaultGroupName, rootSegments));
        }

        return groups;
    }

    /// <summary>
    /// Loads all <see cref="CurveSegment"/> instances from CSV files in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The absolute path to the directory containing segment CSV files.</param>
    /// <returns>An array of loaded curve segments sorted in alphabetical file order.</returns>
    public static async Task<CurveSegment[]> LoadSegmentsFromDirectoryAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        List<CurveSegment> segments = [];
        string[] files = Directory.GetFiles(directoryPath, "*.csv");
        Array.Sort(files);

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            SegmentType segmentType = SegmentType.Unknown;
            foreach (SegmentType type in Enum.GetValues<SegmentType>())
            {
                if (type == SegmentType.Unknown)
                {
                    continue;
                }

                if (fileName.Contains(type.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    segmentType = type;
                    break;
                }
            }

            if (segmentType != SegmentType.Unknown)
            {
                CurveSegment? segment = await LoadSegmentFromCsvAsync(filePath, segmentType);
                if (segment != null)
                {
                    segments.Add(segment);
                }
            }
        }

        return [.. segments];
    }

    private static string? ResolveTestDataDirectory()
    {
        string? testDataFolder = Environment.GetEnvironmentVariable("TEST_DATA_DIRECTORY");

        if (string.IsNullOrWhiteSpace(testDataFolder) || !Directory.Exists(testDataFolder))
        {
            testDataFolder = Path.Combine(AppContext.BaseDirectory, "MechanicalOfMaterials.Optimizations", "CurveFitterSteps", "TestData");

            if (!Directory.Exists(testDataFolder))
            {
                testDataFolder = Path.Combine(Directory.GetCurrentDirectory(), "MechanicalOfMaterials.Optimizations", "CurveFitterSteps", "TestData");
            }
        }

        return Directory.Exists(testDataFolder) ? testDataFolder : null;
    }

    /// <summary>
    /// Loads a single <see cref="CurveSegment"/> from a specified CSV file.
    /// </summary>
    /// <param name="filePath">Absolute or relative file path to the CSV file.</param>
    /// <param name="segmentType">The segment type of this curve data.</param>
    /// <returns>The populated curve segment, or null if empty.</returns>
    public static async Task<CurveSegment?> LoadSegmentFromCsvAsync(string filePath, SegmentType segmentType)
    {
        FileInfo fileInfo = new(filePath);
        if (fileInfo.Length == 0)
        {
            return null;
        }

        byte delimiter = (byte)';';
        using (StreamReader sr = new(filePath))
        {
            string? firstLine = await sr.ReadLineAsync();
            if (firstLine != null && !firstLine.Contains(';') && firstLine.Contains(','))
            {
                delimiter = (byte)',';
            }
        }

        await using FileStream stream = File.OpenRead(filePath);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream, delimiter: delimiter, leaveOpen: false, skipInvalidLines: true);

        List<double> timePoints = [];
        List<double> strainPoints = [];
        List<double> stressPoints = [];

        await foreach (ReadOnlyMemory<double> row in reader.ReadAllRowsAsync())
        {
            ReadOnlySpan<double> span = row.Span;
            if (span.Length >= 3)
            {
                timePoints.Add(span[0]);
                strainPoints.Add(span[1]);
                stressPoints.Add(span[2]);
            }
        }

        if (timePoints.Count == 0)
        {
            return null;
        }

        return new CurveSegment
        {
            Type = segmentType,
            TimePoints = [.. timePoints],
            ExperimentalStrain = [.. strainPoints],
            ExperimentalStress = [.. stressPoints]
        };
    }

    /// <summary>
    /// Computes the coefficient of determination (R² = 1 - SSR/SST) comparing actual values with predicted values.
    /// </summary>
    /// <param name="actual">Actual observed values.</param>
    /// <param name="predicted">Predicted or fitted model values.</param>
    /// <returns>The calculated R² coefficient.</returns>
    public static double CalculateRSquared(ReadOnlySpan<double> actual, ReadOnlySpan<double> predicted)
    {
        if (actual.Length != predicted.Length || actual.Length == 0)
        {
            throw new ArgumentException("Actual and predicted sequences must have identical non-zero length.");
        }

        double sumActual = 0.0;
        for (int i = 0; i < actual.Length; i++)
        {
            sumActual += actual[i];
        }

        double meanActual = sumActual / actual.Length;
        double sst = 0.0;
        double ssr = 0.0;

        for (int i = 0; i < actual.Length; i++)
        {
            double diffMean = actual[i] - meanActual;
            sst += diffMean * diffMean;

            double diffPred = actual[i] - predicted[i];
            ssr += diffPred * diffPred;
        }

        return sst > 1e-15 ? 1.0 - (ssr / sst) : 1.0;
    }
}

