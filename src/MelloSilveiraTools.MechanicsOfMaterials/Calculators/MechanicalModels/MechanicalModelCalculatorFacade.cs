using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using SoftTissue.Infrastructure.Caching;
using SoftTissue.Infrastructure.TypeResolvers;

namespace SoftTissue.UseCases.Facade.MechanicalModels;

/// <inheritdoc cref="IMechanicalModelCalculatorFacade"/>
public class MechanicalModelCalculatorFacade : IMechanicalModelCalculatorFacade
{
    private readonly IMechanicalModelTypeCache _typeCache;
    private readonly IMechanicalModelTypeResolver _typeResolver;
    private readonly object _calculator;
    private readonly Type _calculatorType;

    private readonly Func<object, object[], object> _invokeDisplacement;
    private readonly Func<object, object[], object> _invokeForce;
    private readonly Func<object, object[], object> _invokeStress;

    private readonly CalculatorMethodData[] _calculatorMethodDataList;
    private readonly Func<MechanicalModelResult> _resultFactory;
    private readonly Dictionary<string, Action<object, object>> _resultPropertySetters;
    private readonly Func<double, (double Value, double Derivative)> _calculateValueAndDerivativeMethod;
    private readonly string _inputParameterValueName;
    private readonly string _inputParameterDerivativeName;
    private readonly string _resultParameterValueName;
    private readonly string _resultParameterDerivativeName;
    private readonly Dictionary<string, object> _inputParameters;
    private readonly Dictionary<string, object> _resultParameters;

    public MechanicalModelCalculatorFacade(IMechanicalModelTypeCache typeCache, IMechanicalModelTypeResolver typeResolver, object calculator)
    {
        _typeCache = typeCache ?? throw new ArgumentNullException(nameof(typeCache));
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _calculatorType = _calculator.GetType();
        _invokeDisplacement = _typeCache.GetOrAddMethodInvoker(_calculatorType, nameof(IMechanicalModelCalculator<>.CalculateDisplacement));
        _invokeForce = _typeCache.GetOrAddMethodInvoker(_calculatorType, nameof(IMechanicalModelCalculator<>.CalculateForce));
        _invokeStress = _typeCache.GetOrAddMethodInvoker(_calculatorType, nameof(IMechanicalModelCalculator<>.CalculateStress));
    }

    public MechanicalModelCalculatorFacade(IMechanicalModelTypeCache typeCache, IMechanicalModelTypeResolver typeResolver, object calculator, MechanicalModelInput input)
        : this(typeCache, typeResolver, calculator)
    {
        ArgumentNullException.ThrowIfNull(input);

        Type resultType = _typeResolver.Result;
        _calculatorMethodDataList = _typeCache.GetOrAddMethodDataList(_calculatorType, input.MechanicalRelationship, input.ViscoelasticEffect);
        _resultFactory = _typeCache.GetOrAddResultFactory(resultType);
        _resultPropertySetters = _typeCache.GetOrAddPropertySetters(resultType);

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

        _inputParameters = new(capacity: 4)
        {
            { ParameterNameConstant.MechanicalModelInput, input },
            { ParameterNameConstant.Time, null },
            { _inputParameterValueName, null },
            { _inputParameterDerivativeName, null },
        };

        _resultParameters = new(capacity: 3)
        {
            { nameof(MechanicalModelResult.Time), null },
            { _resultParameterValueName, null },
            { _resultParameterDerivativeName, null },
        };
    }

    /// <inheritdoc/>
    public MechanicalModelResult CalculateResult(double time)
    {
        (double value, double derivative) = _calculateValueAndDerivativeMethod(time);

        _inputParameters[ParameterNameConstant.Time] = time;
        _inputParameters[_inputParameterValueName] = value;
        _inputParameters[_inputParameterDerivativeName] = derivative;

        _resultParameters[nameof(MechanicalModelResult.Time)] = time;
        _resultParameters[_resultParameterValueName] = value;
        _resultParameters[_resultParameterDerivativeName] = derivative;

        MechanicalModelResult result = _resultFactory();

        foreach (KeyValuePair<string, object> entry in _resultParameters)
        {
            var setter = _resultPropertySetters[entry.Key];
            setter(result, entry.Value);
        }

        foreach (CalculatorMethodData data in _calculatorMethodDataList)
        {
            object[] methodParameters = [.. data.ParameterNames.Select(name => _inputParameters.GetValueOrDefault(name))];
            object methodValue = data.Invoker(_calculator, methodParameters);
            _resultPropertySetters[data.PropertyName](result, methodValue);
        }

        return result;
    }

    /// <inheritdoc/>
    public double CalculateDisplacement(MechanicalModelInput input, double time, double force)
        => (double)_invokeDisplacement(_calculator, [input, time, force]);

    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput input, double time, double displacement)
        => (double)_invokeForce(_calculator, [input, time, displacement]);

    /// <inheritdoc/>
    public double CalculateStress(MechanicalModelInput input, double time, double strain)
        => (double)_invokeStress(_calculator, [input, time, strain]);
}