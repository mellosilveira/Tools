# Plano Arquitetural e de Implementação: Integração com FEBio e Simulação Tensorial 3D

## 1. Visão Geral e Objetivos

Este documento especifica a fundamentação teórica, a arquitetura de software e as etapas de implementação para viabilizar a **Simulação Tensorial Tridimensional em Elementos Finitos via FEBio** e sua integração automatizada ao ecossistema `MelloSilveiraTools`.

Os objetivos específicos compreendem:
1. Fechar a **Tríade de Dados** da pesquisa (1. Dados Experimentais Reais, 2. Simulação Numérica 1D em C#, 3. Simulação Tensorial 3D em FEBio).
2. Formular matematicamente a generalização para a mecânica dos meios contínuos 3D dos modelos constitutivos investigados.
3. Desenvolver a rotina analítica de recalibração paramétrica, convertendo os parâmetros escalares 1D identificados no C# em constantes tensoriais equivalentes para tração uniaxial em corpos de prova de ligamentos.
4. Implementar sub-rotinas de material customizado (*User-Defined Material* - UDM) em C++ para os modelos que não possuem implementação nativa no FEBio (foco no modelo não linear de Schapery).
5. Construir um motor automatizado de execução (*Headless Runner*) que gere arquivos de entrada `.feb`, execute o solver em lote, extraia as curvas temporais resultantes e persista os dados no PostgreSQL via `StoredFileEntity`.

---

## 2. Justificativa Estratégica e Metodológica (The "Why")

* **Por que FEBio e não Solvers Proprietários (Abaqus, Ansys, COMSOL):**
  * *Transparência e Ciência Aberta:* O FEBio é uma plataforma de elementos finitos totalmente aberta, gratuita e desenvolvida especificamente para biomecânica e mecânica computacional de tecidos moles. Permite auditoria completa do código-fonte das formulações não lineares e bifásicas.
  * *Independência de Licenças:* Elimina qualquer dependência de servidores de licença comercial nas máquinas do laboratório, garantindo reprodutibilidade irrestrita dos ensaios computacionais.
* **Por que Simulação 3D no FEBio em vez de Implementação Nativa em C#:**
  * O motor em C# (`MelloSilveiraTools`) permanece estritamente escalar (1D). Construir geradores de malha tridimensional, integração numérica isoparamétrica de Gauss, rotinas de contato e resolutores de matrizes esparsas não lineares em C# demandaria um esforço excessivo de engenharia, desviando o foco do objetivo científico central. O C# atua como calibrador ágil de parâmetros 1D e orquestrador de dados.
* **Por que Ensaio de Tração Uniaxial Isolado:**
  * Simulações envolvendo o joelho anatômico completo com articulação tridimensional complexa e contato entre superfícies articulares apresentam alta taxa de não convergência e demandam tempos computacionais proibitivos. O foco no ligamento isolado submetido à tração uniaxial pura reproduz com fidelidade as condições do ensaio de bancada experimental, permitindo isolar os efeitos puramente constitutivos do material sem interferência de instabilidades de contato.
* **Por que Esforço com Orçamento Fechado e Gatilho de Contingência no UDM C++:**
  * O desenvolvimento de materiais customizados em C++ para o FEBio exige a dedução e programação das derivadas analíticas do tensor de Cauchy em relação à deformação (tensor tangente elasto-viscoelástico $C_{ijkl} = 2 \frac{\partial \boldsymbol{S}}{\partial \boldsymbol{C}}$). Para mitigar o risco de divergência matemática ou perda excessiva de tempo em depuração numérica de baixo nível, estabelece-se um ciclo de desenvolvimento delimitado por esforço (4 iterações de desenvolvimento). Se a convergência numérica não for alcançada, ativa-se o plano de contingência aprovado: o modelo de Schapery permanece na dissertação na comparação analítica 1D (onde sua eficácia já está comprovada), e a simulação 3D no FEBio é executada com os modelos suportados nativamente (Fung QLV e Holzapfel-Gasser-Ogden - HGO).

---

## 3. Arquitetura da Integração

```
┌────────────────────────────────────────────────────────┐
│  MelloSilveiraTools (C# 10 / Optimizations)            │
│  - Identificação de Parâmetros 1D                      │
│  - Recalibrador Analítico (1D -> 3D)                   │
└──────────────────────────┬─────────────────────────────┘
                           │ Constantes 3D Recalibradas
                           ▼
┌────────────────────────────────────────────────────────┐
│  FebioAutomationRunner (Headless CLI / Automation)     │
│  - Injeção de Parâmetros no Template XML (.feb)        │
│  - Execução: febio4 -i input.feb -silent               │
│  - Parsing de Resultados (.xplt / .log)                │
└──────────────────────────┬─────────────────────────────┘
                           │ Séries Temporais σ(t) e F(t) 3D
                           ▼
┌────────────────────────────────────────────────────────┐
│  Persistência Relacional e Visualização                │
│  - Armazenamento em disco via IFileStorageService      │
│  - Registro no banco de dados via StoredFileEntity     │
│  - Consumo pela Camada Frontend (Tríade de Dados)      │
└────────────────────────────────────────────────────────┘
```

---

## 4. Componentes e Estrutura de Arquivos

### 4.1 Extensão no Backend (`MelloSilveiraTools.MechanicsOfMaterials.Optimizations`)
* `ConstitutiveModels/Recalibration/IConstitutiveParameter3DRecalibrator.cs`: Contrato para mapeamento analítico de parâmetros escalares 1D em tensores de material 3D.
* `ConstitutiveModels/Recalibration/FungQlv3DRecalibrator.cs`: Recalibração para a formulação quase-linear de Fung no FEBio.
* `ConstitutiveModels/Recalibration/Hgo3DRecalibrator.cs`: Recalibração para a formulação estrutural de fibras colágenas do HGO.
* `ExternalSolvers/Febio/FebioInputGenerator.cs`: Gerador de arquivos XML `.feb` a partir de templates paramétricos.
* `ExternalSolvers/Febio/FebioLogParser.cs`: Leitor de arquivos de log e extração de tensão axial Cauchy e força resultante.

### 4.2 Sub-rotina UDM em C++ (`external/febio-udm/`)
* `SchaperyMaterial.h`: Declaração da classe derivada de `FEViscoElasticMaterial` (ou `FEMaterial`).
* `SchaperyMaterial.cpp`: Implementação do cálculo da tensão de Cauchy $\boldsymbol{\sigma}(t)$, variáveis de história interna e tensor tangente espacial $\mathbb{C}$.
* `CMakeLists.txt`: Configuração de compilação multi-plataforma (gerando `.dll` no Windows ou `.so` no Linux para carga dinâmica pelo solver `febio4`).

---

## 5. Fases de Implementação

### Fase 1: Dedução Matemática dos Cheat Sheets Tensoriais 3D
1. Redigir os Cheat Sheets matemáticos formais na pasta de literatura teórica (`.agents/soft-tissue-biomechanics/base_literature/theory_cheat_sheets/`):
   * `schapery_3d_tensorial.md`: Dedução da forma integral tensorial sob tensão uniaxial.
   * `generalized_maxwell_prony_3d.md`: Formulação da série de Prony tensorial.
   * `weiss_transversely_isotropic_3d.md`: Formulação hiperelástica anisotrópica.
   * `poroviscoelasticity_3d.md`: Formulação bifásica poroelástica para tecidos hidratados.
   * `hgo_collagen_dispersion_3d.md`: Formulação estrutural de Holzapfel-Gasser-Ogden com dispersão contínua de fibras.
2. Validar cada dedução quanto à consistência termodinâmica e definição explícita de variáveis com o agente *Co-Advisor*.

### Fase 2: Metodologia e Rotina de Recalibração Paramétrica (1D $\to$ 3D)
1. Formular as equações de equivalência uniaxial:
   * Sob incompressibilidade e tração ao longo do eixo 1: $\lambda_1 = \lambda$, $\lambda_2 = \lambda_3 = \lambda^{-1/2}$.
   * Tensor de deformação de Green-Lagrange: $\boldsymbol{E} = \frac{1}{2}(\boldsymbol{C} - \boldsymbol{I})$.
   * Mapeamento entre as constantes 1D extraídas ($G_e, g_1, n, c, A, B$) e os coeficientes de material do FEBio.
2. Implementar as classes de recalibração em C# com cobertura completa de testes em `test/UnitTests`.
3. Validar numericamente que, sob o mesmo histórico de deformação uniaxial, o modelo analítico 3D uniaxial degenera exatamente na curva escalar 1D.

### Fase 3: Desenvolvimento UDM C++ e Verificação dos Modelos Nativos (Ciclo com Gatilho de Contingência)
1. **Modelos Nativos:**
   * Configurar templates `.feb` para os modelos nativos do FEBio: *Fung QLV* (hiperelástico transversalmente isotrópico acoplado a relaxação QLV) e *HGO* (Holzapfel-Gasser-Ogden).
   * Validar malha padrão de corpo de prova cilíndrico uniaxial sob passos de tempo idênticos aos dos ensaios de relaxação.
2. **Modelo Não Linear de Schapery (Ciclo de Desenvolvimento com Gatilho de Contingência):**
   * Executar o desenvolvimento da sub-rotina C++ UDM com orçamento delimitado em 4 ciclos sucessivos de esforço técnico:
     * *Ciclo 1:* Implementação do tensor de tensões e variáveis de história.
     * *Ciclo 2:* Implementação do tensor elasto-viscoelástico tangente consistente.
     * *Ciclo 3:* Compilação da biblioteca dinâmica e integração com a interface de plugins do FEBio.
     * *Ciclo 4:* Testes de convergência uniaxial sob grandes deformações.
   * **Critério de Parada e Gatilho de Contingência:**
     * Caso a sub-rotina atinja convergência e estabilidade numérica ao final do 4º ciclo, o UDM é homologado para a produção de dados 3D.
     * Caso persista instabilidade ou divergência após os 4 ciclos, aplica-se a decisão registrada no *Ledger*: interrompe-se a depuração C++, mantém-se o modelo de Schapery na dissertação na análise comparativa 1D em C#, e o FEBio é executado exclusivamente com os modelos nativos (Fung QLV e HGO), registrando detalhadamente as razões físicas e computacionais no capítulo de Discussão.

### Fase 4: Automação Headless e Ingestão de Resultados
1. Implementar o serviço de automação `FebioAutomationRunner` no ecossistema:
   * Leitura das constantes recalibradas no banco de dados.
   * Geração do arquivo XML de entrada `.feb` customizado.
   * Invocação do binário do FEBio em modo em lote (`febio4 -i input.feb -silent`).
   * Captura dos dados nodais de deslocamento e força de reação no topo do modelo.
   * Cálculo da curva de tensão Cauchy ($\sigma(t) = F(t) / A(t)$) sob conservação de volume.
2. Persistir o arquivo de saída gerado através de `IFileStorageService` e registrar o histórico na tabela `stored_files` (`Context = "FEBio3DSimulation"`).

### Fase 5: Validação da Tríade de Dados
1. Executar a cadeia completa para um conjunto de ensaios de ligamentos suínos.
2. Confirmar a convergência e comparar a curva 3D extraída do FEBio com a curva 1D em C# e os dados experimentais brutos.
3. Expor os resultados 3D nos endpoints da API para consumo direto pelo frontend e pelos scripts de publicação.

---

## 6. Critérios de Aceite e Validação

* [ ] Todos os 5 Cheat Sheets tensoriais 3D matematicamente deduzidos e aprovados pelo coorientador.
* [ ] Recalibrador analítico (1D $\to$ 3D) implementado em C# e validado por testes unitários.
* [ ] Simulações nativas no FEBio (Fung QLV e HGO) rodando com sucesso para corpos de prova uniaxiais sob histórico de rampa e relaxação escalonada.
* [ ] Gatilho de contingência do Schapery UDM rigorosamente executado após o orçamento de 4 ciclos de desenvolvimento.
* [ ] Runner headless executando a simulação 3D em modo lote e gravando os resultados no PostgreSQL sem intervenção manual.
* [ ] Curvas 3D disponíveis para sobreposição visual na camada frontend e na esteira de publicação.
