using MelloSilveiraTools.Core.Managers.File;
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
    /// <returns>An array of loaded curve segments.</returns>
    public static async Task<CurveSegment[]> LoadSegmentsForPrefixAsync(string prefix)
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await LoadSegmentGroupsForPrefixAsync(prefix);
        return groups.Count > 0 ? groups[0].Segments : [];
    }

    /// <summary>
    /// Loads groups of <see cref="CurveSegment"/> arrays for a given curve fitter step prefix.
    /// If the prefix folder contains subdirectories, each subdirectory is loaded as an independent group (named after the subdirectory).
    /// If the prefix folder contains no subdirectories, the root folder is loaded as a single group (named after the prefix).
    /// </summary>
    /// <param name="prefix">Prefix folder name under TestData.</param>
    /// <returns>A list of tuples containing the group name and its loaded curve segments.</returns>
    public static async Task<IReadOnlyList<(string GroupName, CurveSegment[] Segments)>> LoadSegmentGroupsForPrefixAsync(string prefix)
    {
        string? testDataFolder = ResolveTestDataDirectory();
        if (testDataFolder == null)
        {
            return [];
        }

        string prefixFolder = Path.Combine(testDataFolder, prefix);
        if (!Directory.Exists(prefixFolder))
        {
            prefixFolder = Path.Combine(testDataFolder, $"{prefix}CurveFitterStep");
        }

        if (!Directory.Exists(prefixFolder))
        {
            return [];
        }

        string[] subDirectories = Directory.GetDirectories(prefixFolder);
        if (subDirectories.Length > 0)
        {
            Array.Sort(subDirectories);
            List<(string GroupName, CurveSegment[] Segments)> groups = [];

            foreach (string subDir in subDirectories)
            {
                string groupName = Path.GetFileName(subDir);
                CurveSegment[] segments = await LoadSegmentsFromDirectoryAsync(subDir);
                if (segments.Length > 0)
                {
                    groups.Add((groupName, segments));
                }
            }

            return groups;
        }

        CurveSegment[] rootSegments = await LoadSegmentsFromDirectoryAsync(prefixFolder);
        return rootSegments.Length > 0 ? [(prefix, rootSegments)] : [];
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

        await using FileStream stream = File.OpenRead(filePath);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream, delimiter: (byte)';', leaveOpen: false, skipInvalidLines: true);

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

