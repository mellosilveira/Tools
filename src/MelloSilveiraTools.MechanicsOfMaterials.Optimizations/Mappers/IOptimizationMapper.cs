using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;

public interface IOptimizationMapper
{
    // Extrai as propriedades do record para um array plano para iniciar o solver
    double[] ExtractOptimizableParameters(ConstitutiveParameters input);

    // Injeta o array plano do solver de volta em um novo record imutável
    ConstitutiveParameters MapToConstitutiveParameters(double[] parameters);
}
