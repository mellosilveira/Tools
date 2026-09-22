using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public class AlglibCurveFitter : CurveFitterBase
{
    public override CurveFitOutput Fit(CurveFitInput input)
    {
        // Extrai as estimativas iniciais do modelo via input
        double[] x = (double[])input.InitialParameters.Clone();

        // 1. Inicializa o estado do solver BLEIC
        alglib.minbleiccreate(x, out alglib.minbleicstate state);

        // 2. Injeta os limites de contorno (Bounds) nativamente, se existirem.
        // Isso impede que o solver sequer tente avaliar constantes negativas, acelerando a busca.
        if (input.LowerBounds != null && input.UpperBounds != null)
        {
            alglib.minbleicsetbc(state, input.LowerBounds, input.UpperBounds);
        }

        // 3. Define as condições de parada
        alglib.minbleicsetcond(state, input.Tolerance, input.Tolerance, input.Tolerance, input.MaxIterations);

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

        return rep.terminationtype > 0 
            ? new CurveFitOutput(x, state.f, rep.iterationscount)
            : throw new InvalidOperationException($"Failed to fit curve using ALGLIB. Termination type: {rep.terminationtype}. Iteractions: {rep.iterationscount}");
    }
}
