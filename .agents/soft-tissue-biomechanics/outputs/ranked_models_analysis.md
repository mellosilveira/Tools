# Análise de Modelos Constitutivos para Ligamentos de Joelho
*Rankeamento baseado em volume de uso na literatura e integração em pacotes de Elementos Finitos (FEA)*

Abaixo está a síntese dos achados do Funil de Pesquisa Bibliográfica, categorizando e rankeando os modelos constitucionais aplicados especificamente a ligamentos do joelho.

## 1. Família Hiperelástica + Maxwell Generalizado (Série de Prony)
**Nível de Uso na Literatura:** Altíssimo (O Padrão da Indústria)
**Motivo do Domínio:** É a formulação "nativa" (Built-in) de softwares como Abaqus, ANSYS e FEBio. Consiste em utilizar um modelo hiperelástico para a resposta imediata, acoplado a uma Série de Prony para computar o relaxamento de tensão via Variáveis Internas de Estado.
- **Sub-modelos embutidos:**
  - **Mooney-Rivlin / Neo-Hookeano:** Usados para tecidos mais isotrópicos ou matriz base.
  - **Holzapfel-Gasser-Ogden (HGO):** Extremamente utilizado por permitir a definição da dispersão angular (bagunça) das fibras de colágeno, tornando-se o padrão-ouro moderno para hiperelasticidade anisotrópica.

## 2. Modelo Hiperelástico Transversalmente Isotrópico de Weiss
**Nível de Uso na Literatura:** Muito Alto (Especialmente no FEBio)
**Motivo do Domínio:** Foi desenhado especificamente para ligamentos. Ele divide a energia de deformação do ligamento matematicamente em duas partes: a matriz isotrópica (o material base) e as fibras de colágeno (que só resistem à tração). É a base de quase todas as simulações de ligamento cruzado anterior (LCA) no FEBio.

## 3. Viscoelasticidade Quase-Linear de Fung (QLV - Quasi-Linear Viscoelasticity)
**Nível de Uso na Literatura:** Alto (O Padrão-Ouro Histórico)
**Motivo do Domínio:** Fung é o "pai" da biomecânica. Seu modelo QLV assume que a resposta elástica (energia de deformação exponencial) e o relaxamento viscoso podem ser separados e multiplicados. É o modelo mais validado experimentalmente em ensaios uniaxiais *ex vivo* nas últimas 3 décadas. Seu uso em FEA 3D diminuiu ligeiramente devido ao custo computacional da integral de convolução contínua, mas continua sendo a referência analítica.

## 4. Modelos Poroelásticos / Poroviscoelásticos (Bifásicos)
**Nível de Uso na Literatura:** Médio para Ligamentos (Altíssimo para Cartilagem/Menisco)
**Motivo do Domínio:** Modelam o ligamento como uma esponja permeável contendo fluido intersticial (água). A viscoelasticidade emerge não das moléculas da fibra, mas da fricção da água escoando pelos poros do tecido quando o ligamento é torcido ou comprimido. Modelos avançados do joelho inteiro (Full-Knee FEA) usam poroelasticidade para prever o fluxo de fluidos articulares, mas o custo de simulação é brutal (requer solver acoplado de mecânica e fluidos).

## 5. Viscoelasticidade Integral Não-Linear de Schapery
**Nível de Uso na Literatura:** Baixo/Emergente na Biomecânica 3D
**Motivo do Domínio:** Nasceu na engenharia de polímeros (materiais aeroespaciais). É termodinamicamente mais robusto e não-linear do que Fung, porém, a extração de suas constantes e a sua formulação matemática tensorial 3D em Elementos Finitos resultam em Jacobianos muitas vezes assimétricos. O seu uso é mais comum em modelos 1D uniaxiais. Migrar Schapery para o 3D, como proposto no Ticket 1.5 da sua tese, é justamente um diferencial científico (gap na literatura).
