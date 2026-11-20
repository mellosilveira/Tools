using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.Host.Controllers;

/// <summary>
/// Exposes pre-bundled knee ligament experimental datasets for 1-click quickstart execution.
/// </summary>
[ApiController]
[Route("api/v1/samples")]
public class SamplesController : ControllerBase
{
    /// <summary>
    /// Metadata describing a bundled sample dataset ready for pipeline processing.
    /// </summary>
    public sealed record SampleDatasetInfo
    {
        /// <summary>
        /// Unique sample identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Human-readable dataset title.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Biomechanical context and specimen description.
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        /// Resolved path to the raw strain CSV file.
        /// </summary>
        public required string StrainFileLocation { get; init; }

        /// <summary>
        /// Resolved path to the raw stress CSV file.
        /// </summary>
        public required string StressFileLocation { get; init; }
    }

    /// <summary>
    /// Lists all pre-bundled knee ligament tensile test samples available on disk.
    /// </summary>
    /// <returns>List of <see cref="SampleDatasetInfo"/> wrapped in <see cref="Result{T}"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Result<List<SampleDatasetInfo>>), StatusCodes.Status200OK)]
    public IActionResult GetSamples()
    {
        string samplesDir = Path.Combine(AppContext.BaseDirectory, "SamplesData");
        string strainPath = Path.Combine(samplesDir, "LCA_02_07_21_L38_D11_strain.csv");
        string stressPath = Path.Combine(samplesDir, "LCA_02_07_21_L38_D11_stress.csv");

        List<SampleDatasetInfo> samples = [];
        if (System.IO.File.Exists(strainPath) && System.IO.File.Exists(stressPath))
        {
            samples.Add(new SampleDatasetInfo
            {
                Id = "lca-02-07-21-l38-d11",
                Name = "ACL Specimen LCA_02_07_21_L38_D11 (Ramp-and-Hold Relaxation)",
                Description = "Anterior Cruciate Ligament (ACL) uniaxial ramp-and-hold stress-relaxation tensile test dataset.",
                StrainFileLocation = strainPath,
                StressFileLocation = stressPath
            });
        }

        return Result.CreateSuccessOk(samples).BuildHttpResponse();
    }
}
