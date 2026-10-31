using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

/// <summary>
/// Acceptance tests for <see cref="SchaperyRelaxationOnlyCurveFitterStep"/> validating end-to-end curve fitting
/// with real experimental ligament multi-strain data and enforcing strict biomechanical physical validity.
/// </summary>
public class SchaperyRelaxationOnlyCurveFitterStepAcceptanceTests : CurveFitterAcceptanceTestBase<SchaperyRelaxationOnlyCurveFitterStep>
{
    /// <inheritdoc/>
    protected override string Prefix => "SchaperyRelaxationOnly";

    /// <inheritdoc/>
    protected override void AssertPhysicalConstraints(string groupName, IReadOnlyList<CurveSegment> segments, IReadOnlyList<MechanicalModelCurveFitOutput> outputs)
    {
        Assert.True(segments.Count >= 4, $"Group '{groupName}': Expected at least 4 relaxation segments for multi-strain Schapery characterization (3%, 4%, 5%, 6%).");

        // Stage 1: Anchor segment verification (the first yielded output corresponds to the lowest strain: 3%)
        MechanicalModelCurveFitOutput anchorOutput = outputs[0];
        Assert.NotNull(anchorOutput.ConstitutiveParameters);
        SchaperyConstitutiveParameters anchorParameters = Assert.IsType<SchaperyConstitutiveParameters>(anchorOutput.ConstitutiveParameters);

        // Physical constraint 1: Equilibrium modulus Ge must be strictly positive and physiologically reasonable for knee ligaments (MPa)
        Assert.True(anchorParameters.Ge > 0.0, $"Group '{groupName}': Equilibrium modulus Ge ({anchorParameters.Ge}) must be strictly positive.");
        Assert.InRange(anchorParameters.Ge, 0.5, 50.0);

        // Physical constraint 2: Transient relaxation function must be PowerLaw with positive amplitude g1 and positive rate n
        Assert.NotNull(anchorParameters.TransientRelaxationFunction);
        double g1 = anchorParameters.TransientRelaxationFunction.Coefficients[0];
        double n = anchorParameters.TransientRelaxationFunction.Coefficients[1];
        Assert.True(g1 > 0.0, $"Group '{groupName}': Transient relaxation amplitude g1 ({g1}) must be strictly positive (contrasting with legacy c < 0 in Populate_database.sql).");
        Assert.True(n > 0.0, $"Group '{groupName}': Relaxation rate parameter n ({n}) must be strictly positive (contrasting with legacy n < 0 in Populate_database.sql).");
        Assert.True(n < 1.0, $"Group '{groupName}': Relaxation rate parameter n ({n}) must be sub-unitary for standard physical viscoelastic decay.");

        // Accuracy of anchor fit: R² must exceed MinimumAcceptableRSquared
        Assert.True(anchorOutput.RSquared >= MinimumAcceptableRSquared, $"Group '{groupName}': Anchor segment R² ({anchorOutput.RSquared}) must be >= {MinimumAcceptableRSquared}.");

        // Stage 2: Remaining segments (outputs 1 to segments.Count - 1)
        for (int i = 1; i < segments.Count; i++)
        {
            MechanicalModelCurveFitOutput segmentOutput = outputs[i];
            SchaperyConstitutiveParameters segmentParams = Assert.IsType<SchaperyConstitutiveParameters>(segmentOutput.ConstitutiveParameters);

            Assert.Equal(anchorParameters.Ge, segmentParams.Ge);
            Assert.NotNull(segmentParams.He);
            Assert.NotNull(segmentParams.H2);

            double strainValue = segments[i].ExperimentalStrain[0];
            double heVal = segmentParams.He.Calculate(strainValue);
            double h2Val = segmentParams.H2.Calculate(strainValue);

            Assert.True(heVal > 0.0, $"Group '{groupName}': Discrete Helmholtz He at strain {strainValue} ({heVal}) must be strictly positive.");
            Assert.True(h2Val > 0.0, $"Group '{groupName}': Discrete Helmholtz H2 at strain {strainValue} ({h2Val}) must be strictly positive.");
            Assert.True(segmentOutput.RSquared >= MinimumAcceptableRSquared, $"Group '{groupName}': Segment at strain {strainValue} R² ({segmentOutput.RSquared}) must be >= {MinimumAcceptableRSquared}.");
        }

        // Stage 3: Consolidated final output (the last output in the batch)
        MechanicalModelCurveFitOutput finalOutput = outputs[segments.Count];
        SchaperyConstitutiveParameters finalParams = Assert.IsType<SchaperyConstitutiveParameters>(finalOutput.ConstitutiveParameters);
        Assert.NotNull(finalParams.He);
        Assert.NotNull(finalParams.H2);

        // Continuous Helmholtz functions must evaluate to strictly positive values across the entire physical strain range
        double[] testStrains = [0.03, 0.04, 0.05, 0.06];
        foreach (double eps in testStrains)
        {
            double continuousHe = finalParams.He.Calculate(eps);
            double continuousH2 = finalParams.H2.Calculate(eps);

            Assert.False(double.IsNaN(continuousHe), $"Group '{groupName}': Continuous Helmholtz He({eps}) is NaN. Type: {finalParams.He.FunctionType}, Coeffs: [{string.Join(", ", finalParams.He.Coefficients)}]");
            Assert.False(double.IsNaN(continuousH2), $"Group '{groupName}': Continuous Helmholtz H2({eps}) is NaN. Type: {finalParams.H2.FunctionType}, Coeffs: [{string.Join(", ", finalParams.H2.Coefficients)}]");
            Assert.True(continuousHe > 0.0, $"Group '{groupName}': Continuous Helmholtz He({eps}) = {continuousHe} must be strictly positive. Type: {finalParams.He.FunctionType}, Coeffs: [{string.Join(", ", finalParams.He.Coefficients)}]");
            Assert.True(continuousH2 > 0.0, $"Group '{groupName}': Continuous Helmholtz H2({eps}) = {continuousH2} must be strictly positive. Type: {finalParams.H2.FunctionType}, Coeffs: [{string.Join(", ", finalParams.H2.Coefficients)}]");
        }

        // Stage 4: Continuous Helmholtz functions R² validation (must be >= MinimumAcceptableRSquared) using AcceptanceTestHelpers.CalculateRSquared
        double[] discreteHe = new double[segments.Count];
        double[] discreteH2 = new double[segments.Count];
        discreteHe[0] = 1.0;
        discreteH2[0] = 1.0;
        for (int i = 1; i < segments.Count; i++)
        {
            SchaperyConstitutiveParameters segParams = Assert.IsType<SchaperyConstitutiveParameters>(outputs[i].ConstitutiveParameters);
            Assert.NotNull(segParams.He);
            Assert.NotNull(segParams.H2);
            discreteHe[i] = segParams.He.Calculate(segments[i].ExperimentalStrain[0]);
            discreteH2[i] = segParams.H2.Calculate(segments[i].ExperimentalStrain[0]);
        }

        double[] calcHe = segments.Select(seg => finalParams.He.Calculate(seg.ExperimentalStrain[0])).ToArray();
        double r2He = AcceptanceTestHelpers.CalculateRSquared(discreteHe, calcHe);
        Assert.True(r2He >= MinimumAcceptableRSquared,
            $"Group '{groupName}': Continuous Helmholtz He R² ({r2He:F4}) must be >= {MinimumAcceptableRSquared}. " +
            $"Type: {finalParams.He.FunctionType}, " +
            $"Coeffs: [{string.Join(", ", finalParams.He.Coefficients)}], " +
            $"Strains: [{string.Join(", ", segments.Select(s => s.ExperimentalStrain[0]))}], " +
            $"Discrete: [{string.Join(", ", discreteHe)}], " +
            $"Calc: [{string.Join(", ", calcHe)}], " +
            $"FinalRSquared: {finalOutput.RSquared}");

        double[] calcH2 = segments.Select(seg => finalParams.H2.Calculate(seg.ExperimentalStrain[0])).ToArray();
        double r2H2 = AcceptanceTestHelpers.CalculateRSquared(discreteH2, calcH2);
        Assert.True(r2H2 >= MinimumAcceptableRSquared,
            $"Group '{groupName}': Continuous Helmholtz H2 R² ({r2H2:F4}) must be >= {MinimumAcceptableRSquared}. " +
            $"Type: {finalParams.H2.FunctionType}, " +
            $"Coeffs: [{string.Join(", ", finalParams.H2.Coefficients)}], " +
            $"Strains: [{string.Join(", ", segments.Select(s => s.ExperimentalStrain[0]))}], " +
            $"Discrete: [{string.Join(", ", discreteH2)}], " +
            $"Calc: [{string.Join(", ", calcH2)}]");

        // Global composite precision across multi-stage fits must exceed MinimumAcceptableRSquared
        Assert.True(finalOutput.RSquared >= MinimumAcceptableRSquared, $"Group '{groupName}': Consolidated composite R² ({finalOutput.RSquared}) must be >= {MinimumAcceptableRSquared}.");
    }
}
