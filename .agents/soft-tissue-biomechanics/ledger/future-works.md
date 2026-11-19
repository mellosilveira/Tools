# Trabalhos Futuros Mapeados (Future Works)

Este documento centraliza as oportunidades de melhoria, expansões e próximos passos da pesquisa em Biomecânica de Tecidos Moles, garantindo que nenhum item se perca ao longo do desenvolvimento do projeto.

## Lista de Trabalhos Futuros

- **Conversão de constantes escalares 1D para tensores 3D**
  - **Contexto:** Otimização FEA Inversa.
  - **Descrição:** Adaptar os modelos mecânicos desenvolvidos (atualmente trabalhando com variáveis escalares 1D) para o espaço tensorial 3D, permitindo simulações de elementos finitos mais realistas.

- **Programação de plugins UDM em C++ para o FEBio**
  - **Contexto:** Integração com software FEA (Finite Element Analysis).
  - **Descrição:** Desenvolver *User-Defined Materials* (UDM) no FEBio utilizando C++.
  - **Nota:** Esta tarefa deve ser rigorosamente controlada com *time-box* para evitar que tome tempo excessivo do escopo principal da pesquisa.

- **Incluir taxa de deformação (strain rate) na análise de sensibilidade de variáveis**
  - **Contexto:** Análise de Sensibilidade (Parametric Sweep / Grid Search).
  - **Descrição:** Avaliar como a taxa de deformação de entrada afeta os parâmetros ajustados e a convergência da simulação para os modelos constitutivos.

- **Explicabilidade e rastreabilidade teórico-experimental integral na síntese da resposta final por IA**
  - **Contexto:** Camada de Apresentação / Agentes de Inteligência Artificial / Relatórios e Conclusões da Análise.
  - **Descrição:** Estruturar a geração de respostas, laudos e análises finais (inclusive quando sintetizados por agentes de IA) para que não apresentem apenas conclusões diretas ou números isolados, mas forneçam obrigatoriamente:
    1. O encadeamento analítico passo a passo (*step-by-step derivation/reasoning*) de todo o processo de cálculo e simulação;
    2. O embasamento teórico estrito da Mecânica dos Sólidos, Teoria de Grandes Deformações e Viscoelasticidade Linear/Não-Linear (hipóteses cinemáticas, tensores ou escalares adotados e formulações constitutivas empregadas);
    3. O embasamento experimental contextualizado (identificação do ensaio, geometria/L0 da amostra, fases de deformação — rampa, relaxação, recuperação —, parâmetros ajustados e métricas de aderência $R^2$/RMSE).
  - **Justificativa/Objetivo:** Eliminar saídas do tipo "caixa-preta" (*black-box*), garantindo auditabilidade científica, transparência metodológica e reprodutibilidade integral requeridas para a dissertação de mestrado (PPEMM/UFCIRJ) e para publicações em periódicos científicos de alto impacto.
