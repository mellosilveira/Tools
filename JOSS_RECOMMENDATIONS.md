# Recomendações e Roteiro de Submissão JOSS — MelloSilveiraTools

Este documento consolida o diagnóstico completo de conformidade, as decisões arquiteturais e as pendências para a submissão formal do **MelloSilveiraTools** ao *Journal of Open Source Software* (JOSS). Ele serve como guia de continuidade para desenvolvimento em outro dispositivo.

---

## 1. Identificação e Metadados do Projeto

- **Nome do Projeto:** `MelloSilveiraTools`
- **Autores:**
  - Bruno Mello Silveira (ORCID: `0000-0001-6793-9387`) — Autor Correspondente (`affiliation: "1, 2"`)
  - Prof. Dr. Paulo Pedro Kenedi (ORCID: `0000-0001-5563-843X`) — Coautor / Orientador (`affiliation: 1`)
- **Afiliações Institucionais:**
  1. *Graduate Program in Mechanical Engineering and Materials Technology (PPEMM), Universidade Federal de Ciência e Inovação do Rio de Janeiro (UFCIRJ), Rio de Janeiro, Brazil*
  2. *Mello Silveira Serviços LTDA, Pinheiros, São Paulo, Brazil*
- **Licença:** GNU General Public License v3.0 or later (`GPL-3.0-or-later`), compatível com o código C# embarcado do ALGLIB Free Edition (`GPL-2.0-or-later`).
- **Público-Alvo Acadêmico:** Pesquisadores em biomecânica de tecidos moles (ligamentos do joelho e tendões), reologia, mecânica dos materiais e otimização numérica.
- **Contexto da Pesquisa:** Dissertação de Mestrado no PPEMM/UFCIRJ: *Viscoelastic Characterization and Load-Sharing Modeling of Knee Ligaments: A Unified Computational Framework*.

---

## 2. Status Atual da Solução (.NET 10 / C# 13)

- **Solução:** `MelloSilveiraTools.sln`
- **Compilação:** **0 Erros, 0 Avisos** em todos os 9 projetos (`net10.0`, AOT-clean, nullable reference types habilitados).
- **Testes Unitários:** **449 testes unitários** passando em ~4 segundos sem dependências de infraestrutura (`dotnet test test/UnitTests/UnitTests.csproj`).
- **Testes de Aceitação / Integração:** **23 testes ponta a ponta** com dados reais de ligamentos de joelho (ACL, PCL, MCL, LCL) utilizando Testcontainers para PostgreSQL (`dotnet test test/AcceptanceTests/AcceptanceTests.csproj` — requer Docker).
- **Documentação de Código:** 100% dos tipos e membros públicos com documentação XML gerada em build (`GenerateDocumentationFile = true`).
- **Arquivos Comunitários:** `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1) e templates de issues em `.github/ISSUE_TEMPLATE/` presentes na raiz.

---

## 3. Estratégia de Demonstração / Quickstart: Web API + Frontend

> [!IMPORTANT]
> **Decisão de Arquitetura:** Não será criado um projeto de console simples para o Quickstart. A experiência interativa de demonstração para revisores da JOSS e usuários finais será provida por uma **Web API funcional** e um **Frontend leve e moderno**, integrando o ecossistema completo.

### 3.1. Web API de Demonstração (Backend)
- **Objetivo:** Expor os pipelines de otimização de dados experimentais e varredura paramétrica de sensibilidade via HTTP REST e streaming NDJSON/SSE.
- **Endpoints Chave a disponibilizar:**
  - `POST /api/v1/experimental-data/process`: Ingestão assíncrona de pares de arquivos `strain.csv` e `stress.csv`, retornando `202 Accepted` com identificador de execução.
  - `POST /api/v1/experimental-data/process/batch`: Execução em lote para múltiplas pastas de corpos de prova (ACL, PCL, MCL, LCL), com concorrência configurável (`MaxDegreeOfParallelism`).
  - `POST /api/v1/parametric-sweeps/process-from-experimental-data`: Disparo de varreduras de sensibilidade sobre os parâmetros viscoelásticos calibrados.
  - `GET /api/v1/pipelines/{id}/progress`: Streaming em tempo real de eventos de progresso e topologia de DAG via NDJSON (`application/x-ndjson`).
  - `GET /swagger`: Interface interativa OpenAPI/Swagger para testes diretos pelo navegador.
- **Dados Amostrais Embarcados:**
  - Incluir pasta com amostras de teste (ex.: `data/samples/knee-ligaments/` com arquivos sintéticos ou reais normalizados de deformação e tensão).

### 3.2. Frontend Web Leve (Interface do Usuário)
- **Objetivo:** Fornecer uma interface visual acessível para pesquisadores sem perfil técnico de programação em C#.
- **Funcionalidades Chave:**
  - Upload de arquivos CSV ou seleção com 1 clique de conjuntos de dados pré-carregados (amostras de ACL, PCL, etc.).
  - Gráfico interativo de dispersão e segmentação automática das fases de ensaio (`Ramp`, `Relaxation`, `Descent`, `Recovery`).
  - Comparação visual em tempo real entre a curva experimental e as curvas ajustadas dos modelos constitutivos (Fung QLV contínuo, Simplified Fung com série de Prony e Schapery), exibindo os parâmetros identificados e a métrica de aderência ($R^2 \ge 0.90$).
  - Visualização gráfica de varredura paramétrica One-At-A-Time (OAAT).

---

## 4. O que Falta para Submissão Formal à JOSS

### Checklist de Tarefas

1. [ ] **Desenvolvimento da Web API Executável:**
   - Criar ou finalizar o projeto de inicialização da API (ex.: `src/MelloSilveiraTools.WebApi.Server/` ou `src/MelloSilveiraTools.WebApi/` configurado como executável).
   - Configurar o `Program.cs` com a injeção unificada de dependência (`AddToolsServices`).
   - Disponibilizar os dados de exemplo (`data/samples/`) prontos para envio.

2. [ ] **Desenvolvimento do Frontend Leve:**
   - Construir a interface web com carregamento rápido e gráficos interativos (ex.: Vue/React/Blazor/HTML5 moderno).
   - Conectar com os endpoints da Web API.

3. [ ] **Workflow de CI para Pull Requests e Pushes (`.github/workflows/ci.yml`):**
   - Criar workflow automático para execução no GitHub Actions:
     - Disparo em `push` e `pull_request` (branches `main` e `feature/*`).
     - Instalação do .NET 10 (`actions/setup-dotnet@v4`).
     - Execução de `dotnet build MelloSilveiraTools.sln --configuration Release`.
     - Execução de `dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build`.
   - Adicionar a badge de status de build no topo do `README.md`.

4. [ ] **Figura Ilustrativa no Artigo (`paper/`):**
   - Os revisores da JOSS valorizam fortemente a presença de uma figura explicativa.
   - Criar e adicionar em `paper/`:
     - Opção A: Diagrama da arquitetura da pipeline TPL Dataflow (ingestão $\to$ segmentação $\to$ acumulação $\to$ calibração $\to$ sensibilidade).
     - Opção B: Gráfico de ajuste comparativo entre dado experimental de ligamento e os modelos ajustados (Fung QLV vs. Simplified Fung vs. Schapery).
   - Referenciar a figura no corpo de `paper/paper.md` (`![...](figure.png)`).

5. [ ] **Compilação e Verificação do PDF do Artigo:**
   - O workflow `.github/workflows/draft-pdf.yml` já está configurado com `actions/checkout@v4`, `actions/upload-artifact@v4` e `workflow_dispatch`.
   - Após subir os commits para o GitHub, disparar o workflow e inspecionar o arquivo `paper.pdf` gerado.

6. [ ] **Geração de Tag e Publicação no Zenodo:**
   - Criar tag de versão (ex.: `v2.0.0`).
   - Integrar o repositório GitHub com o [Zenodo](https://zenodo.org/) para emissão automática do DOI permanente do software.

7. [ ] **Submissão na Plataforma JOSS:**
   - Submeter a URL do repositório e a branch em [https://joss.theoj.org/papers/new](https://joss.theoj.org/papers/new).

---

## 5. Comandos de Referência Rápida

### Compilação e Testes
```bash
# Compilar toda a solução em Release
dotnet build MelloSilveiraTools.sln --configuration Release

# Executar suíte completa de testes unitários (449 testes)
dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build

# Executar testes de aceitação com banco de dados real (requer Docker)
dotnet test test/AcceptanceTests/AcceptanceTests.csproj --configuration Release
```

### Git Status & Sincronização
```bash
# Verificar arquivos modificados
git status

# Adicionar arquivos e commitar
git add .
git commit -m "docs: finalize JOSS submission readiness, paper metadata and roadmap"

# Enviar para o repositório remoto
git push origin feature/thesis-soft-tissue
```
