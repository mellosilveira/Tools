using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations.Range;

namespace MelloSilveiraTools.MechanicsOfMaterials.ExtensionMethods;

/// <summary>
/// It contains extension methods for <see cref="RangeParameters"/>
/// </summary>
public static class RangeExtensions
{
    extension(List<RangeParameters> rangeParametersList)
    {
        /// <summary>
        /// Gets all possible combinations from a list of <see cref="RangeParameters"/>.
        /// </summary>
        /// <returns>List of double array with all possible combinations.</returns>
        public IEnumerable<double[]> GetCombinations() => rangeParametersList.Select(rp => rp.ToList()).GetCombinations();
    }

    extension(RangeParameters value)
    {
        /// <summary>
        /// Converts a <see cref="RangeParameters"/> to <see cref="List{T}"/> of double.
        /// </summary>
        /// <returns></returns>
        public List<double> ToList()
        {
            if (value is null)
                return [];

            if (!value.Step.HasValue && (!value.MultiplicativeFactor.HasValue || !value.FinalPoint.HasValue))
                return [value.InitialPoint];

            List<double> list = [];
            double point = value.InitialPoint;

            if (value.Step.HasValue)
            {
                while (point < value.FinalPoint)
                {
                    list.Add(point);
                    point += value.Step.Value;
                }

                list.Add(value.FinalPoint.Value);
            }
            else if (value.MultiplicativeFactor.HasValue)
            {
                while (point < value.FinalPoint)
                {
                    list.Add(point);
                    point *= value.MultiplicativeFactor.Value;
                }

                list.Add(value.FinalPoint.Value);
            }

            return list;
        }
    }

    extension(RangeFunction rangeFunction)
    {
        public IEnumerable<Function> BuildFunctions(FunctionFactory functionFactory)
        {
            if (rangeFunction is null)
            {
                yield break;
            }

            foreach (double[] coefficients in rangeFunction.Coefficients.GetCombinations())
            {
                foreach (FunctionType type in rangeFunction.Types)
                {
                    yield return functionFactory.Create(type, rangeFunction.InitialVariableValue, rangeFunction.FinalVariableValue, coefficients);
                }
            }
        }
    }

    extension(RangePowerLaw rangePowerLaw)
    {
        public IEnumerable<PowerLaw> BuildPowerLawFunctions()
        {
            if (rangePowerLaw is null)
            {
                yield break;
            }

            foreach (double[] iteratorCoefficients in rangePowerLaw.Coefficients.GetCombinations())
            {
                yield return new PowerLaw(rangePowerLaw.InitialVariableValue, rangePowerLaw.FinalVariableValue, iteratorCoefficients);
            }
        }
    }

    extension(RangePronySeries rangePronySeries)
    {
        public IEnumerable<PronySeries> BuildPronySeriesFunctions()
        {
            if (rangePronySeries is null)
            {
                yield break;
            }

            foreach (double independentParameter in rangePronySeries.IndependentParameter.ToList())
            {
                foreach (double[] iteratorCoefficients in rangePronySeries.Coefficients.GetCombinations())
                {
                    yield return new PronySeries(rangePronySeries.InitialVariableValue, rangePronySeries.FinalVariableValue, independentParameter, iteratorCoefficients);
                }
            }
        }
    }
}
