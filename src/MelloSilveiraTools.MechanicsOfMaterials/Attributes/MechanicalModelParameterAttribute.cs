using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Attributes;

/// <summary>
/// Attribute to tag properties in mechanical models, linking them to specific mechanical 
/// relationships and viscoelastic effects for dynamic construction of output.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MechanicalModelParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelParameterAttribute"/>.
    /// </summary>
    public MechanicalModelParameterAttribute() { }

    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelParameterAttribute"/>.
    /// </summary>
    /// <param name="mechanicalBehaviorType"></param>
    public MechanicalModelParameterAttribute(MechanicalBehaviorType mechanicalBehaviorType)
    {
        MechanicalBehaviorType = mechanicalBehaviorType;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelParameterAttribute"/>.
    /// </summary>
    /// <param name="viscoelasticEffect"></param>
    public MechanicalModelParameterAttribute(ViscoelasticEffect viscoelasticEffect)
    {
        ViscoelasticEffect = viscoelasticEffect;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelParameterAttribute"/>.
    /// </summary>
    /// <param name="mechanicalBehaviorType"></param>
    /// <param name="viscoelasticEffect"></param>
    public MechanicalModelParameterAttribute(MechanicalBehaviorType mechanicalBehaviorType, ViscoelasticEffect viscoelasticEffect)
    {
        MechanicalBehaviorType = mechanicalBehaviorType;
        ViscoelasticEffect = viscoelasticEffect;
    }

    /// <inheritdoc cref="Models.MechanicalModels.MechanicalBehaviorType"/>
    public MechanicalBehaviorType? MechanicalBehaviorType { get; }

    /// <inheritdoc cref="Models.MechanicalModels.Viscoelasticity.ViscoelasticEffect"/>
    public ViscoelasticEffect? ViscoelasticEffect { get; }

    /// <summary>
    /// Checks if the mechanical relationship and viscoelastic effect matches with the values used to build the attribute.
    /// </summary>
    /// <param name="mechanicalBehaviorType"></param>
    /// <param name="viscoelasticEffect"></param>
    /// <returns></returns>
    public bool CanMethodBeInvoked(MechanicalBehaviorType mechanicalBehaviorType, ViscoelasticEffect viscoelasticEffect)
    {
        return (!MechanicalBehaviorType.HasValue || MechanicalBehaviorType == mechanicalBehaviorType)
            && (!ViscoelasticEffect.HasValue || ViscoelasticEffect == viscoelasticEffect);
    }
}
