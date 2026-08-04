using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting.MechanicalModels;

public abstract class QuasiLinearModelCurveFitter(
    ICurveFitter mathematicalEngine,
    IOptimizationMapper optimizationMapper) : ICurveFitter
{
    protected readonly ICurveFitter MathematicalEngine = mathematicalEngine;

    public CurveFitResult Fit(CurveFitInput input)
    {
        var rampSegment = input.Segments.FirstOrDefault(s => s.Type == SegmentType.Ramp);
        var relaxationSegment = input.Segments.FirstOrDefault(s => s.Type == SegmentType.Relaxation);

        // Se não houver rampa (ex: step-strain ideal), ajusta apenas a relaxação
        if (rampSegment == null && relaxationSegment != null)
        {
            return FitRelaxationPhase(input with { Segments = [relaxationSegment] });
        }

        if (rampSegment == null)
            return MathematicalEngine.Fit(input); // Fallback genérico se não tiver nenhum dos dois

        // 1. Ajuste do trecho de Rampa (Isolando o segmento no Input)
        var rampInput = input with { Segments = [rampSegment] };
        var elasticResult = MathematicalEngine.Fit(rampInput);

        if (!elasticResult.IsSuccessful)
            return elasticResult;

        // 2. Atualiza os parâmetros do modelo com A e B otimizados
        var updatedInitialInput = input.InitialInput with { ConstitutiveParameters = optimizationMapper.MapToConstitutiveParameters(elasticResult.OptimizedParameters) };

        // 3. Prepara o input apenas com a Relaxação
        var relaxationInput = input with
        {
            InitialInput = updatedInitialInput,
            Segments = relaxationSegment != null ? [relaxationSegment] : []
        };

        // 4. Ajuste do trecho de Relaxação
        return relaxationSegment != null
            ? FitRelaxationPhase(relaxationInput)
            : elasticResult;
    }

    protected abstract CurveFitResult FitRelaxationPhase(CurveFitInput relaxationInput);
}

public class FungModelCurveFitter(
    ICurveFitter mathematicalEngine,
    IOptimizationMapper optimizationMapper) 
    : QuasiLinearModelCurveFitter(mathematicalEngine, optimizationMapper)
{
    protected override CurveFitResult FitRelaxationPhase(CurveFitInput relaxationInput)
    {
        var relaxationOptions = relaxationInput.Options with
        {
            LowerBounds = [0.001, 0.001, 0.001], // Ex: limites para c, tau1, tau2
            UpperBounds = null
        };

        var customizedInput = relaxationInput with { Options = relaxationOptions };

        return MathematicalEngine.Fit(customizedInput);
    }
}

public class SimplifiedFungCurveFitter(
    ICurveFitter mathematicalEngine,
    IOptimizationMapper optimizationMapper) 
    : QuasiLinearModelCurveFitter(mathematicalEngine, optimizationMapper)
{
    protected override CurveFitResult FitRelaxationPhase(CurveFitInput relaxationInput)
    {
        var relaxationOptions = relaxationInput.Options with
        {
            // Ex: Limites para G1, G2, G3, tau1, tau2, tau3
            LowerBounds = [0.001, 0.001, 0.001, 0.001, 0.001, 0.001],
            UpperBounds = null
        };

        // Regra física strongly-typed graças à nova arquitetura
        Func<GenericMechanicalModelInput, double> pronyPenalties = (currentInput) =>
        {
            if (currentInput.ConstitutiveParameters is not SimplifiedFungConstitutiveParameters pronyParams)
                return 0;

            double penalty = 0;

            // Exemplo: Garantir que a soma dos coeficientes G não fuja de 1
            // double sumG = pronyParams.G1 + pronyParams.G2 + pronyParams.G3;
            // if (Math.Abs(sumG - 1.0) > 1e-5)
            //     penalty += 1e10 * Math.Pow(sumG - 1.0, 2);

            return penalty;
        };

        var customizedInput = relaxationInput with
        {
            Options = relaxationOptions,
            EvaluateConstraintsAndPenalties = pronyPenalties
        };

        return MathematicalEngine.Fit(customizedInput);
    }
}