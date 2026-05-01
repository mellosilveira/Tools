using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.DifferentialEquation;

namespace MelloSilveiraTools.Mathematics.Factories.NumericalMethods;

/// <summary>
/// Resolves the appropriate <see cref="IDifferentialEquationMethod"/> implementation based on the
/// requested <see cref="DifferentialEquationMethodType"/>. The factory matches by the
/// <see cref="IDifferentialEquationMethod.Type"/> property of every implementation registered in the
/// DI container, picking the single one whose type equals the requested value (e.g. it returns
/// <see cref="NewmarkMethod"/> for <see cref="DifferentialEquationMethodType.Newmark"/> and
/// <see cref="NewmarkBetaMethod"/> for <see cref="DifferentialEquationMethodType.NewmarkBeta"/>).
/// </summary>
public class DifferentialEquationMethodFactory(IEnumerable<IDifferentialEquationMethod> differentialEquationMethods)
{
    private readonly Dictionary<DifferentialEquationMethodType, IDifferentialEquationMethod> _dictionary = differentialEquationMethods.ToDictionary(dem => dem.Type);

    /// <summary>
    /// Gets the <see cref="IDifferentialEquationMethod"/> registered for the supplied <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The numerical method to resolve.</param>
    /// <returns>The single registered implementation whose <see cref="IDifferentialEquationMethod.Type"/> equals <paramref name="type"/>.</returns>
    public IDifferentialEquationMethod Get(DifferentialEquationMethodType type) => _dictionary[type];
}
