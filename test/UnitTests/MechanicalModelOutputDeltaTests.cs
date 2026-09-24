using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using Xunit;

namespace UnitTests;

/// <summary>
/// Unit tests verifying delta and percentage delta calculations across the <see cref="MechanicalModelOutput"/> hierarchy.
/// </summary>
public class MechanicalModelOutputDeltaTests
{
    [Fact]
    public void MechanicalModelOutput_CalculateDelta_ComputesAbsoluteDifference()
    {
        MechanicalModelOutput initial = new()
        {
            Time = 0.0,
            Strain = 0.05,
            Stress = 10.0,
            Displacement = 1.0,
            Force = 50.0
        };

        MechanicalModelOutput current = new()
        {
            Time = 10.0,
            Strain = 0.15,
            Stress = 30.0,
            Displacement = 3.0,
            Force = 150.0
        };

        MechanicalModelOutput delta = current.CalculateDelta(initial);

        Assert.Equal(10.0, delta.Time);
        Assert.Equal(0.10, delta.Strain!.Value, 6);
        Assert.Equal(20.0, delta.Stress!.Value, 6);
        Assert.Equal(2.0, delta.Displacement!.Value, 6);
        Assert.Equal(100.0, delta.Force!.Value, 6);
    }

    [Fact]
    public void MechanicalModelOutput_CalculatePercentageDelta_ComputesPercentageDifference()
    {
        MechanicalModelOutput initial = new()
        {
            Time = 0.0,
            Strain = 0.10,
            Stress = 100.0
        };

        MechanicalModelOutput current = new()
        {
            Time = 5.0,
            Strain = 0.15,
            Stress = 80.0
        };

        MechanicalModelOutput delta = current.CalculatePercentageDelta(initial);

        Assert.Equal(100.0, delta.Time);
        Assert.Equal(50.0, delta.Strain!.Value, 3);
        Assert.Equal(-20.0, delta.Stress!.Value, 3);
    }

    [Fact]
    public void QuasiLinearModelOutput_CalculateDelta_PreservesDerivedProperties()
    {
        QuasiLinearModelOutput initial = new()
        {
            Time = 0.0,
            Strain = 0.1,
            Stress = 100.0,
            ReducedRelaxationFunction = 1.0,
            ElasticResponse = 100.0,
            ElasticForceResponse = 200.0
        };

        QuasiLinearModelOutput current = new()
        {
            Time = 10.0,
            Strain = 0.1,
            Stress = 60.0,
            ReducedRelaxationFunction = 0.6,
            ElasticResponse = 100.0,
            ElasticForceResponse = 200.0
        };

        QuasiLinearModelOutput delta = (QuasiLinearModelOutput)current.CalculateDelta(initial);

        Assert.Equal(10.0, delta.Time);
        Assert.Equal(-40.0, delta.Stress!.Value, 6);
        Assert.Equal(-0.4, delta.ReducedRelaxationFunction!.Value, 6);
        Assert.Equal(0.0, delta.ElasticResponse!.Value, 6);
    }

    [Fact]
    public void SchaperyModelOutput_CalculatePercentageDelta_PreservesDerivedProperties()
    {
        SchaperyModelOutput initial = new()
        {
            Time = 0.0,
            Stress = 100.0,
            TransientRelaxationFunction = 50.0,
            TransientCreepCompliance = 0.01
        };

        SchaperyModelOutput current = new()
        {
            Time = 10.0,
            Stress = 80.0,
            TransientRelaxationFunction = 40.0,
            TransientCreepCompliance = 0.015
        };

        SchaperyModelOutput delta = (SchaperyModelOutput)current.CalculatePercentageDelta(initial);

        Assert.Equal(100.0, delta.Time);
        Assert.Equal(-20.0, delta.Stress!.Value, 3);
        Assert.Equal(-20.0, delta.TransientRelaxationFunction!.Value, 3);
        Assert.Equal(50.0, delta.TransientCreepCompliance!.Value, 3);
    }
}
