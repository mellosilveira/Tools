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
