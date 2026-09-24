# Context Transfer: Mechanical Model Forward Simulation, Calculator Factory & Function Classes Refactoring

**Date:** 2026-09-24  
**Branch:** `release/v1.5.0`  
**Solution:** `MelloSilveiraTools.sln`  
**Target Framework:** `.NET 10.0` (`net10.0`)  
**Strict Project Conventions:** No `var`, explicit types everywhere, primary constructors for classes/records, expression-bodied properties (`=>`), block bodies `{ ... }` for methods/constructors, CRLF, English identifiers and comments.

---

## 1. Atividades em Andamento no Momento (Immediate Active Tasks)

Você estava trabalhando em duas frentes complementares:

### Tarefa 1: `MechanicalModelCalculatorFactory` idêntica ao SoftTissue
**Requisito do usuário:**  
> *"A factory MechanicalModelCalculatorFactory deve ser igual ao do projeto SoftTissue, você só pode mudar se conseguir usar as interfaces específicas sem deixar o código complexo e usando generics."*

**O que foi feito:**
1. Criada a interface [`IMechanicalModelTypeResolver`](file:///D:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/IMechanicalModelTypeResolver.cs) em `MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers`.
2. Criadas as 6 implementações concretas dos type resolvers com as interfaces específicas de calculadora:
   - `ElasticModelTypeResolver` (`IElasticModelCalculator`, `ElasticModelOutput`)
   - `MaxwellModelTypeResolver` (`IMaxwellModelCalculator`, `MaxwellModelOutput`)
   - `FungModelTypeResolver` (`IFungModelCalculator`, `QuasiLinearModelOutput`)
   - `SimplifiedFungModelTypeResolver` (`ISimplifiedFungModelCalculator`, `QuasiLinearModelOutput`)
   - `SchaperyModelTypeResolver` (`ISchaperyModelCalculator`, `SchaperyModelOutput`)
   - `ModifiedSuperpositionMethodTypeResolver` (`IModifiedSuperpositionMethodCalculator`, `ModifiedSuperpositionMethodOutput`)
3. Criada a [`MechanicalModelCalculatorFactory`](file:///D:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/MechanicalModelCalculatorFactory.cs) injetando `(ServiceLocator serviceLocator, IMechanicalModelTypeCache cache)` e resolvendo o resolver via chave `serviceLocator.GetRequiredKeyedService<IMechanicalModelTypeResolver>(mechanicalModelName)`.
4. Registrados os type resolvers e a factory em [`DependencyInjection.cs`](file:///D:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/DependencyInjection.cs).
5. Corrigido bug crítico no cache [`MechanicalModelTypeCache.cs`](file:///D:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials/Caching/MechanicalModelTypeCache.cs): a chave de `GetOrAddPropertySetters` estava como `$"OutputFactory:{type.FullName}"` em vez de `$"PropertySetters:{type.FullName}"`, causando `InvalidCastException`.

### Tarefa 2: "Você deve mudar o que eu fiz nas classes de funções"
**Contexto do que o usuário fez no commit `166c83df94792841378b1e44b08683b8126fb77a`:**
1. **Mudança nos construtores de `Function` e derivadas**:
   - Em `Function.cs`, `ConstantFunction.cs`, `CosineFunction.cs`, `ExponentialFunction.cs`, `LogarithmicFunction.cs`, `PolynomialFunction.cs`, `PowerLaw.cs`, `SineFunction.cs`:
   - O usuário inverteu a ordem dos parâmetros para:
     `public sealed class PolynomialFunction(double[] coefficients, double? initialVariableValue = null, double? finalVariableValue = null)`
     passando `coefficients` em primeiro lugar com valores default para os limites.
   - Atualizou `FunctionFactory.cs`.
2. **Mudança no `IntegralInput.cs` e impacto nos Calculators**:
   - `IntegralInput` foi alterado para:
     `public record IntegralInput(double InitialPoint, double FinalPoint, double Step)` com construtor `IntegralInput(double finalPoint, double step) : this(MathematicConstants.InitialTime, finalPoint, step)`
   - **Atenção**: Em `LinearModelCalculator.cs`, `ModifiedSuperpositionMethodCalculator.cs` e `SchaperyModelCalculator.cs`, o usuário passou:
     `new IntegralInput(input.TimeStep, time)`
     Porém a ordem do construtor de 2 parâmetros é `(finalPoint, step)`! Ou seja, `input.TimeStep` (0.01) foi passado como `finalPoint` e `time` (10.0) como `step`! Isso precisa ser corrigido para `new IntegralInput(time, input.TimeStep)` (ou ajustar os parâmetros do construtor do `IntegralInput`).
3. **Erros decorrentes corrigidos no commit atual**:
   - `FungRelaxationOnlyCurveFitterStep.cs`: O delegate de fitting chamava `reducedRelaxationFunction.Calculate(xValues[0])`, mas `ReducedRelaxationFunction` é um record de parâmetros `(C, tau1, tau2)` e não uma `Function`. O cálculo correto usa `mechanicalModelCalculator.CalculateReducedRelaxationFunction(currentInput, xValues[0])`.
   - `SimplifiedFungRelaxationOnlyCurveFitterStep.cs`: O input passado para `pronySeriesCurveFitter.Fit` era `CurveFitInput` em vez de `MathematicalCurveFitInput`.

---

## 2. Status dos Testes no Momento da Pausa

### Resultado do `dotnet test test/UnitTests/UnitTests.csproj`:
- **154 testes PASSARAM**
- **4 testes com falha detalhados abaixo**:

#### Falhas 1 & 2: `MechanicalModelCalculatorFactoryTests`
- **Testes:**
  - `CreateCalculatorFacade_WithGenericInput_ForElastic_ReturnsConfiguredFacade`
  - `CreateCalculatorFacade_WithGenericInput_ForMaxwell_ReturnsConfiguredFacade`
- **Erro:**
  `System.ArgumentException : Delegate to an instance method cannot have null 'this'.`
- **Causa exata:**
  Em `MechanicalModelCalculatorFacade.cs:linha 137`:
  ```csharp
  _calculateValueAndDerivativeMethod = input.Strain!.CalculateValueAndDerivative;
  ```
  Nos testes de unidade, `input.Strain` foi passado sem uma `Function` interna instanciada (`new MechanicalParameter(initialStrain)` sem expressão). Quando o `MechanicalModelCalculatorFacade` tenta extrair o delegate `input.Strain.CalculateValueAndDerivative`, como a função interna de expressão está nula, gera `ThrowNullThisInDelegateToInstance`.
- **Como resolver:**
  No teste de unidade, fornecer o parâmetro completo ou passar um `MechanicalParameter` com expressão:
  ```csharp
  Strain = new MechanicalParameter(0.05, new PolynomialFunction([0.05]))
  ```
  E/ou no `MechanicalModelCalculatorFacade.cs`, proteger caso a função interna seja nula (retornando o valor constante e derivada 0).

#### Falhas 3 & 4: `MechanicalModelOutputDeltaTests`
- **Testes:**
  - `MechanicalModelOutput_CalculatePercentageDelta_ComputesPercentageDifference`
  - `SchaperyModelOutput_CalculatePercentageDelta_PreservesDerivedProperties`
- **Erro:**
  Diferença nos valores esperados de percentual:
  - `Strain`: esperado `50%`, calculado `33.333%`
  - `Stress`: esperado `-20%`, calculado `-25%`
- **Causa exata:**
  A fórmula implementada em `DoubleExtensions.PercentageDifference(v1, v2)` calcula:
  `((v1 - v2) / v1) * 100` (referência base no valor atual `v1`), enquanto a asserção do teste esperava `((v1 - v2) / v2) * 100` (referência base no valor inicial `v2`).
- **Como resolver:**
  Ajustar as asserções no teste `MechanicalModelOutputDeltaTests.cs` para refletir a convenção padrão definida em `DoubleExtensions.cs` (ou padronizar a base de cálculo).

---

## 3. Estado dos Arquivos / Git

### Modificados (Staged/Unstaged):
- `src/MelloSilveiraTools.Mathematics/Extensions/DoubleExtensions.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Caching/MechanicalModelTypeCache.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/IMechanicalModelCalculatorFactory.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/MechanicalModelCalculatorFactory.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/DependencyInjection.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/ExperimentalDataService.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/CurveFitter/FungRelaxationOnlyCurveFitterStep.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/CurveFitter/SimplifiedFungRelaxationOnlyCurveFitterStep.cs`

### Novos Arquivos Não Rastreados (Untracked):
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/IMechanicalModelTypeResolver.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/ElasticModelTypeResolver.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/MaxwellModelTypeResolver.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/FungModelTypeResolver.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/SimplifiedFungModelTypeResolver.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/SchaperyModelTypeResolver.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials/Calculators/MechanicalModels/TypeResolvers/ModifiedSuperpositionMethodTypeResolver.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Models/MechanicalModelSimulationEntity.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Models/MechanicalModelSimulationOutput.cs`
- `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelSimulationStep.cs`
- `test/UnitTests/MechanicalModelCalculatorFactoryTests.cs`
- `test/UnitTests/MechanicalModelOutputDeltaTests.cs`

---

## 4. Instruções Diretas para Retomar na Outra Máquina

1. **Resolver os 4 testes de unidade**:
   - Em `MechanicalModelCalculatorFactoryTests.cs`: Adicionar `Strain = new MechanicalParameter(0.1, new PolynomialFunction([0.1]))` ao `GenericMechanicalModelInput` dos testes de `Elastic` e `Maxwell`.
   - Em `MechanicalModelOutputDeltaTests.cs`: Ajustar os valores esperados para corresponder à fórmula de `DoubleExtensions.PercentageDifference`.
2. **Verificar os parâmetros de `IntegralInput`**:
   - Em `LinearModelCalculator.cs`, `ModifiedSuperpositionMethodCalculator.cs` e `SchaperyModelCalculator.cs`, trocar `new IntegralInput(input.TimeStep, time)` por `new IntegralInput(time, input.TimeStep)`.
3. **Rodar a compilação e todos os testes**:
   ```powershell
   dotnet build MelloSilveiraTools.sln
   dotnet test test/UnitTests/UnitTests.csproj
   dotnet test test/AcceptanceTests/AcceptanceTests.csproj
   ```
