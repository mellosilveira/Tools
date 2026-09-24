# Planejamento Técnico: Otimização e Ajuste de Curvas com Restrições de Domínio

Este documento detalha o planejamento arquitetural e técnico para a modernização do módulo de ajuste de curvas (*Curve Fitting*) no ecossistema **MelloSilveiraTools**.

O objetivo é fornecer um sistema agnóstico a casos de uso (Engenharia Mecânica, Eletrônica/Sensores, Finanças/Mercado) que permita lidar com funções contínuas contendo combinações de parâmetros matematicamente possíveis, porém proibidas por regras de consistência de domínio (físicas, de negócio ou operacionais).

---

## 1. Contexto e Diagnóstico

### 1.1. Limitações do Cenário Atual
* **Falha de Solvers de Gradiente:** Atualmente, a base utiliza `alglib.minbleic` (L-BFGS com bounds) e `MathNet.Numerics.Optimization.BfgsMinimizer`. Esses algoritmos dependem do cálculo de derivadas numéricas por diferenças finitas (`CalculateNumericalGradient`).
* **Regras Booleanas Criam "Penhascos":** Ao aplicar regras físicas ou de negócio restritivas (ex.: $E_1 > E_2$, tempos de relaxação ordenados $\tau_1 < \tau_2$, condições de não-arbitragem financeira), o espaço de busca sofre descontinuidades abruptas. Isso faz o gradiente explodir ou divergir para NaN/infinito.
* **Penalidades Analíticas Insuficientes:** A tentativa anterior baseava-se em `EvaluateConstraintsAndPenalties` (funções de penalidade escalar). Além de mascarar a função objetivo original, distorce a convergência do solver se os pesos não forem perfeitamente sintonizados.
* **Ausência de Métrica $R^2$ Nativa:** O critério de qualidade e parada depende apenas da tolerância no erro quadrático, sem expor ou permitir corte pelo Coeficiente de Determinação ($R^2$).

---

## 2. Decisão Arquitetural: Abordagem A (`CurveFitProfile`)

Para que o pacote NuGet seja compreensível para times de produto, desenvolvedores de aplicação e engenheiros sem formação avançada em métodos numéricos, o jargão matemático (*L-BFGS*, *Differential Evolution*, *Simplex*) fica encapsulado sob um enum orientado ao comportamento do problema:

```csharp
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
```

---

## 3. Mapeamento Interno dos Solvers (ALGLIB)

```
[ Usuário / Pipeline ]
         │
         ▼
  ResolveProfile(input)
         │
   ┌─────┴────────────────────────────────┐
   │                                      │
   ▼                                      ▼
Profile == Standard             Profile == RuleConstrained
   │                                      │
   ▼                                      ▼
[ ALGLIB minbleic ]              [ ALGLIB mindf (GDEMO) ]
• L-BFGS com Bounds              • Evolução Diferencial / SHADE
• Rápido, ordem de ms            • Livre de derivadas (Derivative-Free)
• Para curvas contínuas          • Suporta rejeição por barreira (1e12)
• Limites rígidos                • População adaptativa (10 * N)
```

---

## 4. Evolução dos Contratos de Dados

### 4.1. `CurveFitInput`
```csharp
public record CurveFitInput
{
    public required double[] InitialParameters { get; init; }
    public required double[] LowerBounds { get; init; }
    public required double[] UpperBounds { get; init; }
    public required List<double[]> IndependentVariables { get; init; }
    public required double[] DependentVariable { get; init; }
    public required Func<double[], double[], double> Calculate { get; init; }

    /// <summary>
    /// Perfil de otimização selecionado (padrão: Automatic).
    /// </summary>
    public CurveFitProfile Profile { get; init; } = CurveFitProfile.Automatic;

    /// <summary>
    /// Motor de regras booleanas de curto-circuito.
    /// Retorne false para combinações fisicamente ou comercialmente inadmissíveis.
    /// </summary>
    public Func<double[], bool>? ValidateParameters { get; init; }

    /// <summary>
    /// Meta dinâmica de qualidade do ajuste (Coeficiente de Determinação).
    /// </summary>
    public double? TargetRSquared { get; init; }

    public int MaxIterations { get; init; } = CurveFittingConstants.MaxIterations;
    public double Tolerance { get; init; } = CurveFittingConstants.Tolerance;
}
```

### 4.2. `CurveFitOutput`
```csharp
public record CurveFitOutput(
    double[] OptimizedParameters,
    double FinalError,
    double RSquared,
    int Iterations);
```

### 4.3. Cálculo Padronizado de $R^2$ em `CurveFitterBase`
$$R^2 = 1 - \frac{\sum_{i=1}^N (y_i - \hat{y}_i)^2}{\sum_{i=1}^N (y_i - \bar{y})^2} = 1 - \frac{SSR}{SST}$$

Se $SST \le 10^{-15}$ (sinal constante), convenciona-se $R^2 = 1.0$.

---

## 5. Roteiro de Implementação (Roadmap)

### Fase 1: Modelos e Contratos
1. Criar o enum `CurveFitProfile.cs` em `CurveFitting/Models/`.
2. Atualizar `CurveFitInput.cs` substituindo `EvaluateConstraintsAndPenalties` por `ValidateParameters`, adicionando `Profile` e `TargetRSquared`.
3. Atualizar `CurveFitOutput.cs` com o novo campo `double RSquared`.

### Fase 2: Motor de Otimização Numérica no ALGLIB
1. Em `CurveFitterBase.cs`: implementar o cálculo centralizado de $R^2$ (`CalculateRSquared`).
2. Em `AlglibCurveFitter.cs`:
   * Implementar `FitStandard`: isolar o uso do `alglib.minbleic`.
   * Implementar `FitRuleConstrained`:
     * Inicializar estado via `alglib.mindfcreate`.
     * Configurar limites de caixa via `alglib.mindfsetbc`.
     * Configurar escalas de parâmetros via `alglib.mindfsetscale`.
     * Configurar o solver GDEMO via `alglib.mindfsetalgogdemo` com tamanho de população adaptativo ($10 \times N$).
     * No callback `fvec`:
       1. Avaliar `input.ValidateParameters(parameters)`. Se inválido, atribuir barreira estrita (`fi[0] = 1e12`) e retornar imediatamente (*short-circuit* sem calcular resíduos).
       2. Se válido, computar a Soma dos Quadrados dos Resíduos (SSR) e atribuir a `fi[0]`.
     * Processar resultados via `alglib.mindfresults`, calcular o $R^2$ final e retornar o `CurveFitOutput`.

### Fase 3: Integração nos Pipelines de Modelos
1. Atualizar etapas de modelos constitutivos (`FungCurveFitterStep`, `SimplifiedFungCurveFitterStep`, `SchaperyCurveFitterStep`) para injetar suas validações físicas no `ValidateParameters`.
2. Corrigir inconsistências pré-existentes de tipagem identificadas no projeto `Optimizations` para obter compilação limpa (`0 erros`).

### Fase 4: Testes Unitários e Validação
1. **Teste Unitário - Modo Standard:** Validar velocidade e convergência em dados suaves com limites simples.
2. **Teste Unitário - Modo RuleConstrained:**
   * Criar cenário sintético com mínimo irrestrito em região proibida por regra booleana.
   * Comprovar que o GDEMO encontra o melhor ótimo restrito à região válida sem quebrar gradientes.
   * Validar cálculo do $R^2$.
3. Executar a suíte de testes: `dotnet test test/UnitTests/UnitTests.csproj`.
