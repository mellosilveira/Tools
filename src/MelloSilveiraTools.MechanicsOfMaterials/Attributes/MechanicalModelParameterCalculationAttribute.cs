using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Attributes;

/// <summary>
/// Attribute to tag methods that calculate parameters in mechanical models, linking them to specific mechanical 
/// relationships and viscoelastic effects for dynamic method selection based on the model's characteristics.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class MechanicalModelParameterCalculationAttribute : MechanicalModelParameterAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelParameterAttribute"/>.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="mechanicalRelationship"></param>
    public MechanicalModelParameterCalculationAttribute(
        string propertyName,
        MechanicalRelationship mechanicalRelationship) : base(mechanicalRelationship)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelParameterAttribute"/>.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="viscoelasticEffect"></param>
    public MechanicalModelParameterCalculationAttribute(
        string propertyName,
        ViscoelasticEffect viscoelasticEffect) : base(viscoelasticEffect)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelParameterAttribute"/>.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="mechanicalRelationship"></param>
    /// <param name="viscoelasticEffect"></param>
    public MechanicalModelParameterCalculationAttribute(
        string propertyName,
        MechanicalRelationship mechanicalRelationship,
        ViscoelasticEffect viscoelasticEffect) : base(mechanicalRelationship, viscoelasticEffect)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Name of property that is calculated.
    /// </summary>
    public string PropertyName { get; }
}