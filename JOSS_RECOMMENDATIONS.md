# Guia Rápido de Submissão JOSS — MelloSilveiraTools

> **Fonte Única de Verdade:** Todo o diagnóstico detalhado, estratégia dual de publicação (JOSS + Artigo Científico Principal) e mapeamento sistemático de periódicos estão consolidados em [`.agents/soft-tissue-biomechanics/consolidated-joss-and-publications.md`](.agents/soft-tissue-biomechanics/consolidated-joss-and-publications.md).

---

## Metadados & Status Atual
* **Manuscrito:** [`paper/paper.md`](paper/paper.md) e [`paper/paper.bib`](paper/paper.bib) (compilação PDF via `.github/workflows/draft-pdf.yml`).
* **Autores:** Bruno Mello Silveira (`ORCID: 0000-0001-6793-9387`, correspondente) e Prof. Dr. Paulo Pedro Kenedi (`ORCID: 0000-0001-5563-843X`) — PPEMM/UFCIRJ & Mello Silveira Serviços LTDA.
* **Licença:** `GPL-3.0-or-later` (compatível com ALGLIB Free Edition `GPL-2.0-or-later`).
* **Arquivos Comunitários:** `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `.github/ISSUE_TEMPLATE/` presentes.
* **Solução (`.NET 10` / `C# 13`):** 9 projetos em `src/` (8 pacotes NuGet + `Database.Migrator`) compilando com **0 avisos e 0 erros**, documentação XML completa e suítes automatizadas (`UnitTests` em memória + `AcceptanceTests` com Testcontainers PostgreSQL).

## Pendências para Submissão Formal
1. [ ] **Web API Executável (Quickstart)** com `AddToolsServices`, CORS, `/swagger` e dados amostrais (`data/samples/knee-ligaments/`). *(Decisão: Web API + Frontend em vez de console app.)*
2. [ ] **Frontend Web Leve** (upload/amostras com 1 clique, segmentação, ajuste Fung/SimplifiedFung/Schapery + $R^2 \ge 0.90$ e sensibilidade OAAT).
3. [ ] **Workflow de CI (`.github/workflows/ci.yml`)** com `actions/setup-dotnet@v4` em `push`/`pull_request` (`main` e `feature/*`) + badge no `README.md`.
4. [ ] **Figura Ilustrativa em `paper/`** referenciada no corpo de `paper/paper.md`.
5. [ ] **Execução Final em `Release` & Revisão do PDF:** sincronizar contagem exata de testes em `paper/paper.md` e inspecionar `paper.pdf` gerado pelo workflow `draft-pdf.yml`.
6. [ ] **Submissão:** enviar em [https://joss.theoj.org/papers/new](https://joss.theoj.org/papers/new). *(Tag de release e arquivamento com DOI no Zenodo ocorrem somente após o aceite da revisão por pares.)*

## Comandos Rápidos
```powershell
dotnet build MelloSilveiraTools.sln --configuration Release
dotnet test test/UnitTests/UnitTests.csproj --configuration Release --no-build
dotnet test test/AcceptanceTests/AcceptanceTests.csproj --configuration Release
```
