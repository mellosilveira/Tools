using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting;

public class AlglibCurveFitter(
    IMechanicalModelCalculatorFacade calculatorFacade,
    IOptimizationMapper mapper)
    : GenericCurveFittingBase(calculatorFacade, mapper)
{
    public override CurveFitResult Fit(CurveFitInput input)
    {
        try
        {
            // Extrai as estimativas iniciais do modelo via Mapper
            var initialGuesses = Mapper.ExtractOptimizableParameters(input.InitialInput.ConstitutiveParameters);
            double[] x = (double[])initialGuesses.Clone();

            // 1. Inicializa o estado do solver BLEIC
            alglib.minbleiccreate(x, out alglib.minbleicstate state);

            // 2. Injeta os limites de contorno (Bounds) nativamente, se existirem.
            // Isso impede que o solver sequer tente avaliar constantes negativas, acelerando a busca.
            if (input.Options.LowerBounds != null && input.Options.UpperBounds != null)
            {
                alglib.minbleicsetbc(state, input.Options.LowerBounds, input.Options.UpperBounds);
            }

            // 3. Define as condições de parada
            alglib.minbleicsetcond(state, input.Options.Tolerance, input.Options.Tolerance, input.Options.Tolerance, input.Options.MaxIterations);

            // 4. Executa a otimização
            // O ALGLIB exige o cálculo simultâneo do Erro (func) e da Derivada (grad) a cada passo
            alglib.minbleicoptimize(state, (double[] currentParams, ref double func, double[] grad, object obj) =>
            {
                // Calcula o erro chamando seu Facade + Penalidades (Estágio 2 embutido)
                func = CalculateObjectiveFunction(input, currentParams, applyConstraints: true);

                // Calcula a derivada numérica (Diferenças Finitas) via classe base
                var computedGradient = CalculateNumericalGradient(input, currentParams, applyConstraints: true);

                // Copia a derivada calculada para o array exigido pelo ALGLIB
                Array.Copy(computedGradient, grad, currentParams.Length);
            }, null, null);

            // 5. Coleta os resultados finais
            alglib.minbleicresults(state, out x, out alglib.minbleicreport rep);

            bool success = rep.terminationtype > 0;
            return new CurveFitResult(
                success,
                x,
                state.f,
                $"Sucesso ALGLIB. Código de terminação: {rep.terminationtype}. Iterações: {rep.iterationscount}"
            );
        }
        catch (Exception ex)
        {
            return new CurveFitResult(false, [], double.MaxValue, ex.Message);
        }
    }
}