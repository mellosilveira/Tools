using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Attributes;

/// <summary>
/// Attribute to tag properties in mechanical models, linking them to specific mechanical 
/// relationships and viscoelastic effects for dynamic construction of result.
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
    /// <param name="mechanicalRelationship"></param>
    public MechanicalModelParameterAttribute(MechanicalRelationship mechanicalRelationship)
    {
        MechanicalRelationship = mechanicalRelationship;
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
    /// <param name="mechanicalRelationship"></param>
    /// <param name="viscoelasticEffect"></param>
    public MechanicalModelParameterAttribute(MechanicalRelationship mechanicalRelationship, ViscoelasticEffect viscoelasticEffect)
    {
        MechanicalRelationship = mechanicalRelationship;
        ViscoelasticEffect = viscoelasticEffect;
    }

    /// <inheritdoc cref="Models.MechanicalModels.MechanicalRelationship"/>
    public MechanicalRelationship? MechanicalRelationship { get; }

    /// <inheritdoc cref="Models.MechanicalModels.Viscoelasticity.ViscoelasticEffect"/>
    public ViscoelasticEffect? ViscoelasticEffect { get; }

    /// <summary>
    /// Checks if the mechanical relationship and viscoelastic effect matches with the values used to build the attribute.
    /// </summary>
    /// <param name="mechanicalRelationship"></param>
    /// <param name="viscoelasticEffect"></param>
    /// <returns></returns>
    public bool CanMethodBeInvoked(MechanicalRelationship mechanicalRelationship, ViscoelasticEffect viscoelasticEffect)
    {
        return (!MechanicalRelationship.HasValue || MechanicalRelationship == mechanicalRelationship) 
            && (!ViscoelasticEffect.HasValue || ViscoelasticEffect == viscoelasticEffect);
    }
}
