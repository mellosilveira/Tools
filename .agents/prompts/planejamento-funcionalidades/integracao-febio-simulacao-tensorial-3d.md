# Plano Arquitetural e de Implementação — Módulo 07: Integração com FEBio e Simulação Tensorial 3D

## 1. Visão Geral e Objetivos

Especifica a fundamentação teórica, a arquitetura de software e as etapas de implementação para a **Simulação Tensorial 3D em Elementos Finitos via FEBio** integrada ao ecossistema `MelloSilveiraTools`:

1. Fechar a **Tríade de Dados** da pesquisa: (1) Dados Experimentais Reais, (2) Simulação Analítico-Numérica 1D em C# e (3) Simulação Tensorial 3D em FEBio.
2. Formular matematicamente a generalização 3D (mecânica dos meios contínuos) dos modelos constitutivos investigados.
3. Implementar rotina analítica de recalibração paramétrica ($1\text{D} \to 3\text{D}$), convertendo parâmetros escalares 1D identificados no C# em constantes tensoriais equivalentes para tração uniaxial em ligamentos.
4. Desenvolver sub-rotina de material customizado (*User-Defined Material* — UDM) em C++ para modelos sem suporte nativo no FEBio (foco no modelo não linear de Schapery), sob orçamento fechado de esforço.
5. Construir motor automatizado (*Headless Runner*) que gera `.feb`, executa o solver em lote, extrai curvas temporais e persiste resultados no PostgreSQL (`stored_files` via `StorageEntryMetadataEntity`).

---

## 2. Justificativa Estratégica e Metodológica (The "Why")

* **FEBio em vez de solvers proprietários (Abaqus, Ansys, COMSOL):** Plataforma open-source, gratuita e dedicada à biomecânica de tecidos moles; permite auditoria completa do código-fonte das formulações não lineares e bifásicas e elimina dependência de servidores de licença no laboratório, garantindo reprodutibilidade irrestrita.
* **Simulação 3D no FEBio em vez de implementação nativa em C#:** O motor `MelloSilveiraTools` permanece estritamente escalar (1D) como calibrador ágil e orquestrador. Implementar malhadores 3D, integração isoparamétrica de Gauss, rotinas de contato e solvers esparsos não lineares em C# desviaria esforço de engenharia do objetivo científico central.
* **Tração uniaxial em ligamento isolado:** Simulações do joelho anatômico completo com contato articular apresentam alta taxa de não convergência e custo computacional proibitivo. O ligamento isolado em tração uniaxial reproduz a bancada experimental e isola os efeitos puramente constitutivos sem instabilidades de contato.
* **Orçamento fechado (4 ciclos) e gatilho de contingência no UDM C++:** Materiais customizados no FEBio exigem a dedução e implementação do tensor tangente elasto-viscoelástico consistente ($C_{ijkl} = 2\frac{\partial \boldsymbol{S}}{\partial \boldsymbol{C}}$). Para mitigar risco de divergência numérica e depuração excessiva de baixo nível, limita-se o esforço a 4 ciclos; caso não convirja, ativa-se a contingência aprovada no *Scientific Ledger* (manter Schapery na análise comparativa 1D em C# e rodar o FEBio com modelos nativos: Fung QLV e HGO).

> Referências consolidadas: [`consolidated-thesis-guide.md`](../../soft-tissue-biomechanics/consolidated-thesis-guide.md) e [`ledger/decisions.md`](../../soft-tissue-biomechanics/ledger/decisions.md).

---

## 3. Arquitetura e Componentes

**Fluxo da Integração:**
`MelloSilveiraTools (Fit 1D + Recalibrador Analítico 1D→3D)` → `FebioAutomationRunner` (injeção no template XML `.feb` → `febio4 -i input.feb -silent` → parse `.xplt`/`.log` → séries $\sigma(t)$ e $F(t)$ 3D) → `IFileStorageService` + `StorageEntryMetadataEntity` (`stored_files`) → Camada Frontend e Esteira de Publicação (Tríade de Dados).

### 3.1 Extensão no Backend (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`)
* `ConstitutiveModels/Recalibration/IConstitutiveParameter3DRecalibrator.cs`: Contrato para mapeamento analítico de parâmetros escalares 1D em tensores de material 3D.
* `ConstitutiveModels/Recalibration/FungQlv3DRecalibrator.cs`: Recalibração para a formulação quase-linear de Fung no FEBio.
* `ConstitutiveModels/Recalibration/Hgo3DRecalibrator.cs`: Recalibração para a formulação estrutural de fibras colágenas do HGO.
* `ExternalSolvers/Febio/FebioInputGenerator.cs`: Gerador de arquivos XML `.feb` a partir de templates paramétricos.
* `ExternalSolvers/Febio/FebioLogParser.cs`: Leitor de logs/saídas para extração de tensão axial de Cauchy e força resultante.

### 3.2 Sub-rotina UDM em C++ (`external/febio-udm/`)
* `SchaperyMaterial.h`: Declaração da classe derivada de `FEViscoElasticMaterial` (ou `FEMaterial`).
* `SchaperyMaterial.cpp`: Cálculo do tensor de Cauchy $\boldsymbol{\sigma}(t)$, variáveis de história interna e tensor tangente espacial $\mathbb{C}$.
* `CMakeLists.txt`: Build multiplataforma gerando biblioteca dinâmica (`.dll` no Windows / `.so` no Linux) para carga pelo `febio4`.

---

## 4. Fases de Implementação

### Fase 1: Dedução Matemática dos Cheat Sheets Tensoriais 3D
1. Redigir os 5 Cheat Sheets formais em `.agents/soft-tissue-biomechanics/base-literature/theory-cheat-sheets/`:
   * `schapery-3d-tensorial.md`: Dedução da forma integral tensorial sob tensão uniaxial.
   * `generalized-maxwell-prony-3d.md`: Formulação da série de Prony tensorial.
   * `weiss-transversely-isotropic-3d.md`: Formulação hiperelástica anisotrópica.
   * `poroviscoelasticity-3d.md`: Formulação bifásica poroelástica para tecidos hidratados *(condicionada à manutenção no escopo da dissertação)*.
   * `hgo-collagen-dispersion-3d.md`: Formulação estrutural de Holzapfel-Gasser-Ogden com dispersão contínua de fibras.
2. Validar consistência termodinâmica e definição explícita de variáveis com o agente `biomechanics-co-advisor`.

### Fase 2: Metodologia e Rotina de Recalibração Paramétrica ($1\text{D} \to 3\text{D}$)
1. Formular equivalência uniaxial sob incompressibilidade ao longo do eixo 1: $\lambda_1 = \lambda$, $\lambda_2 = \lambda_3 = \lambda^{-1/2}$; tensor de Green-Lagrange $\boldsymbol{E} = \frac{1}{2}(\boldsymbol{C} - \boldsymbol{I})$; mapeamento das constantes 1D ($G_e, g_1, n, c, A, B$) para coeficientes de material do FEBio.
2. Implementar as classes de recalibração em C# com cobertura completa de testes unitários em `test/UnitTests`.
3. Validar numericamente que, sob o mesmo histórico de deformação uniaxial, o modelo analítico 3D uniaxial degenera exatamente na curva escalar 1D.

### Fase 3: Modelos Nativos e Ciclo UDM C++ (com Gatilho de Contingência)
1. **Modelos Nativos:**
   * Configurar templates `.feb` para **Fung QLV** (hiperelástico transversalmente isotrópico acoplado a relaxação QLV) e **HGO** (Holzapfel-Gasser-Ogden).
   * Validar malha padrão de corpo de prova cilíndrico uniaxial sob passos de tempo idênticos aos dos ensaios de relaxação.
2. **Modelo Não Linear de Schapery (Orçamento Máximo de 4 Ciclos):**
   * *Ciclo 1:* Implementação do tensor de tensões e variáveis de história interna.
   * *Ciclo 2:* Implementação do tensor elasto-viscoelástico tangente consistente.
   * *Ciclo 3:* Compilação da biblioteca dinâmica (`.dll`/`.so`) e integração com a interface de plugins do FEBio.
   * *Ciclo 4:* Testes de convergência uniaxial sob grandes deformações.
   * **Critério de Parada e Gatilho de Contingência:** Se convergir com estabilidade ao final do 4º ciclo, homologar o UDM para produção 3D. Caso persista instabilidade/divergência após o 4º ciclo, interromper depuração C++, manter Schapery na análise comparativa 1D em C#, executar FEBio exclusivamente com modelos nativos (Fung QLV e HGO) e **registrar detalhadamente as razões físicas e computacionais no capítulo de Discussão**.

### Fase 4: Automação Headless e Ingestão de Resultados
1. Implementar `FebioAutomationRunner`:
   * Ler constantes recalibradas no banco de dados.
   * Gerar arquivo XML `.feb` customizado a partir dos templates.
   * Invocar o binário em modo lote: `febio4 -i input.feb -silent`.
   * Capturar dados nodais de deslocamento e força de reação no topo do modelo.
   * Calcular a curva de tensão de Cauchy $\sigma(t) = F(t)/A(t)$ sob conservação de volume.
2. Persistir o arquivo de saída via `IFileStorageService` e registrar metadados na tabela `stored_files` via `StorageEntryMetadataEntity` (correlacionado por `ExternalIdentifier`, ex.: `"FEBio3DSimulation_{SampleId}"`).

### Fase 5: Validação da Tríade de Dados
1. Executar a cadeia completa para um conjunto de ensaios de ligamentos suínos.
2. Confirmar convergência e comparar a curva 3D extraída do FEBio com a curva 1D em C# e os dados experimentais brutos.
3. Expor os resultados 3D nos endpoints da API para consumo direto pelo frontend e pela esteira de publicação.

---

## 5. Critérios de Aceite e Validação

* [ ] Todos os 5 Cheat Sheets tensoriais 3D matematicamente deduzidos e aprovados pelo `biomechanics-co-advisor`.
* [ ] Recalibrador analítico ($1\text{D} \to 3\text{D}$) implementado em C# e validado por testes unitários em `test/UnitTests`.
* [ ] Simulações nativas no FEBio (Fung QLV e HGO) convergindo para corpos de prova uniaxiais sob histórico de rampa e relaxação escalonada.
* [ ] Gatilho de contingência do Schapery UDM rigorosamente respeitado após o orçamento de 4 ciclos de desenvolvimento.
* [ ] `FebioAutomationRunner` executando em modo lote e gravando resultados em `stored_files` (`StorageEntryMetadataEntity`) sem intervenção manual.
* [ ] Curvas 3D disponíveis para sobreposição visual na camada frontend e na esteira de publicação.
