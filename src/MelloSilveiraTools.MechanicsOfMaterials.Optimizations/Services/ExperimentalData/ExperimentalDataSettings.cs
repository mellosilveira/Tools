namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

public record ExperimentalDataSettings
{
    public int FileWriterBoundedCapacity { get; init; } = 10000;
}
