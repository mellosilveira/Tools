# Hardening de Segurança Multi-Tenant (`userId` Isolation)

## Contexto Atual (Já Implementado — Não Refazer)
- **PostgreSQL RLS + Particionamento:** `stored_files`, `mechanical_model_curve_fit_outputs`, `mechanical_model_simulations` e `parametric_sweeps` utilizam `PARTITION BY LIST (user_id)` + partições `_default` + `ENABLE`/`FORCE ROW LEVEL SECURITY` (`user_id = public.current_app_user_id()`).
- **Role não-superusuário:** `tools_app` (`NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS`) provisionado em `src/MelloSilveiraTools.Database.Migrator/Scripts/Common/0001_create_users.sql`, com `public.ensure_user_partitions(BIGINT)` (`SECURITY DEFINER`) chamado em `RegisterUserCommand`.
- **Propagação de usuário:** implementação única singleton `CurrentUserProvider : ICurrentUserProvider` (`src/MelloSilveiraTools.Database/Security/CurrentUserProvider.cs`) com `AsyncLocal<long?>` de instância (sem estado estático), populada por `UserContextMiddleware` (`src/MelloSilveiraTools.WebApi/Application/Middlewares/UserContextMiddleware.cs`) e consumida obrigatoriamente por `PostgresRepository` (`SELECT set_config('app.current_user_id', @UserId, false);`).

---

## Objetivo
Fechar as brechas de isolamento multi-tenant identificadas na auditoria de segurança **fora do banco de dados** (storage de arquivos, canais em memória, autenticação JWE, rate limiting e defesa em profundidade), mantendo **0 avisos / 0 erros** no build e todos os testes unitários passando.

---

## Bloco 1 — Isolamento de Storage e Pipelines em Memória (Prioridade Máxima)

### 1. [CRÍTICA] Storage de arquivos sem isolamento por `user_id` + acesso/deleção por `Location` cru
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.Core/Files/Providers/FileStorageServiceBase.cs`
  - `src/MelloSilveiraTools.Core/Files/Providers/Local/LocalFileStorageService.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Create/AddStorageEntryCommand.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Delete/DeleteStorageEntryCommand.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Read/ReadStorageEntriesCommand.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Commands/StorageEntryMetadata/Upload/UpdateStorageEntryFileCommand.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Commands/Process/Single/ExecuteExperimentalDataPipeline.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Commands/Process/Single/ExecuteExperimentalDataPipelineValidator.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/ExperimentalData/Pipelines/Steps/ExperimentalDataSegmenterStep.cs`
- **Vulnerabilidades:**
  1. `LocalFileStorageService` só valida path traversal em `ResolveLocation`, mas `OpenReadAsync`, `OpenWriteAsync`, `ExistsAsync`, `GetMetadataAsync` e `DeleteAsync` aceitam caminhos crus e executam `File.Delete`/`Directory.Delete(location, recursive: true)` diretamente.
  2. `DeleteStorageEntryCommand` aceita `request.Location` cru quando `FileId` não é encontrado/informado, permitindo apagar arquivos de outros usuários ou diretórios arbitrários sem passar pelo RLS de `stored_files`.
  3. `ExecuteExperimentalDataPipeline` / `ExperimentalDataSegmenterStep` recebem `StrainFileLocation` e `StressFileLocation` diretamente no payload JSON e abrem os arquivos sem checar propriedade em `stored_files`, permitindo processar e exfiltrar CSVs de outros usuários.
  4. `AddStorageEntryCommand` / `FileStorageServiceBase` gravam em `storage/{identifier}/{context}/{fileName}` sem prefixo de `user_id`: dois usuários com o mesmo `Identifier`/`Context`/`FileName` sobrescrevem arquivos físicos um do outro.
- **Ajustes necessários:**
  1. Validar em **todos** os métodos `location`-based de `LocalFileStorageService` (e demais providers, quando aplicável) que `Path.GetFullPath(location)` permanece estritamente dentro de `BaseDirectory` (e dentro do escopo do usuário quando autenticado), bloqueando path traversal (`..`, caminhos absolutos fora da raiz, symlinks/escape).
  2. Particionar fisicamente os caminhos de storage por usuário (ex.: `u_{userId}/{identifier}/{context}/{fileName}`) para evitar colisão/sobrescrita entre usuários no disco e em object storage (Blob/S3/DFS).
  3. Em `DeleteStorageEntryCommand`, não permitir deleção arbitrária de caminho físico fora do escopo do usuário; preferir resolução via `FileId` em `stored_files` (RLS) e, caso mantenha fallback por `Location` para idempotência, exigir que o caminho esteja estritamente dentro do diretório do usuário autenticado.
  4. Em `ExecuteExperimentalDataPipeline` (e batch/sweep correlatos), validar que `StrainFileLocation`/`StressFileLocation` pertencem ao usuário atual (idealmente recebendo `StrainFileId`/`StressFileId` e resolvendo `Location` via `stored_files` protegida por RLS, ou verificando existência na tabela `stored_files` filtrada por RLS antes de abrir o stream).

### 2. [ALTA] Canais de progresso em memória sem isolamento por `user_id`
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.Core/Pipelines/Notifiers/ChannelPipelineProgressNotifier.cs`
  - `src/MelloSilveiraTools.MechanicsOfMaterials.Optimizations/Controllers/PipelineProgressController.cs`
- **Vulnerabilidade:** `_channels` é chaveado apenas por `pipelineId` (frequentemente escolhido pelo usuário). Qualquer usuário autenticado que chame `GET /api/v1/pipelines/{pipelineId}/progress` pode ler e consumir (roubar) eventos de progresso/erro de pipelines de outros usuários, ou colidir canais com o mesmo identificador.
- **Ajuste necessário:** escopar os canais de progresso por usuário (chave composta `(userId, pipelineIdentifier)` ou resolução via abstração de contexto de usuário), garantindo que `ReadAllAsync` só acesse canais do usuário autenticado (retornando `404 NotFound` se pertencer a outro usuário).

---

## Bloco 2 — Autenticação, Rate Limiting e Defesa em Profundidade

### 3. [CRÍTICA] Chaves JWE hardcoded em `appsettings.json`
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.WebApi.Host/appsettings.json`
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
- **Vulnerabilidade:** `Jwt:EncryptionKey` e `Jwt:SigningKey` versionadas em texto puro permitem a qualquer pessoa com acesso ao repositório forjar tokens JWE válidos com qualquer `sub` (`user_id`), contornando 100% do RLS.
- **Ajuste necessário:**
  1. Mover segredos de desenvolvimento para `dotnet user-secrets` / `appsettings.Development.json` (ou geração/injeção via variáveis de ambiente em Docker/CI) e remover chaves reais de `appsettings.json`.
  2. Adicionar validação no startup de `WebApi.Host` recusando chaves vazias, com tamanho inválido (< 32 bytes UTF-8 para AES-256) ou valores placeholder quando `!app.Environment.IsDevelopment()`.

### 4. [ALTA] `/api/v1/auth/refresh` não verifica `users.is_active`
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.WebApi/Authentication/Commands/AuthenticationCommands.cs` (`RefreshAuthenticationTokenCommand`)
  - `src/MelloSilveiraTools.WebApi/Authentication/Services/AuthenticationJweTokenService.cs`
- **Vulnerabilidade:** um usuário desativado (`is_active = false`) com token ainda não expirado consegue renovar o token indefinidamente.
- **Ajuste necessário:** no `RefreshAuthenticationTokenCommand`, extrair o `userId` (`sub`), consultar `UserEntity` via `IRepository` e retornar `401 Unauthorized` caso o usuário não exista ou `user.IsActive == false`.

### 5. [ALTA] Rate Limiting nos endpoints de autenticação e mitigação de DoS de DDL no `/register`
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
  - `src/MelloSilveiraTools.WebApi/Application/Controllers/AuthController.cs`
- **Vulnerabilidade:** `/api/v1/auth/register` é `[AllowAnonymous]` e cria 4 partições físicas no PostgreSQL por conta (`ensure_user_partitions`), criando vetor de DoS de catálogo (`pg_class`) e brute-force em `/login`.
- **Ajuste necessário:** configurar `AddRateLimiter` / `UseRateLimiter` (ASP.NET Core nativo) com políticas restritivas por IP para `/api/v1/auth/register`, `/login`, `/refresh` e `/validate`.

### 6. [MÉDIA] Startup guard contra conexão como superusuário / `BYPASSRLS` no PostgreSQL
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
- **Vulnerabilidade:** no PostgreSQL, roles `SUPERUSER` ou `BYPASSRLS` ignoram `FORCE ROW LEVEL SECURITY` silenciosamente. Uma connection string incorreta desativa o RLS sem aviso.
- **Ajuste necessário:** no startup de `WebApi.Host`, executar `SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname = current_user;` e abortar a inicialização (`InvalidOperationException`) se retornar `true` (permitindo opt-out explícito apenas se desejado em cenário controlado de desenvolvimento).

### 7. [MÉDIA] Bloquear controle de `UserId` via JSON em endpoints CRUD e forçar `UserId` no repositório
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.Database/RelationalDatabase/Models/Entities/UserScopedEntityBase.cs`
  - `src/MelloSilveiraTools.Database/RelationalDatabase/Repositories/PostgresRepository.cs` (`ApplyCurrentUserIdParameters`)
- **Ajuste necessário:**
  1. Avaliar `[JsonIgnore]` em `UserScopedEntityBase.UserId` (verificando impacto nos clientes/WebUi) para impedir que o payload HTTP tente definir `userId`.
  2. Em `PostgresRepository.ApplyCurrentUserIdParameters`, decidir se deve **sempre** sobrescrever `@UserId` com `currentUserProvider.GetCurrentUserId()` quando houver usuário autenticado (em vez de apenas quando `== 0`), mantendo o RLS `WITH CHECK` como segunda barreira.

### 8. [MÉDIA] Restringir CORS em produção e documentar isolamento de cache em memória
- **Arquivos afetados:**
  - `src/MelloSilveiraTools.WebApi.Host/Program.cs`
- **Ajuste necessário:** configurar origens permitidas via `appsettings` (`Cors:AllowedOrigins`), evitando `AllowAnyOrigin()` em produção.

---

## Diretrizes Obrigatórias (`AGENTS.md`)
- C# 13 / `.NET 10`: **proibido usar `var`** (tipos explícitos obrigatórios), finais de linha **CRLF**, 4 espaços de indentação, primary constructors, XML docs obrigatórios em todos os tipos e membros `public`, `TimeProvider` obrigatório (sem `DateTime.UtcNow` / `DateTimeOffset.UtcNow` em código de produção).
- Controllers não podem conter lógica de negócio (delegar sempre para `Command` + `IValidator`).
- Não alterar assinaturas públicas sem alinhar antes com o usuário (especialmente se mudar contratos de `IFileStorageService` ou requests HTTP como `ExecuteExperimentalDataPipelineRequest`).
- Criar testes unitários determinísticos cobrindo cada brecha corrigida (path traversal, tentativa de acesso/deleção cross-user, colisão de `ChannelPipelineProgressNotifier`, refresh de usuário inativo, startup guard contra superuser, rate limiting).

---

## Como Executar em Nova Conversa
1. **Para executar o Bloco 1:**
   > Leia `.agents/prompts/multi-tenant-security-hardening.md` e planeje/implemente o **Bloco 1** (itens #1 e #2), propondo primeiro as decisões de contrato antes de editar código.
2. **Para executar o Bloco 2:**
   > Leia `.agents/prompts/multi-tenant-security-hardening.md` e implemente o **Bloco 2** (itens #3 a #8) com testes unitários e validação via `dotnet build` e `dotnet test`.
