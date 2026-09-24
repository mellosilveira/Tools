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
    public static async Task<CurveSegment[]> LoadSegmentsForPrefixAsync(string prefix)
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

        if (!Directory.Exists(testDataFolder))
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

        List<CurveSegment> segments = [];
        string[] files = Directory.GetFiles(prefixFolder, "*.csv");
        Array.Sort(files); // Sort alphabetically to maintain chronological sequence (e.g., 01_Ramp.csv, 02_Relaxation.csv)

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            SegmentType segmentType = SegmentType.Unknown;
            foreach (SegmentType type in Enum.GetValues<SegmentType>())
            {
                if (type == SegmentType.Unknown) continue;
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

    private static async Task<CurveSegment?> LoadSegmentFromCsvAsync(string filePath, SegmentType segmentType)
    {
        FileInfo fileInfo = new(filePath);
        if (fileInfo.Length == 0) return null;

        await using FileStream stream = File.OpenRead(filePath);
        await using CsvStreamReader reader = new(stream, skipInvalidLines: true);

        List<double> timePoints = [];
        List<double> strainPoints = [];
        List<double> stressPoints = [];

        await foreach (double[] row in reader.ReadAllRowsAsync())
        {
            if (row.Length >= 3)
            {
                timePoints.Add(row[0]);
                strainPoints.Add(row[1]);
                stressPoints.Add(row[2]);
            }
        }

        if (timePoints.Count == 0) return null;

        return new CurveSegment
        {
            Type = segmentType,
            TimePoints = [.. timePoints],
            ExperimentalStrain = [.. strainPoints],
            ExperimentalStress = [.. stressPoints]
        };
    }
}
