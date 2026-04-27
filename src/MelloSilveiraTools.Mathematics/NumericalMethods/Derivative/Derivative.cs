namespace MelloSilveiraTools.Mathematics.NumericalMethods.Derivative
{
    /// <inheritdoc/>
    public class Derivative : IDerivative
    {
        // TODO: SE PRECISAR DE UMA DERIVADA MUITO COMPLEXA, PODE GERAR UM TEMPO ARTIFICIAL PARA CADA DERIVADA.
        // TODO: PESQUISAR SOBRE PARKING LOT RELACIONADA A UTILIZAÇÃO DE MEMÓRIA.

        /// <inheritdoc/>
        public double Calculate(Func<double, double> equation, double timeStep, double time)
        {
            double previous = equation(time - timeStep);
            double nextValue = equation(time + timeStep);

            return (nextValue - previous) / (2 * timeStep);
        }

        /// <inheritdoc/>
        public double Calculate(double initialPoint, double finalPoint, double step)
        {
            return (finalPoint - initialPoint) / step;
        }
    }
}
