# Plano de Correções — MelloSilveiraTools Pipeline

> Baseado na revisão de código reportada em `review_report.md`. Itens organizados em 4 fases de acordo com impacto e dependências. Cada fase pode ser executada independentemente após a anterior ser concluída e compilada.

---

## Fase 1 — Bugs Críticos (produção/dados)

> [!CAUTION]
> Estes itens causam deadlock, perda de dados ou vazamento de recursos em produção. Devem ser corrigidos antes de qualquer outra coisa.

### ~~1.1 — `PropagateCompletion = true` em todos os `LinkTo`~~ ✅ Já implementado

`DataflowExtensions.cs` define dois extension methods C# 13 (`extension<TTail>`) que envolvem `ISourceBlock<T>.LinkTo` e forçam `PropagateCompletion = true` como padrão. Item descartado.

---

### 1.2 — `DataflowPipeline<TIn>.DisposeAsync`: Envolver disposal em `try/finally`

**Problema:** Se `await Completion` jogar exceção (ex: step faultado), o bloco `foreach` de disposal nunca é executado → streams, conexões e file handles vazam.

**Arquivo:** [`DataflowPipelineBuilder.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Pipelines/Dataflow/DataflowPipelineBuilder.cs) — linhas 544–556

**Ação:** Refatorar:

```csharp
// ANTES
public async ValueTask DisposeAsync()
{
    Complete();
    await Completion.ConfigureAwait(false);  // ← exceção escapa sem disposal

    foreach (IPipelineStep step in steps) { ... }
}

// DEPOIS
public async ValueTask DisposeAsync()
{
    Complete();
    try
    {
        await Completion.ConfigureAwait(false);
    }
    finally
    {
        foreach (IPipelineStep step in steps)
        {
            if (step is IAsyncDisposable asyncDisposableStep)
            {
                await asyncDisposableStep.DisposeAsync().ConfigureAwait(false);
            }
            else if (step is IDisposable disposableStep)
            {
                disposableStep.Dispose();
            }
        }
    }
}
```

---

### 1.3 — `CurveFitOutputPersistenceStep.cs`: Corrigir serialização polimorfa

**Problema:** `JsonSerializer.Serialize(input.CurveFitOutput.ConstitutiveParameters, JsonOptions)` usa o tipo declarado (`ConstitutiveParameters` — record abstrato vazio) em vez do tipo runtime concreto. Resultado no banco: `"{}"` — todos os parâmetros do modelo são silenciosamente descartados.

**Arquivo:** [`CurveFitOutputPersistenceStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/CurveFitOutputPersistenceStep.cs) — linha 28

**Ação:**

```csharp
// ANTES (linha 28)
string constitutiveParamsJson = JsonSerializer.Serialize(input.CurveFitOutput.ConstitutiveParameters, JsonOptions);

// DEPOIS
ConstitutiveParameters constitutiveParameters = input.CurveFitOutput.ConstitutiveParameters;
string constitutiveParamsJson = JsonSerializer.Serialize(
    constitutiveParameters,
    constitutiveParameters.GetType(),
    _jsonOptions);
```

> [!NOTE]
> O mesmo padrão `(object)` já é usado em `IdentifierBuilderStep.cs` linha 23 como workaround, mas a forma com `.GetType()` é mais explícita e robusta. Ambas devem ser harmonizadas para usar `.GetType()`.

---

### 1.4 — `AddBroadcastStep`: Avaliar comportamento lossy do `BroadcastBlock`

**Problema:** `BroadcastBlock<T>` descarta mensagens se o consumidor estiver com backpressure. O builder retorna o `broadcastBlock` como novo `tailBlock` da pipeline principal, o que significa que mensagens do fluxo principal podem ser perdidas.

**Arquivo:** [`DataflowPipelineBuilder.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Pipelines/Dataflow/DataflowPipelineBuilder.cs) — linhas 388–428

**Ação:** Duas opções — escolher com o time:

- **Opção A (recomendada):** Substituir `BroadcastBlock` por um `BufferBlock` + `ActionBlock` manual para o observer. O `tailBlock` principal continua sendo o `BufferBlock` antes da bifurcação, não o `BroadcastBlock`. Garante entrega sem perdas.
- **Opção B (pragmática):** Documentar explicitamente na API que `AddBroadcastStep` usa semântica *best-effort/fire-and-forget* e não garante entrega. Adicionar `<remarks>` no XML doc e `Debug.Assert` ou log de aviso quando `BoundedCapacity` for excedido.

---

## Fase 2 — Bugs Lógicos (integridade de dados)

> [!WARNING]
> Estes itens produzem resultados incorretos sem erros visíveis — colisões de identidade no banco e falhas silenciosas de processamento.

### 2.1 — `IdentifierBuilderStep.cs`: Expandir o input do hash SHA-256

**Problema:** O hash é calculado apenas sobre `ConstitutiveParameters`. Dois modelos com mesmos parâmetros numéricos mas tipos/configurações diferentes geram o mesmo hash → colisão na `[UniqueColumn] Identifier` de `MechanicalModelCurveFitEntity`.

**Arquivo:** [`IdentifierBuilderStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/IdentifierBuilderStep.cs) — linha 23

**Ação:** Incluir no input do hash todas as dimensões identitárias do fit:

```csharp
public string Execute(MechanicalModelCurveFitOutput output)
{
    ConstitutiveParameters constitutiveParameters = output.ConstitutiveParameters;
    string rawData = string.Concat(
        output.MechanicalModelName,
        output.LoadResponseRelationship.ToString(),
        output.ViscoelasticEffect.ToString(),
        output.RampTimeConsideration.ToString(),
        output.AcceptedRange.InitialPoint.ToString("R"),
        output.AcceptedRange.FinalPoint.ToString("R"),
        JsonSerializer.Serialize(constitutiveParameters, constitutiveParameters.GetType(), _jsonOptions));

    byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
    return Convert.ToHexString(bytes);
}
```

Também aproveitar para usar `JsonSerializer.SerializeToUtf8Bytes` diretamente e evitar a alocação intermediária de string.

---

### 2.2 — `MechanicalModelOutputPersistenceStep.cs`: Expandir o input do hash da simulação

**Problema:** `rawDataToHash` inclui apenas os JSONs dos deltas. Simulações de modelos diferentes com deltas idênticos colidem na `[UniqueColumn] Identifier`.

**Arquivo:** [`MechanicalModelOutputPersistenceStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputPersistenceStep.cs) — linha 35

**Ação:** Incluir `MechanicalModelName`, `CurveFitIdentifier` e `AsymptoteTime` no hash:

```csharp
string rawDataToHash = string.Concat(
    input.MechanicalModelName,
    input.CurveFitIdentifier,
    input.AsymptoteTime?.ToString("R") ?? "null",
    initialOutputJson,
    finalOutputJson,
    absoluteDeltaOutputJson,
    percentageDeltaOutputJson);
```

---

### 2.3 — `MechanicalModelStepFactory.cs`: Tratar `TargetSegments` vazio

**Problema:** `ExperimentalDataProcessingInput.TargetSegments` tem default `= []`. O factory recebe uma lista vazia e lança `ArgumentException` imediatamente, pois não tem case para lista vazia.

**Arquivo:** [`MechanicalModelStepFactory.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Factories/MechanicalModelStepFactory.cs)

**Ação:** Definir um comportamento explícito para lista vazia — por exemplo, usar todos os segmentos suportados pelo modelo (comportamento documentado em `ExperimentalDataProcessingInput.TargetSegments`). Atualizar o switch/matching para tratar `[]` como "usar padrão do modelo".

---

### 2.4 — `ExperimentalDataProcessingPipeline.cs`: Tornar falhas de pipeline observáveis

**Problema:** `.WithLoggingErrors()` absorve todas as exceções, e `ProcessAsync` retorna `Result<string>` de sucesso mesmo que steps tenham falhado. O chamador nunca sabe que dados foram descartados.

**Arquivo:** [`ExperimentalDataProcessingPipeline.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/ExperimentalDataProcessingPipeline.cs) — linhas 51–88

**Ação (duas opções):**

- **Opção A:** Substituir `.WithLoggingErrors()` por `.WithDeadLetterQueue(failedPayload => { /* incrementa contador */ })` e verificar o contador após `await pipeline.Completion`. Retornar `Result.CreateBadRequest(...)` se houver falhas.
- **Opção B:** Remover `.WithLoggingErrors()` e deixar exceções propagarem por `Completion`, capturando-as em `catch` dentro de `ProcessAsync`. Retornar `Result.CreateInternalServerError(...)`.

Também verificar o retorno de `await pipeline.SendAsync(...)` — se `false`, o pipeline não aceitou o item (backpressure ou pipeline cancelado).

---

## Fase 3 — Convenções do Projeto

> [!NOTE]
> Violações das regras definidas em `AGENTS.md` e `.editorconfig`. Não afetam comportamento em produção mas são obrigatórias pelo padrão do projeto.

### 3.1 — `DataflowPipelineBuilder.cs`: Substituir `var` por tipos explícitos

**Arquivo:** [`DataflowPipelineBuilder.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Pipelines/Dataflow/DataflowPipelineBuilder.cs)

| Linha | Atual | Correto |
|---|---|---|
| 125 | `var builder1 = new DataflowPipelineBuilder<...>(...)` | `DataflowPipelineBuilder<THead, TTail> builder1 = new(...)` |
| 126 | `var builder2 = ...` | idem |
| 129 | `var out1Builder = branch1(builder1)` | `IDataflowPipelineBuilder<THead, TOut1> out1Builder = branch1(builder1)` |
| 130 | `var out2Builder = ...` | `IDataflowPipelineBuilder<THead, TOut2> out2Builder = ...` |
| 159 | `var builder1 = ...` | `DataflowPipelineBuilder<THead, TTail> builder1 = new(...)` |
| 160 | `var builder2 = ...` | idem |
| 161 | `var builder3 = ...` | idem |

---

### 3.2 — `DataflowPipelineBuilder.cs` + `ExperimentalDataProcessingPipeline.cs`: Converter expression-bodied methods para block bodies

**Regra:** `csharp_style_expression_bodied_methods = false`

| Arquivo | Linha | Atual | Correto |
|---|---|---|---|
| `DataflowPipelineBuilder.cs` | 254 | `AddCollectAllStep(...) => ...` | Block body com `return` |
| `DataflowPipelineBuilder.cs` | 486 | `WithDeadLetterQueue(...) => new(...)` | Block body com `return` |
| `DataflowPipelineBuilder.cs` | 521 | `GetTelemetryName(...) => $"..."` | Block body com `return` |
| `DataflowPipelineBuilder.cs` | 531 | `SendAsync(...) => headBlock.SendAsync(...)` | Block body com `return` |
| `ExperimentalDataProcessingPipeline.cs` | 156 | `ToSegmenterInput() => new(...)` | Block body com `return` |

---

### 3.3 — `DataflowPipelineBuilder.cs`: Adicionar chaves obrigatórias em control flow

**Regra:** `csharp_prefer_braces = true`

```csharp
// ANTES (linhas 308–311)
if (safeResult.Success)
    await source.SendAsync(...);
else
    await deadLetterQueueBlock!.SendAsync(...);

// DEPOIS
if (safeResult.Success)
{
    await source.SendAsync(...);
}
else
{
    await deadLetterQueueBlock!.SendAsync(...);
}
```

Mesma correção nas linhas 551–554 (disposal por tipo).

---

### 3.4 — Três persistence steps: Renomear `JsonOptions` para `_jsonOptions`

**Regra:** `_privateField` para campos privados.

**Arquivos:**
- [`IdentifierBuilderStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/IdentifierBuilderStep.cs) linha 15
- [`CurveFitOutputPersistenceStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/CurveFitOutputPersistenceStep.cs) linha 20
- [`MechanicalModelOutputPersistenceStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputPersistenceStep.cs) linha 22

---

### 3.5 — Centralizar `JsonSerializerOptions` duplicado

**Problema:** A mesma definição `{ Converters = { new SignificantFiguresDoubleJsonConverter(7) } }` está triplicada em três classes.

**Ação:** Criar classe estática interna no namespace `Pipelines.ExperimentalData`:

```csharp
// Novo arquivo: Pipelines/ExperimentalData/OptimizationJsonOptions.cs
namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData
{
    internal static class OptimizationJsonOptions
    {
        /// <summary>
        /// Shared JSON serialization options for optimization pipeline persistence steps,
        /// using 7 significant figures for double values.
        /// </summary>
        internal static readonly JsonSerializerOptions SignificantFigures7 = new()
        {
            Converters = { new SignificantFiguresDoubleJsonConverter(7) }
        };
    }
}
```

Substituir todas as 3 ocorrências por `OptimizationJsonOptions.SignificantFigures7`.

---

### 3.6 — Dois persistence steps: Substituir `new Exception(...)` por `new InvalidOperationException(...)`

**Arquivos:**
- [`CurveFitOutputPersistenceStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/CurveFitOutputPersistenceStep.cs) linha 51
- [`MechanicalModelOutputPersistenceStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputPersistenceStep.cs) linha 59

```csharp
// ANTES
throw new Exception(string.Join(Environment.NewLine, insertResult.Messages));

// DEPOIS
throw new InvalidOperationException(string.Join(Environment.NewLine, insertResult.Messages));
```

---

### 3.7 — Todos os arquivos revisados: Converter namespaces file-scoped para block-scoped

**Regra:** `csharp_style_namespace_declarations = block_scoped`

**Arquivos afetados (13):**

| Camada Core | Camada Optimizations |
|---|---|
| `IPipelineStep.cs` | `ExperimentalDataProcessingPipeline.cs` |
| `IAsyncPipelineStep.cs` | `IdentifierBuilderStep.cs` |
| `ISyncPipelineStep.cs` | `CurveFitOutputPersistenceStep.cs` |
| `IAsyncEnumerablePipelineStep.cs` | `MechanicalModelOutputPersistenceStep.cs` |
| `IDataflowPipelineBuilder.cs` | `MechanicalModelOutputPersistenceInput.cs` |
| `DataflowPipelineBuilder.cs` | `SimulationDeltaOutput.cs` |
| | `MechanicalModelCurveFitOutput.cs` |

**Ação:** Para cada arquivo, converter `namespace X.Y.Z;` para `namespace X.Y.Z { ... }` envolvendo todo o conteúdo.

---

### 3.8 — `IDataflowPipelineBuilder.cs`: Corrigir nomes de parâmetros inconsistentes com implementação

| Interface | Implementação | Correção |
|---|---|---|
| `WithDeadLetterQueue(... errorHandlerAsync)` | `DataflowPipelineBuilder.cs: errorHandler` | Alinhar para `errorHandler` em ambos |
| `AddGroupWhileStep(... condition)` | `DataflowPipelineBuilder.cs: groupingCondition` | Alinhar para `groupingCondition` em ambos |

---

## Fase 4 — Qualidade e Documentação

> [!TIP]
> Itens que melhoram manutenibilidade, legibilidade e conformidade com o padrão XML doc do projeto.

### 4.1 — `MechanicalModelCurveFitOutput.cs`: Renomear `Precision` para `RSquared`

**Problema:** `AGENTS.md` e `CurveFitOutput.cs` usam `RSquared`. Aqui está como `Precision`, criando inconsistência de nomenclatura no domínio. Além disso, `&sup2;` é entidade HTML inválida em XML doc (gera `CS1570`).

**Arquivo:** [`MechanicalModelCurveFitOutput.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Models/MechanicalModelCurveFitOutput.cs) — linhas 17, 28

**Ação:**
- Renomear parâmetro `Precision` → `RSquared`
- Corrigir doc: `/// <param name="RSquared">The coefficient of determination (R²) of the fit.</param>`
- Atualizar todos os usos de `.Precision` no projeto para `.RSquared` (principalmente `CurveFitOutputPersistenceStep` — para adicionar a coluna na entidade também)

---

### 4.2 — `ExperimentalDataProcessingPipeline.cs`: Remover dead code e extrair tipos para arquivos próprios

**Problema:** `CurveSegmentBuilderInput` é declarada mas nunca usada. Além disso, `ExperimentalDataProcessingInput` e `ExperimentalDataSegmenterInput` estão no mesmo arquivo da pipeline.

**Ação:**
1. **Deletar** `CurveSegmentBuilderInput` (linhas 168–173)
2. **Mover** `ExperimentalDataProcessingInput` → `Models/ExperimentalDataProcessingInput.cs`
3. **Mover** `ExperimentalDataSegmenterInput` → `Models/ExperimentalDataSegmenterInput.cs`

---

### 4.3 — `IDataflowPipelineBuilder.cs`: Completar XML docs faltantes

**Ação:** Adicionar em todos os métodos que estiverem incompletos:

- `<returns>` em todos os builders (retorna `IDataflowPipelineBuilder<THead, ...>` para encadeamento fluente) e terminais (retorna `IDataflowPipeline<THead>`)
- `<typeparam name="TNextOut">`, `<typeparam name="TOut1">`, etc. nos métodos genéricos
- `<param>` nos overloads de `Fork` que recebem steps (parâmetros `branch1Step`, `branch2Step`, `branch3Step`, `options`)

---

### 4.4 — `IAsyncPipelineStep.cs`: Corrigir documentação copy-paste da variante sem output

**Problema:** `IAsyncPipelineStep<in TIn>` (consumer/terminal) foi documentado com texto de `IAsyncPipelineStep<TIn, TOut>`, mencionando "output state" e "mutated terminal state" — semanticamente incorreto.

**Arquivo:** [`IAsyncPipelineStep.cs`](file:///D:/Mello Silveira Serviços LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Pipelines/Steps/IAsyncPipelineStep.cs) — linhas 3–16

**Ação:** Reescrever summary e `<returns>` para refletir que este contrato é um *consumer/side-effect* terminal sem retorno de payload.

---

### 4.5 — Adicionar `sealed` nas classes/records sem herança intencional

| Arquivo | Tipo |
|---|---|
| `IdentifierBuilderStep.cs` | `public sealed class` |
| `CurveFitOutputPersistenceStep.cs` | `public sealed class` |
| `MechanicalModelOutputPersistenceStep.cs` | `public sealed class` |
| `MechanicalModelCurveFitOutput.cs` | `public sealed record` |
| `SimulationDeltaOutput.cs` | `public sealed record` |
| `MechanicalModelOutputPersistenceInput.cs` | `public sealed record` |
| `ExperimentalDataProcessingInput.cs` (após extração) | `public sealed record` |
| `ExperimentalDataSegmenterInput.cs` (após extração) | `public sealed record` |

---

### 4.6 — Corrigir mensagens de log e TODOs

| Arquivo | Linha | Atual | Correto |
|---|---|---|---|
| `DataflowPipelineBuilder.cs` | 10 | `// TODO: ADICIONAR CIRCUIT BREAKER` | `// TODO: Add circuit breaker` |
| `ExperimentalDataProcessingPipeline.cs` | 41 | `// TODO: ADICIONAR LOG PARA SEGMENTOS IGNORADOS.` | `// TODO: Add log for ignored segments.` |
| `CurveFitOutputPersistenceStep.cs` | 46 | `"already exist on database"` | `"already exists in the database"` |
| `MechanicalModelOutputPersistenceStep.cs` | 54 | `"already exist on database"` | `"already exists in the database"` |

---

### 4.7 — Completar `<param>` tags faltantes

| Arquivo | Membros sem doc |
|---|---|
| `ExperimentalDataProcessingPipeline.cs` (constructor) | `loggerFactory`, `fileManager`, `differentiation`, `stepFactory`, `repository`, `calculatorFactory`, `settings` |
| `ExperimentalDataSegmenterInput.cs` | `StartExperimentalTimeThreshold` (tag vazia) |
| `MechanicalModelOutputPersistenceInput.cs` | `MechanicalModelName`, `CurveFitIdentifier`, `AsymptoteTime`, `Delta`, `FileData` |
| `CurveFitOutputPersistenceStep.cs` | `logger`, `repository` (no constructor doc) |
| `MechanicalModelOutputPersistenceStep.cs` | `logger`, `repository` (no constructor doc) |

---

## Resumo por Arquivo

| Arquivo | Fase 1 | Fase 2 | Fase 3 | Fase 4 |
|---|:---:|:---:|:---:|:---:|
| `DataflowPipelineBuilder.cs` | ✅ 1.1, 1.2 | — | ✅ 3.1, 3.2, 3.3, 3.7 | ✅ 4.6 |
| `CurveFitOutputPersistenceStep.cs` | ✅ 1.3 | — | ✅ 3.4, 3.6, 3.7 | ✅ 4.5, 4.6, 4.7 |
| `IdentifierBuilderStep.cs` | — | ✅ 2.1 | ✅ 3.4, 3.7 | ✅ 4.5 |
| `MechanicalModelOutputPersistenceStep.cs` | — | ✅ 2.2 | ✅ 3.4, 3.6, 3.7 | ✅ 4.5, 4.6, 4.7 |
| `MechanicalModelStepFactory.cs` | — | ✅ 2.3 | — | — |
| `ExperimentalDataProcessingPipeline.cs` | — | ✅ 2.4 | ✅ 3.2, 3.7 | ✅ 4.2, 4.6, 4.7 |
| `MechanicalModelCurveFitOutput.cs` | — | — | ✅ 3.7 | ✅ 4.1, 4.5 |
| `MechanicalModelOutputPersistenceInput.cs` | — | — | ✅ 3.7 | ✅ 4.5, 4.7 |
| `SimulationDeltaOutput.cs` | — | — | ✅ 3.7 | ✅ 4.5 |
| `IDataflowPipelineBuilder.cs` | — | — | ✅ 3.7, 3.8 | ✅ 4.3 |
| `IPipelineStep.cs` / `IAsync...` / `ISync...` / `IAsyncEnumerable...` | — | — | ✅ 3.7 | ✅ 4.4 |
| `OptimizationJsonOptions.cs` (novo) | — | — | ✅ 3.5 | — |
