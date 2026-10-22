# Planejamento Arquitetural e de Implementação: Armazenamento Agnóstico, Persistência de Pipelines e Exposição via Web API

## 1. Visão Geral e Diretrizes de Design Acordadas

Este documento consolida todas as decisões arquiteturais, o estado atual do código na branch `feature/thesis-soft-tissue` e o planejamento faseado para conclusão da infraestrutura de arquivos, persistência em banco de dados, rastreamento em tempo real e exposição via Web API.

### 1.1 Regras de Ouro e Decisões do Usuário
1. **Separação Estrita de Responsabilidades em Arquivos (SRP):**
   - **`IFileStorageService`**: É o **único** componente que sabe **onde** o arquivo está armazenado (`{BaseDirectory}/{context}/{identifier}/{fileName}`), resolvendo caminhos, abrindo `Stream` para escrita/leitura e fornecendo `FileMetadata`.
   - **`CsvFileWriter`**: Sabe apenas **como** escrever um CSV. Possui um único construtor primário `CsvFileWriter(Stream stream, string header)` e o método privado `CreateWriter(Stream stream, string header)`. Não conhece caminhos de arquivo, não depende de `IFileStorageService` e não contabiliza linhas (`RowCount`).
   - **`CsvStreamReader`**: Sabe apenas **como** ler uma `Stream` de CSV (permanece inalterado).
   - **Proibição de Legado e Contagem de Linhas**: `IFileManager`, `FileManager`, `FileData`, `RowCount` e `Checksum` não devem existir em nenhuma camada (nem em modelos, nem em entidades, nem em steps).
2. **Injeção de Dependência (`IFileStorageService`):**
   - `StorageProvider` é um `enum` (`Local = 1`, `BlobStorage = 2`, `DistributedFileSystem = 3`).
   - `IFileStorageService` não expõe propriedades de infraestrutura (`StorageProvider` ou `BaseDirectory`).
   - Cada implementação é registrada como **Keyed Service** de `IFileStorageService` usando a chave `StorageProvider` correspondente (com resolução padrão apontando para `StorageProvider.Local`).
3. **Governança de Diretórios de Saída:**
   - Nesta versão, **o usuário não informa pasta de saída** (`OutputDirectory` / `OutputFileUri`) nas requisições. A aplicação controla integralmente onde os arquivos são salvos através do `IFileStorageService`.
4. **Convenções de Código (`.editorconfig` / `AGENTS.md`):**
   - Proibição absoluta de `var` (tipagem explícita obrigatória).
   - Preferência por construtores primários.
   - Corpos em bloco `{ ... }` para métodos e construtores; expression bodies `=>` para propriedades.
   - Documentação XML completa em todos os tipos e membros públicos.

---

## 2. Estado Atual da Implementação (O que já foi feito)

### 2.1 Concluído em `MelloSilveiraTools.Core`
- [`StorageProvider.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/StorageProvider.cs): Enum criado (`Local`, `BlobStorage`, `DistributedFileSystem`).
- [`FileMetadata.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/FileMetadata.cs): Record construído via construtor único que extrai `DirectoryPath` e `FileName` simultaneamente a partir de `FullPath` com uma única chamada a `LastIndexOfAny(['/', '\\'])`, contendo `FullPath`, `FileName`, `DirectoryPath`, `SizeBytes` e `CreatedAt`.
- [`FileStorageSettings.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/FileStorageSettings.cs): Record de configuração com `BaseDirectory` padrão.
- [`IFileStorageService.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/IFileStorageService.cs) e [`LocalFileStorageService.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/LocalFileStorageService.cs): Implementados e registrados como Keyed Singleton em [`DependencyInjection.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/DependencyInjection.cs).
- [`CsvFileWriter.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Core/Managers/File/CsvFileWriter.cs): Refatorado para receber `Stream stream, string header` no construtor primário.
- `IFileManager`, `FileManager` e `FileData` removidos.

### 2.2 Concluído em `MelloSilveiraTools.Database`
- [`StoredFileEntity.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.Database/RelationalDatabase/Models/Entities/StoredFileEntity.cs): Entidade genérica mapeada para a tabela `stored_files` contendo `ExternalIdentifier`, `Context`, `StorageProvider`, `FileName`, `Location`, `SizeBytes` e `ContentType`.
- Suporte a propriedades `Enum` adicionado em `TypeExtensions.IsSimpleType` e `ClassExtensions` para geração de SQL e parâmetros Dapper.

---

## 3. Ponto de Ajuste Imediato: Por que `ExperimentalDataFileWriterStep` ainda não está no formato ideal

Na implementação atual de [`ExperimentalDataFileWriterStep.cs`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/ExperimentalDataFileWriterStep.cs), ainda restaram problemas de design que violam a separação de responsabilidades desejada:

1. **Vazamento de responsabilidade de caminho dentro do Step:**
   O step ainda recebe `string outputFileUri` e possui um método privado `ResolveLocation(...)` fazendo verificações manuais (`Path.HasExtension`, `Path.IsPathRooted`, `Path.Combine`) e sendo chamado **duas vezes** na inicialização dos campos `_filePath` e `_writer`:
   ```csharp
   // PROBLEMA ATUAL:
   private readonly string _filePath = ResolveLocation(fileStorageService, outputFileUri, identifier);
   private readonly CsvFileWriter _writer = new(fileStorageService.OpenWrite(ResolveLocation(fileStorageService, outputFileUri, identifier)), CsvHeader);
   ```
2. **Como deve ser (Design Limpo):**
   - Se quem sabe onde o arquivo está é o `IFileStorageService` e o usuário não informa `outputFileUri`:
     - **Alternativa A (Step recebe `IFileStorageService` e `identifier`):**
       O step usa diretamente `fileStorageService.OpenWrite(Context, identifier, fileName)` e `fileStorageService.GetMetadata(Context, identifier, fileName)`, sem nenhum método `ResolveLocation`, sem `outputFileUri` e sem duplicação de chamadas:
       ```csharp
       public sealed class ExperimentalDataFileWriterStep(
           IFileStorageService fileStorageService,
           string identifier)
           : IAsyncPipelineStep<SegmentedDataPoint>
       {
           private const string Context = "ExperimentalData";
           private const string CsvHeader = "Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration";

           private readonly CsvFileWriter _writer = new(
               fileStorageService.OpenWrite(Context, identifier, $"{identifier}{FileExtensions.CommaSeparatedValues}"),
               CsvHeader);

           /// <inheritdoc />
           public string Name => nameof(ExperimentalDataFileWriterStep);

           /// <summary>
           /// Gets the metadata of the file generated by this writer step.
           /// </summary>
           public FileMetadata FileMetadata => fileStorageService.GetMetadata(
               Context,
               identifier,
               $"{identifier}{FileExtensions.CommaSeparatedValues}");
           // ...
       }
       ```
     - **Alternativa B (`IFileStorageService.OpenWrite` ou sobrecarga com `out FileMetadata` / `Stream` preparada fora do Step):**
       O pipeline pede ao `IFileStorageService` a `Stream` (ou localização) e passa ao Step, mantendo o Step 100% focado na escrita.

---

## 4. Planejamento Faseado para a Nova Conversa

### Fase 2 (Conclusão): Alinhamento Final dos Steps de Escrita CSV e Inputs de Pipeline
1. **Remover `OutputFileUri` / `OutputDirectory` dos Inputs:**
   - Remover `OutputFileUri` de [`ExperimentalDataProcessingInput`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Models/ExperimentalDataProcessingInput.cs).
   - Remover `OutputDirectory` de `ParametricSweepInput`.
2. **Padronizar os 3 Steps que utilizam `CsvFileWriter`:**
   - [`ExperimentalDataFileWriterStep`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/ExperimentalDataFileWriterStep.cs):
     - Único construtor primário, sem `outputFileUri`, sem `ResolveLocation`, sem tupla `_context`, sem `GetWriter()`, expondo apenas a propriedade `FileMetadata`.
   - [`MechanicalModelOutputFileWriterStep`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/ExperimentalData/Steps/MechanicalModelOutputFileWriterStep.cs):
     - Remover `outputFileUri`, `GeneratedFilePaths` e `GeneratedFilesMetadata` desnecessários; utilizar `fileStorageService.OpenWrite(Context, uniqueIdentifier, fileName)` e retornar `FileMetadata` no `ExecuteAsync`.
   - [`ParametricSweepCsvWriterStep`](file:///d:/Mello%20Silveira%20Servi%C3%A7os%20LTDA/Projetos/Tools/src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Pipelines/SensitivityAnalysis/Steps/ParametricSweepCsvWriterStep.cs):
     - Receber `IFileStorageService` e `string identifier` (mandatório), resolvendo cada arquivo de variável via `fileStorageService.OpenWrite("SensitivityAnalysis", identifier, fileName)` e expondo `IReadOnlyCollection<FileMetadata> GeneratedFilesMetadata`.

### Fase 3: Persistência em Banco de Dados nos Pipelines (`ExperimentalData` e `ParametricSweep`)
1. **`ExperimentalDataProcessingPipeline`:**
   - Garantir que `MechanicalModelSimulationEntity` armazene o `ExternalIdentifier` (`uniqueIdentifier` da execução).
   - Persistir em `stored_files` (`StoredFileEntity`) tanto o arquivo de pontos experimentais processados (`ExperimentalDataFileWriterStep.FileMetadata`) quanto os arquivos de simulação numérica (`MechanicalModelOutputPersistenceStep`).
2. **`ParametricSweepPipeline`:**
   - Criar a entidade `ParametricSweepEntity` (mapeada para `parametric_sweeps`) contendo:
     - `ExternalIdentifier`
     - `MechanicalModelName`
     - `BaseStrain`, `InitialTime`, `FinalTime`, `TimeStep`, `AsymptoteTolerance`
     - `BaselineParametersJson` e `VariableRangesJson` (serializados com `OptimizationJsonOptions.SignificantFigures7`).
   - Criar o passo terminal `ParametricSweepPersistenceStep` para persistir `ParametricSweepEntity` e registrar todos os CSVs gerados (`GeneratedFilesMetadata`) na tabela `stored_files` (`StoredFileEntity` com `Context = "SensitivityAnalysis"`).

### Fase 4: Notificação de Progresso da Pipeline em Tempo Real (`MelloSilveiraTools.Core.Pipelines`)
1. **Modelos de Progresso no Core:**
   - Criar `PipelineStepState` (`Started`, `Running`, `Completed`, `Faulted`) e `PipelineProgressEvent(string StepName, string Description, PipelineStepState State, int? StepIndex, int? TotalSteps, string? Detail, DateTimeOffset Timestamp)`.
2. **Integração nos Pipelines:**
   - Adicionar `IProgress<PipelineProgressEvent>? Progress { get; init; }` nos inputs dos pipelines (ou integração direta no builder/telemetria do `DataflowPipelineBuilder`) para emitir eventos em tempo real à medida que cada passo inicia, processa e finaliza.

### Fase 5: Commands, Validadores e Endpoints Web API
1. **Commands e DTOs (`CommandBaseWithData<TRequest, TResponse>`):**
   - **`ProcessExperimentalDataCommand`**:
     - `ProcessExperimentalDataRequest`: Recebe `IFormFile StrainFile`, `IFormFile StressFile`, `MechanicalModelName`, `TargetRSquared` e opcionais anuláveis (sem pasta de saída).
     - `ProcessExperimentalDataResponse`: Retorna `Identifier`, metadados dos arquivos gerados e resumo das simulações.
   - **`ExecuteParametricSweepCommand`**:
     - `ExecuteParametricSweepRequest`: DTO JSON puro (`application/json`, sem `IFormFile` e sem pasta de saída) contendo `MechanicalModelName`, `Baseline`, `ConstantVariables`, `ArrayVariables`, `StrainRange` e parâmetros temporais.
     - `ExecuteParametricSweepResponse`: Retorna `Identifier` e lista de `FileMetadata` gerados.
   - **`FitCurveCommand`** (revisão e alinhamento ao padrão).
2. **Controllers e Streaming em Tempo Real:**
   - Endpoints REST tradicionais (`POST`) retornando `Result<TResponse>`.
   - Endpoints de Streaming (`POST .../stream`) utilizando `WriteNdjsonAsync` / Server-Sent Events (SSE) para transmitir cada `PipelineProgressEvent` em tempo real ao cliente até a conclusão da pipeline.
   - Endpoint de consulta/download de arquivos gerados a partir do `ExternalIdentifier` consultando `StoredFileEntity` e lendo a `Stream` via `IFileStorageService.OpenRead`.

### Fase 6: Testes Unitários e de Integração
1. Testes unitários cobrindo `ExperimentalDataFileWriterStep`, `MechanicalModelOutputFileWriterStep`, `ParametricSweepCsvWriterStep` e `ParametricSweepPersistenceStep`.
2. Testes unitários para os Commands, Validadores e notificação de progresso em tempo real.
3. Validação final de build (`0 erros`) e bateria completa de testes (`dotnet test`).
