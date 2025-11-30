using MelloSilveiraTools.Domain.Models;

namespace MelloSilveiraTools.Domain.NumericalMethods.DifferentialEquation;

public class DifferentialEquationMethodFactory(IEnumerable<IDifferentialEquationMethod> differentialEquationMethods)
{
    public IDifferentialEquationMethod Get(DifferentialEquationMethodType type) => differentialEquationMethods.Single(dem => dem.Type == type);
}
