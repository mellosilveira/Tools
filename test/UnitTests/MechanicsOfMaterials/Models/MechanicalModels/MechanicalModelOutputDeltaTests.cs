using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace UnitTests.MechanicsOfMaterials.Models.MechanicalModels;

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
        Assert.Equal(33.333, delta.Strain!.Value, 3);
        Assert.Equal(-25.0, delta.Stress!.Value, 3);
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
        Assert.Equal(-25.0, delta.Stress!.Value, 3);
        Assert.Equal(-25.0, delta.TransientRelaxationFunction!.Value, 3);
        Assert.Equal(33.333, delta.TransientCreepCompliance!.Value, 3);
    }

    [Fact]
    public void SpecializedOutputs_CopyConstructorsDeltaAndPercentageDelta_WorkCorrectly()
    {
        ElasticModelOutput e0 = new() { Time = 1.0, Stress = 10.0, Stiffness = 100.0 };
        ElasticModelOutput e1 = new() { Time = 2.0, Stress = 20.0, Stiffness = 200.0 };
        ElasticModelOutput eCopy = new(e1);
        Assert.Equal(200.0, eCopy.Stiffness);
        ElasticModelOutput eDelta = (ElasticModelOutput)e1.CalculateDelta(e0);
        ElasticModelOutput ePct = (ElasticModelOutput)e1.CalculatePercentageDelta(e0);
        Assert.Equal(100.0, eDelta.Stiffness);
        Assert.Equal(50.0, ePct.Stiffness);

        MaxwellModelOutput m0 = new() { Time = 1.0, Stress = 10.0, RelaxationTime = 2.0 };
        MaxwellModelOutput m1 = new() { Time = 2.0, Stress = 20.0, RelaxationTime = 4.0 };
        MaxwellModelOutput mCopy = new(m1);
        Assert.Equal(4.0, mCopy.RelaxationTime);
        MaxwellModelOutput mDelta = (MaxwellModelOutput)m1.CalculateDelta(m0);
        MaxwellModelOutput mPct = (MaxwellModelOutput)m1.CalculatePercentageDelta(m0);
        Assert.Equal(2.0, mDelta.RelaxationTime);
        Assert.Equal(50.0, mPct.RelaxationTime);

        ModifiedSuperpositionMethodOutput msm0 = new() { Time = 1.0, InitialYoungModulus = 50.0, StressRelaxationRate = -0.2 };
        ModifiedSuperpositionMethodOutput msm1 = new() { Time = 2.0, InitialYoungModulus = 100.0, StressRelaxationRate = -0.4 };
        ModifiedSuperpositionMethodOutput msmCopy = new(msm1);
        Assert.Equal(100.0, msmCopy.InitialYoungModulus);
        ModifiedSuperpositionMethodOutput msmDelta = (ModifiedSuperpositionMethodOutput)msm1.CalculateDelta(msm0);
        ModifiedSuperpositionMethodOutput msmPct = (ModifiedSuperpositionMethodOutput)msm1.CalculatePercentageDelta(msm0);
        Assert.Equal(50.0, msmDelta.InitialYoungModulus);
        Assert.Equal(50.0, msmPct.StressRelaxationRate!.Value, 6);

        QuasiLinearModelOutput q0 = new() { Time = 1.0, ReducedRelaxationFunction = 1.0, ElasticResponse = 10.0, ElasticForceResponse = 20.0, StressByReducedRelaxationFunctionDerivative = 10.0, StressByConvolutionDerivative = 10.0 };
        QuasiLinearModelOutput q1 = new() { Time = 2.0, ReducedRelaxationFunction = 0.5, ElasticResponse = 20.0, ElasticForceResponse = 40.0, StressByReducedRelaxationFunctionDerivative = 20.0, StressByConvolutionDerivative = 20.0 };
        QuasiLinearModelOutput qCopy = new(q1);
        Assert.Equal(0.5, qCopy.ReducedRelaxationFunction);
        QuasiLinearModelOutput qPct = (QuasiLinearModelOutput)q1.CalculatePercentageDelta(q0);
        Assert.Equal(50.0, qPct.ElasticResponse!.Value, 6);

        SchaperyModelOutput s0 = new() { Time = 1.0, TransientCreepCompliance = 0.01, TransientRelaxationFunction = 10.0 };
        SchaperyModelOutput s1 = new() { Time = 2.0, TransientCreepCompliance = 0.03, TransientRelaxationFunction = 15.0 };
        SchaperyModelOutput sCopy = new(s1);
        Assert.Equal(15.0, sCopy.TransientRelaxationFunction);
        SchaperyModelOutput sDelta = (SchaperyModelOutput)s1.CalculateDelta(s0);
        Assert.Equal(0.02, sDelta.TransientCreepCompliance!.Value, 6);
        Assert.Equal(5.0, sDelta.TransientRelaxationFunction!.Value, 6);

        // Cover copy constructors with base types and nullable null-branches in CalculateDelta/CalculatePercentageDelta
        MechanicalModelOutput fullBase0 = new()
        {
            Time = 1.0,
            Strain = 0.1,
            StrainDerivative = 0.01,
            Stress = 10.0,
            StressDerivative = 1.0,
            Displacement = 0.01,
            DisplacementDerivative = 0.001,
            Force = 100.0,
            ForceDerivative = 10.0
        };
        MechanicalModelOutput fullBase1 = new()
        {
            Time = 2.0,
            Strain = 0.2,
            StrainDerivative = 0.02,
            Stress = 20.0,
            StressDerivative = 2.0,
            Displacement = 0.02,
            DisplacementDerivative = 0.002,
            Force = 200.0,
            ForceDerivative = 20.0
        };
        MechanicalModelOutput fullDelta = fullBase1.CalculateDelta(fullBase0);
        Assert.Equal(0.01, fullDelta.StrainDerivative!.Value, 6);
        Assert.Equal(1.0, fullDelta.StressDerivative!.Value, 6);
        Assert.Equal(0.001, fullDelta.DisplacementDerivative!.Value, 6);
        Assert.Equal(10.0, fullDelta.ForceDerivative!.Value, 6);

        MechanicalModelOutput emptyBase = new() { Time = 1.0 };
        MechanicalModelOutput emptyDelta = emptyBase.CalculateDelta(emptyBase);
        Assert.Null(emptyDelta.Strain);

        MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.ViscoelasticModelOutput viscoFromBase = new(emptyBase);
        Assert.Null(viscoFromBase.CreepCompliance);

        MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.ViscoelasticModelOutput visco0 = new() { Time = 1.0, CreepCompliance = 0.1, RelaxationFunction = 10.0 };
        MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.ViscoelasticModelOutput visco1 = new() { Time = 2.0, CreepCompliance = 0.2, RelaxationFunction = 20.0 };
        MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.ViscoelasticModelOutput viscoDelta =
            (MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.ViscoelasticModelOutput)visco1.CalculateDelta(visco0);
        Assert.Equal(0.1, viscoDelta.CreepCompliance!.Value, 6);
        Assert.Equal(10.0, viscoDelta.RelaxationFunction!.Value, 6);

        QuasiLinearModelOutput qlFromVisco = new(visco0);
        Assert.Null(qlFromVisco.ReducedRelaxationFunction);

        QuasiLinearModelOutput qlNullDelta = (QuasiLinearModelOutput)new QuasiLinearModelOutput().CalculateDelta(new QuasiLinearModelOutput());
        Assert.Null(qlNullDelta.ReducedRelaxationFunction);
        QuasiLinearModelOutput qlFullDelta = (QuasiLinearModelOutput)q1.CalculateDelta(q0);
        Assert.Equal(10.0, qlFullDelta.StressByReducedRelaxationFunctionDerivative!.Value, 6);

        SchaperyModelOutput sNullDelta = (SchaperyModelOutput)new SchaperyModelOutput().CalculateDelta(new SchaperyModelOutput());
        Assert.Null(sNullDelta.TransientCreepCompliance);

        ModifiedSuperpositionMethodOutput msmNullDelta = (ModifiedSuperpositionMethodOutput)new ModifiedSuperpositionMethodOutput().CalculateDelta(new ModifiedSuperpositionMethodOutput());
        Assert.Null(msmNullDelta.InitialYoungModulus);

        MaxwellModelOutput mNullDelta = (MaxwellModelOutput)new MaxwellModelOutput().CalculateDelta(new MaxwellModelOutput());
        Assert.Null(mNullDelta.RelaxationTime);
    }
}
