using MelloSilveiraTools.MechanicsOfMaterials.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <inheritdoc cref="IMechanicalModelCalculatorFacade"/>
public class MechanicalModelCalculatorFacade : IMechanicalModelCalculatorFacade
{
    private readonly Type _calculatorType;

    /// <summary>
    /// Cached compiled invoker for the underlying displacement calculation method.
    /// </summary>
    private readonly Func<object, object?[], object> _invokeDisplacement;
    
    /// <summary>
    /// Cached compiled invoker for the underlying force calculation method.
    /// </summary>
    private readonly Func<object, object?[], object> _invokeForce;
    
    /// <summary>
    /// Cached compiled invoker for the underlying stress calculation method.
    /// </summary>
    private readonly Func<object, object?[], object> _invokeStress;
    
    /// <summary>
    /// Cached compiled invoker for the underlying strain calculation method.
    /// </summary>
    private readonly Func<object, object?[], object> _invokeStrain;

    /// <summary>
    /// An optimized array containing pre-fetched reflection metadata and compiled invokers for specific parameter calculation loops.
    /// </summary>
    private readonly CalculatorMethodData[]? _calculatorMethodDataList;
    
    /// <summary>
    /// Factory delegate compiled to instantly instantiate instances of the target projection output model without instantiation overhead.
    /// </summary>
    private readonly Func<MechanicalModelOutput>? _outputFactory;
    
    /// <summary>
    /// Fast-access dictionary containing compiled property-setter expressions mapped by property names for the designated output type.
    /// </summary>
    private readonly Dictionary<string, Action<object, object>>? _outputPropertySetters;
    
    /// <summary>
    /// Delegate targeting the boundary condition's function to compute the boundary value and its first-order time derivative concurrently.
    /// </summary>
    private readonly Func<double, (double Value, double Derivative)>? _calculateValueAndDerivativeMethod;
    
    /// <summary>
    /// The exact argument name corresponding to the input boundary parameter value expected by the underlying model methods.
    /// </summary>
    private readonly string? _inputParameterValueName;
    
    /// <summary>
    /// The exact argument name corresponding to the input boundary parameter's temporal derivative expected by the underlying model methods.
    /// </summary>
    private readonly string? _inputParameterDerivativeName;
    
    /// <summary>
    /// The target property identifier on the output instance where the independent boundary condition value is mirrored.
    /// </summary>
    private readonly string? _outputParameterValueName;
    
    /// <summary>
    /// The target property identifier on the output instance where the independent boundary condition's temporal derivative is mirrored.
    /// </summary>
    private readonly string? _outputParameterDerivativeName;
    
    /// <summary>
    /// Mutable parameter buffer dictionary passing time, input data models, and boundary states into the compiled reflection invokers.
    /// </summary>
    private readonly Dictionary<string, object>? _inputParameters;
    
    /// <summary>
    /// State map tracking independent variables and current integration times to initialize the freshly generated output objects.
    /// </summary>
    private readonly Dictionary<string, object>? _outputParameters;
    private readonly IMechanicalModelTypeCache _cache;
    private readonly object _calculator;

    public MechanicalModelCalculatorFacade(IMechanicalModelTypeCache cache, object calculator)
    {
        _calculatorType = calculator.GetType();
        _invokeDisplacement = cache.GetOrAddMethodInvoker(_calculatorType, nameof(IMechanicalModelCalculator<>.CalculateDisplacement));
        _invokeForce = cache.GetOrAddMethodInvoker(_calculatorType, nameof(IMechanicalModelCalculator<>.CalculateForce));
        _invokeStress = cache.GetOrAddMethodInvoker(_calculatorType, nameof(IMechanicalModelCalculator<>.CalculateStress));
        _invokeStrain = cache.GetOrAddMethodInvoker(_calculatorType, nameof(IMechanicalModelCalculator<>.CalculateStrain));

        _cache = cache;
        _calculator = calculator;
    }

    /// <summary>
    /// Initializes a secondary instance of the <see cref="MechanicalModelCalculatorFacade"/> class optimized for iterative time-history simulations.
    /// </summary>
    /// <remarks>
    /// This constructor evaluates the combination of <see cref="MechanicalBehaviorType"/> and <see cref="ViscoelasticEffect"/> 
    /// to determine the independent boundary condition. It extracts the value-derivative delegate, registers parameter tokens, 
    /// and pre-allocates localized parameter buffers to eliminate allocation overhead during numerical loops.
    /// </remarks>
    /// <param name="cache">The high-performance type metadata cache mechanism.</param>
    /// <param name="outputType">The explicit projection model type representing the output structure.</param>
    /// <param name="calculator">The specific mathematical mechanical solver instance.</param>
    /// <param name="input">The operational boundaries, specimen geometry, and time configurations.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided input argument or required internal entities evaluate to null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combination of behavior type and viscoelastic mechanism is unsupported by the domain logic.</exception>
    public MechanicalModelCalculatorFacade(IMechanicalModelTypeCache cache, Type outputType, object calculator, GenericMechanicalModelInput input) : this(cache, calculator)
    {
        ArgumentNullException.ThrowIfNull(input);

        _calculatorMethodDataList = cache.GetOrAddMethodDataList(_calculatorType, input.MechanicalBehaviorType, input.ViscoelasticEffect);
        _outputFactory = cache.GetOrAddOutputFactory(outputType);
        _outputPropertySetters = cache.GetOrAddPropertySetters(outputType);

        switch (input.MechanicalBehaviorType, input.ViscoelasticEffect)
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
                throw new ArgumentOutOfRangeException($"{nameof(input.MechanicalBehaviorType)} and {nameof(input.ViscoelasticEffect)}");
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
            object methodValue = data.Invoker(_calculator, methodParameters);
            _outputPropertySetters![data.PropertyName](output, methodValue);
        }

        return output;
    }

    /// <inheritdoc/>
    public double CalculateDisplacement(GenericMechanicalModelInput input, double time, double? force) => (double)_invokeDisplacement(_calculator, [input, time, force]);

    /// <inheritdoc/>
    public double CalculateForce(GenericMechanicalModelInput input, double time, double? displacement) => (double)_invokeForce(_calculator, [input, time, displacement]);

    /// <inheritdoc/>
    public double CalculateStress(GenericMechanicalModelInput input, double time, double? strain) => (double)_invokeStress(_calculator, [input, time, strain]);

    /// <inheritdoc/>
    public double CalculateStrain(GenericMechanicalModelInput input, double time, double? stress) => (double)_invokeStrain(_calculator, [input, time, stress]);

    /// <summary>
    /// Contains standard literal parameter name constants used across the meta-programming routing infrastructure.
    /// </summary>
    public class ParameterNameConstant
    {
        /// <summary>Identifier for the configuration parameter model instance argument.</summary>
        public const string MechanicalModelInput = "input";
        
        /// <summary>Identifier for the current step timeline argument.</summary>
        public const string Time = "time";
        
        /// <summary>Identifier for structural force bounds.</summary>
        public const string Force = "force";
        
        /// <summary>Identifier for the temporal rate of structural force bounds.</summary>
        public const string ForceDerivative = "forceDerivative";
        
        /// <summary>Identifier for macrostructural kinematic displacements.</summary>
        public const string Displacement = "displacement";
        
        /// <summary>Identifier for the kinematic velocity of structural displacements.</summary>
        public const string DisplacementDerivative = "displacementDerivative";
        
        /// <summary>Identifier for the continuum Cauchy stress bounds.</summary>
        public const string Stress = "stress";
        
        /// <summary>Identifier for the internal loading rate of the stress continuum.</summary>
        public const string StressDerivative = "stressDerivative";
        
        /// <summary>Identifier for the material Green-Lagrange or infinitesimal strains.</summary>
        public const string Strain = "strain";
        
        /// <summary>Identifier for the material strain rate deformation tensor component.</summary>
        public const string StrainDerivative = "strainDerivative";
    }
}