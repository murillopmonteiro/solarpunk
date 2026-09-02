# Solarpunk — Game Design Doc v0.1

## Premissa

Tile-placement em hex grid. Você constrói cidade, usinas e extração num território
finito, equilibrando 5 recursos. Puzzle espacial (restrição de relevo) + puzzle de
sistema (energia vs. sustentabilidade vs. dinheiro).

## 1. Recursos (stats globais)

| Recurso | Símbolo | Função |
|---|---|---|
| Energia | ⚡ | Consumido pela cidade, gerado pelas usinas. Se demanda > oferta → penalidade (blackout, felicidade cai) |
| Dinheiro | $ | Custo de construção, upgrade, pesquisa |
| Sustentabilidade | 🌱 | Recurso "de saúde do planeta". Cai com extração e usinas fósseis, sobe com usinas limpas. Se chegar a 0 → condição de derrota |
| População | 👤 | Cresce com nível da cidade, consome energia |
| Felicidade | ❤️ | Cai com blackout e baixa sustentabilidade, sobe com energia limpa e pesquisa. Se chegar a 0 → derrota (êxodo) |

Todo tile do jogo é definido por um vetor de efeito nesses 5 recursos, por turno.

## 2. Grid

- Hexágonos, território ~10u² gerado aleatoriamente por partida.
- Cada hex tem **Terreno** (tipo de tile que pode ser construído) + **Relevo**
  (restrição espacial fixa, gerada no setup):
  - Relevo mutável (genérico, sem restrição)
  - Cachoeira → única condição pra Hidrelétrica
  - Montanha → bônus/restrição pra Eólica (mais vento, mais custo)
  - Litoral → única condição pra Maremotriz
- Relevo é sorteado uma vez no início da partida e não muda — isso é o que cria o
  quebra-cabeça de "onde construir o quê".

## 3. Tiles

### T-Cidade (nível 1-10)

- **Progressão: híbrida.** Cresce um "tick" automático por turno *se* felicidade e
  energia estiverem em nível saudável (sem blackout, sustentabilidade não crítica).
  Jogador pode acelerar gastando $ + ⚡ num upgrade manual (pula 1 nível
  instantâneo, custo escala com nível atual).
- **Efeito por nível:** `+pesquisa, +população, +felicidade` / `-sustentabilidade,
  -energia` (consumo de energia escala com nível — cidade nível 10 consome muito
  mais que nível 1).
- Isso é o motor de pressão do jogo: cidade cresce, consome mais, você precisa
  gerar mais energia limpa sem estourar sustentabilidade.

### T-Usina (8 tipos, cada uma com trade-off energia vs. sustentabilidade vs. custo)

| Tipo | Restrição de relevo | Perfil |
|---|---|---|
| Hidrelétrica | Cachoeira | Alta energia, sustentável, custo médio-alto |
| Maremotriz | Litoral | Energia média, muito sustentável, custo alto |
| Eólica | Livre (bônus em montanha) | Energia média, sustentável, intermitente |
| Solar | Livre | Energia média, sustentável, barata, intermitente |
| Nuclear | Livre | Energia altíssima, custo altíssimo, sustentabilidade neutra (não suja, mas não "limpa" na percepção/felicidade) |
| Biomassa | Livre | Energia baixa-média, sustentabilidade neutra/levemente negativa |
| Carvão | Livre | Energia alta, barata, sustentabilidade muito negativa |
| Petróleo | Livre | Energia alta, cara, sustentabilidade muito negativa |

### T-Extração

- `+nível, +dinheiro` / `-sustentabilidade`
- Alimenta usinas fósseis/nuclear (sem extração ativa, essas usinas operam com
  penalidade ou não podem ser construídas — a definir).

## 4. Ações (turno)

1. **Construir** (martelo) — gasta $ pra colocar tile num hex válido
2. **Pesquisa** (lâmpada) — gasta pesquisa acumulada pra desbloquear upgrades
   (ex: reduzir custo de solar, aumentar eficiência das usinas)
3. **Avançar turno** (play) — resolve produção/consumo do turno, aplica eventos
4. **Editar** (lápis) — modifica/remove tile já construído no hex

## 5. Condições de fim de jogo

- **Derrota:** sustentabilidade = 0 OU felicidade = 0, a qualquer momento antes
  do turno 300
- **Vitória:** sobreviver até o turno 300 sem derrota. Não há estado "ideal"
  obrigatório (ex: não precisa ser 100% renovável) — o jogo é sandbox de
  otimização, e o objetivo declarado é sempre tentar melhorar o desempenho da
  partida anterior (score comparativo, não meta fixa).

## Decisões resolvidas

- Progressão de cidade: **híbrida** (automática condicional + upgrade manual pago)
- Relevo é fixo desde o setup, não regenera
- Extração é pré-requisito de operação pra fósseis/nuclear, não só de construção
- **Turno = 1 ano.** A cada turno, evento aleatório dispara.
- **Território:** começa em 10u², fixo — só cresce via (a) evento aleatório de
  expansão ou (b) compra direta com $ (caro). Sem expansão, joga a partida
  inteira nesses 10u².
- **Storage:** não existe como tile separado. Energia excedente fica implícita
  dentro do próprio recurso global ⚡ — ele funciona como buffer (sobra de um
  turno cobre déficit de outro). Sem hex dedicado, sem restrição de relevo.
  Revisão futura possível se o playtesting mostrar que fica fácil demais.
- **Duração da partida:** 300 turnos (ano 0 → ano 300), sem correspondência com
  calendário real — é linha do tempo interna do jogo. Ano 0 representa o começo
  da era industrial (ambientação), não uma data histórica literal.

---

*Fonte original: `solarpunk-game-design.pdf`, mantido em `docs/` para referência.*
