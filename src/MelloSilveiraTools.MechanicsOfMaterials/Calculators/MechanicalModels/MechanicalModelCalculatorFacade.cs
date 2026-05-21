using MelloSilveiraTools.MechanicsOfMaterials.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <inheritdoc cref="IMechanicalModelCalculatorFacade"/>
public class MechanicalModelCalculatorFacade(IMechanicalModelTypeCache cache, object calculator) : IMechanicalModelCalculatorFacade
{
    private readonly Type _calculatorType = calculator.GetType();

    private readonly Func<object, object?[], object> _invokeDisplacement = cache.GetOrAddMethodInvoker(calculator.GetType(), nameof(IMechanicalModelCalculator<>.CalculateDisplacement));
    private readonly Func<object, object?[], object> _invokeForce = cache.GetOrAddMethodInvoker(calculator.GetType(), nameof(IMechanicalModelCalculator<>.CalculateForce));
    private readonly Func<object, object?[], object> _invokeStress = cache.GetOrAddMethodInvoker(calculator.GetType(), nameof(IMechanicalModelCalculator<>.CalculateStress));
    private readonly Func<object, object?[], object> _invokeStrain = cache.GetOrAddMethodInvoker(calculator.GetType(), nameof(IMechanicalModelCalculator<>.CalculateStrain));

    private readonly CalculatorMethodData[]? _calculatorMethodDataList;
    private readonly Func<MechanicalModelResult>? _resultFactory;
    private readonly Dictionary<string, Action<object, object>>? _resultPropertySetters;
    private readonly Func<double, (double Value, double Derivative)>? _calculateValueAndDerivativeMethod;
    private readonly string? _inputParameterValueName;
    private readonly string? _inputParameterDerivativeName;
    private readonly string? _resultParameterValueName;
    private readonly string? _resultParameterDerivativeName;
    private readonly Dictionary<string, object>? _inputParameters;
    private readonly Dictionary<string, object>? _resultParameters;

    public MechanicalModelCalculatorFacade(IMechanicalModelTypeCache cache, Type resultType, object calculator, MechanicalModelInput input) : this(cache, calculator)
    {
        ArgumentNullException.ThrowIfNull(input);

        _calculatorMethodDataList = cache.GetOrAddMethodDataList(_calculatorType, input.MechanicalRelationship, input.ViscoelasticEffect);
        _resultFactory = cache.GetOrAddResultFactory(resultType);
        _resultPropertySetters = cache.GetOrAddPropertySetters(resultType);

        switch (input.MechanicalRelationship, input.ViscoelasticEffect)
        {
            case (MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation):
                _calculateValueAndDerivativeMethod = input.Displacement.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Displacement;
                _inputParameterDerivativeName = ParameterNameConstant.DisplacementDerivative;
                _resultParameterValueName = nameof(MechanicalModelResult.Displacement);
                _resultParameterDerivativeName = nameof(MechanicalModelResult.DisplacementDerivative);
                break;

            case (MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Creep):
                _calculateValueAndDerivativeMethod = input.Force.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Force;
                _inputParameterDerivativeName = ParameterNameConstant.ForceDerivative;
                _resultParameterValueName = nameof(MechanicalModelResult.Force);
                _resultParameterDerivativeName = nameof(MechanicalModelResult.ForceDerivative);
                break;

            case (MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation):
                _calculateValueAndDerivativeMethod = input.Strain.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Strain;
                _inputParameterDerivativeName = ParameterNameConstant.StrainDerivative;
                _resultParameterValueName = nameof(MechanicalModelResult.Strain);
                _resultParameterDerivativeName = nameof(MechanicalModelResult.StrainDerivative);
                break;

            case (MechanicalRelationship.StressStrain, ViscoelasticEffect.Creep):
                _calculateValueAndDerivativeMethod = input.Stress.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Stress;
                _inputParameterDerivativeName = ParameterNameConstant.StressDerivative;
                _resultParameterValueName = nameof(MechanicalModelResult.Stress);
                _resultParameterDerivativeName = nameof(MechanicalModelResult.StressDerivative);
                break;

            default:
                throw new ArgumentOutOfRangeException($"{nameof(input.MechanicalRelationship)} and {nameof(input.ViscoelasticEffect)}");
        }

        _inputParameters = new(capacity: 4) { { ParameterNameConstant.MechanicalModelInput, input } };
        _resultParameters = new(capacity: 3);
    }

    /// <inheritdoc/>
    public MechanicalModelResult CalculateResult(double time)
    {
        (double value, double derivative) = _calculateValueAndDerivativeMethod!(time);

        _inputParameters![ParameterNameConstant.Time] = time;
        _inputParameters[_inputParameterValueName!] = value;
        _inputParameters[_inputParameterDerivativeName!] = derivative;

        _resultParameters![nameof(MechanicalModelResult.Time)] = time;
        _resultParameters[_resultParameterValueName!] = value;
        _resultParameters[_resultParameterDerivativeName!] = derivative;

        MechanicalModelResult result = _resultFactory!();

        foreach (KeyValuePair<string, object> entry in _resultParameters)
        {
            var setter = _resultPropertySetters![entry.Key];
            setter(result, entry.Value);
        }

        foreach (CalculatorMethodData data in _calculatorMethodDataList!)
        {
            object[] methodParameters = [.. data.ParameterNames.Select(name => _inputParameters[name])];
            object methodValue = data.Invoker(calculator, methodParameters);
            _resultPropertySetters![data.PropertyName](result, methodValue);
        }

        return result;
    }

    /// <inheritdoc/>
    public double CalculateDisplacement(MechanicalModelInput input, double time, double? force) => (double)_invokeDisplacement(calculator, [input, time, force]);

    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput input, double time, double? displacement) => (double)_invokeForce(calculator, [input, time, displacement]);

    /// <inheritdoc/>
    public double CalculateStress(MechanicalModelInput input, double time, double? strain) => (double)_invokeStress(calculator, [input, time, strain]);

    public double CalculateStrain(MechanicalModelInput input, double time, double? stress) => (double)_invokeStrain(calculator, [input, time, stress]);

    public class ParameterNameConstant
    {
        public const string MechanicalModelInput = "input";
        public const string Time = "time";
        public const string Force = "force";
        public const string ForceDerivative = "forceDerivative";
        public const string Displacement = "displacement";
        public const string DisplacementDerivative = "displacementDerivative";
        public const string Stress = "stress";
        public const string StressDerivative = "stressDerivative";
        public const string Strain = "strain";
        public const string StrainDerivative = "strainDerivative";
    }
}