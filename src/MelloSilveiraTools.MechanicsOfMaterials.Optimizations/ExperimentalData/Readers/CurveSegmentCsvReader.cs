using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using System.Globalization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Readers;

/// <summary>
/// Helper service that reads and parses a segmented experimental CSV file (<c>experimental_data.csv</c>)
/// directly from <see cref="IFileStorageService"/>, grouping data points sequentially into <see cref="CurveSegment"/> instances.
/// </summary>
public static class CurveSegmentCsvReader
{
    /// <summary>
    /// Asynchronously streams and parses the segmented CSV file from storage, grouping points by physical deformation phase.
    /// </summary>
    /// <param name="fileStorageService">Storage service for opening the file stream.</param>
    /// <param name="fileLocation">Location of the segmented experimental CSV in storage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of parsed <see cref="CurveSegment"/> objects.</returns>
    public static async Task<IReadOnlyList<CurveSegment>> ReadSegmentsAsync(
        IFileStorageService fileStorageService,
        string fileLocation,
        CancellationToken cancellationToken = default)
    {
        Stream stream = await fileStorageService.OpenReadAsync(fileLocation, cancellationToken).ConfigureAwait(false);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream, leaveOpen: false, cancellationToken: cancellationToken).ConfigureAwait(false);

        List<CurveSegment> segments = [];
        SegmentType currentType = SegmentType.Unknown;
        List<double> timePoints = [];
        List<double> strainPoints = [];
        List<double> stressPoints = [];

        while (!cancellationToken.IsCancellationRequested)
        {
            ReadOnlyMemory<double> row = await reader.ReadNextRowAsync(cancellationToken).ConfigureAwait(false);
            if (row.Length < 4)
            {
                break;
            }

            ReadOnlySpan<double> span = row.Span;
            double time = span[0];
            double strain = span[1];
            double stress = span[2];
            SegmentType segmentType = (SegmentType)(int)span[3];

            if (segmentType != currentType)
            {
                if (timePoints.Count > 0 && currentType != SegmentType.Unknown)
                {
                    segments.Add(new CurveSegment
                    {
                        Type = currentType,
                        TimePoints = [.. timePoints],
                        ExperimentalStrain = [.. strainPoints],
                        ExperimentalStress = [.. stressPoints]
                    });

                    timePoints.Clear();
                    strainPoints.Clear();
                    stressPoints.Clear();
                }

                currentType = segmentType;
            }

            if (segmentType != SegmentType.Unknown)
            {
                timePoints.Add(time);
                strainPoints.Add(strain);
                stressPoints.Add(stress);
            }
        }

        if (timePoints.Count > 0 && currentType != SegmentType.Unknown)
        {
            segments.Add(new CurveSegment
            {
                Type = currentType,
                TimePoints = [.. timePoints],
                ExperimentalStrain = [.. strainPoints],
                ExperimentalStress = [.. stressPoints]
            });
        }

        return segments;
    }
}
