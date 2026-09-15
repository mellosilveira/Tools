namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;

public interface IOptimizationMapper
{
    // Extrai as propriedades do record para um array plano para iniciar o solver
    double[] ExtractOptimizableParameters<TConstitutiveParameters>(TConstitutiveParameters input);

    // Injeta o array plano do solver de volta em um novo record imutável
    TConstitutiveParameters MapToConstitutiveParameters<TConstitutiveParameters>(double[] parameters);
}
