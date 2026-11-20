# Guia Rápido de Submissão JOSS — MelloSilveiraTools

> **Fonte Única de Verdade:** Todo o diagnóstico detalhado, estratégia dual de publicação (JOSS + Artigo Científico Principal) e mapeamento sistemático de periódicos estão consolidados em [`.agents/soft-tissue-biomechanics/consolidated-joss-and-publications.md`](.agents/soft-tissue-biomechanics/consolidated-joss-and-publications.md).

---

## Metadados & Status Atual
* **Manuscrito:** [`paper/paper.md`](paper/paper.md) e [`paper/paper.bib`](paper/paper.bib) (compilação PDF via `.github/workflows/draft-pdf.yml`).
* **Autores:** Bruno Mello Silveira (`ORCID: 0000-0001-6793-9387`, correspondente) e Prof. Dr. Paulo Pedro Kenedi (`ORCID: 0000-0001-5563-843X`) — PPEMM/UFCIRJ & Mello Silveira Serviços LTDA.
* **Licença:** `GPL-3.0-or-later` (compatível com ALGLIB Free Edition `GPL-2.0-or-later`).
* **Arquivos Comunitários:** `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `.github/ISSUE_TEMPLATE/` presentes.
* **Solução (`.NET 10` / `C# 13`):** 12 projetos em `src/` (8 pacotes NuGet + `Database.Migrator` + `WebApi.Host` + `WebUi` + `Launcher`) compilando com **0 avisos e 0 erros**, documentação XML completa e suítes automatizadas (`UnitTests` em memória + `AcceptanceTests` com Testcontainers PostgreSQL).

## Pendências para Submissão Formal
1. [x] **Web API Executável (`MelloSilveiraTools.WebApi.Host`)** com `AddToolsServices`, JWE obrigatória (`/api/v1/auth/demo-token`), CORS, `/swagger`, upload `multipart/form-data`, consultas de resultados e dados amostrais (`SamplesData/`).
2. [x] **Frontend Web (`MelloSilveiraTools.WebUi` Blazor WASM + ECharts) & `MelloSilveiraTools.Launcher`:** Interface interativa servida pelo host + orquestrador multiplataforma (Docker -> PostgreSQL local -> prompt de instalação -> Migrator -> Host).
3. [ ] **Figura Ilustrativa em `paper/`** referenciada no corpo de `paper/paper.md` *(última etapa a partir do que foi implementado)*.
4. [ ] **Execução Final em `Release` & Revisão do PDF:** executar as suítes completas em `Release` e inspecionar `paper.pdf` gerado pelo workflow `draft-pdf.yml`.
5. [ ] **Submissão:** enviar em [https://joss.theoj.org/papers/new](https://joss.theoj.org/papers/new). *(Tag de release e arquivamento com DOI no Zenodo ocorrem somente após o aceite da revisão por pares.)*

> *(Nota: A automação de build e execução de testes no GitHub Actions já é coberta pelos workflows existentes em `.github/workflows/publish_release_candidate_nuget_package.yml` e `.github/workflows/publish_nuget_package.yml`, dispensando um `ci.yml` adicional.)*

## Comandos Rápidos
```powershell
dotnet build MelloSilveiraTools.sln --configuration Release
dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build
dotnet test test/AcceptanceTests/AcceptanceTests.csproj --configuration Release
```
