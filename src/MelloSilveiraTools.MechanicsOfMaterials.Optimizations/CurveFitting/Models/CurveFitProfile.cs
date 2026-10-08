namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Define o perfil de comportamento e restrições para o ajuste de curvas.
/// </summary>
public enum CurveFitProfile
{
    /// <summary>
    /// Seleção automática. Se houver regras de validação booleana informadas,
    /// assume RuleConstrained; caso contrário, Standard.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Ajuste analítico rápido para funções suaves e limites simples de caixa (Min/Max).
    /// Execução na ordem de milissegundos via L-BFGS.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Busca global estocástica (Evolução Diferencial / GDEMO).
    /// Suporta validações booleanas de consistência e descarta combinações inválidas sem quebrar derivadas.
    /// </summary>
    RuleConstrained = 2
}
