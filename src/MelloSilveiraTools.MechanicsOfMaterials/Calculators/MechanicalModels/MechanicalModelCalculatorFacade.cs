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
    private readonly Func<MechanicalModelOutput>? _outputFactory;
    private readonly Dictionary<string, Action<object, object>>? _outputPropertySetters;
    private readonly Func<double, (double Value, double Derivative)>? _calculateValueAndDerivativeMethod;
    private readonly string? _inputParameterValueName;
    private readonly string? _inputParameterDerivativeName;
    private readonly string? _outputParameterValueName;
    private readonly string? _outputParameterDerivativeName;
    private readonly Dictionary<string, object>? _inputParameters;
    private readonly Dictionary<string, object>? _outputParameters;

    public MechanicalModelCalculatorFacade(IMechanicalModelTypeCache cache, Type outputType, object calculator, MechanicalModelInput input) : this(cache, calculator)
    {
        ArgumentNullException.ThrowIfNull(input);

        _calculatorMethodDataList = cache.GetOrAddMethodDataList(_calculatorType, input.MechanicalRelationship, input.ViscoelasticEffect);
        _outputFactory = cache.GetOrAddOutputFactory(outputType);
        _outputPropertySetters = cache.GetOrAddPropertySetters(outputType);

        switch (input.MechanicalRelationship, input.ViscoelasticEffect)
        {
            case (MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Relaxation):
                _calculateValueAndDerivativeMethod = input.Displacement!.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Displacement;
                _inputParameterDerivativeName = ParameterNameConstant.DisplacementDerivative;
                _outputParameterValueName = nameof(MechanicalModelOutput.Displacement);
                _outputParameterDerivativeName = nameof(MechanicalModelOutput.DisplacementDerivative);
                break;

            case (MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Creep):
                _calculateValueAndDerivativeMethod = input.Force!.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Force;
                _inputParameterDerivativeName = ParameterNameConstant.ForceDerivative;
                _outputParameterValueName = nameof(MechanicalModelOutput.Force);
                _outputParameterDerivativeName = nameof(MechanicalModelOutput.ForceDerivative);
                break;

            case (MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation):
                _calculateValueAndDerivativeMethod = input.Strain!.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Strain;
                _inputParameterDerivativeName = ParameterNameConstant.StrainDerivative;
                _outputParameterValueName = nameof(MechanicalModelOutput.Strain);
                _outputParameterDerivativeName = nameof(MechanicalModelOutput.StrainDerivative);
                break;

            case (MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Creep):
                _calculateValueAndDerivativeMethod = input.Stress!.CalculateValueAndDerivative;
                _inputParameterValueName = ParameterNameConstant.Stress;
                _inputParameterDerivativeName = ParameterNameConstant.StressDerivative;
                _outputParameterValueName = nameof(MechanicalModelOutput.Stress);
                _outputParameterDerivativeName = nameof(MechanicalModelOutput.StressDerivative);
                break;

            default:
                throw new ArgumentOutOfRangeException($"{nameof(input.MechanicalRelationship)} and {nameof(input.ViscoelasticEffect)}");
        }

        _inputParameters = new(capacity: 4) { { ParameterNameConstant.MechanicalModelInput, input } };
        _outputParameters = new(capacity: 3);
    }

    /// <inheritdoc/>
    public MechanicalModelOutput Calculate(double time)
    {
        (double value, double derivative) = _calculateValueAndDerivativeMethod!(time);

        _inputParameters![ParameterNameConstant.Time] = time;
        _inputParameters[_inputParameterValueName!] = value;
        _inputParameters[_inputParameterDerivativeName!] = derivative;

        _outputParameters![nameof(MechanicalModelOutput.Time)] = time;
        _outputParameters[_outputParameterValueName!] = value;
        _outputParameters[_outputParameterDerivativeName!] = derivative;

        MechanicalModelOutput output = _outputFactory!();

        foreach (KeyValuePair<string, object> entry in _outputParameters)
        {
            var setter = _outputPropertySetters![entry.Key];
            setter(output, entry.Value);
        }

        foreach (CalculatorMethodData data in _calculatorMethodDataList!)
        {
            object[] methodParameters = [.. data.ParameterNames.Select(name => _inputParameters[name])];
            object methodValue = data.Invoker(calculator, methodParameters);
            _outputPropertySetters![data.PropertyName](output, methodValue);
        }

        return output;
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