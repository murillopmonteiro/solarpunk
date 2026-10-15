using System;
using System.Collections.Generic;
using UnityEngine;

namespace Solarpunk.Core
{
    public enum Language
    {
        English,
        Portuguese
    }

    /// <summary>
    /// Every player-visible string, in English and Brazilian Portuguese.
    ///
    /// A table in code rather than the Localization package, for the same reason
    /// the scene is generated: it is readable C#, it diffs, and there is nothing in
    /// the Editor to wire. Two languages do not need more machinery than this.
    ///
    /// Tiles and research nodes are looked up by asset name ("tile.Carvao"), so a
    /// new asset with no entry here still shows its own displayName instead of a
    /// key. A missing UI key shows the key itself, which is loud on purpose.
    ///
    /// The language is the saved choice, else the system language. The player
    /// can be forced with "-lang pt" / "-lang en", which is how the screenshot
    /// harness captures both.
    /// </summary>
    public static class Loc
    {
        private const string PrefKey = "language";

        private static Language? _current;

        /// <summary>Raised after the language changes, so the HUD can rebuild itself.</summary>
        public static event Action Changed;

        public static Language Current
        {
            get
            {
                _current ??= Initial();
                return _current.Value;
            }
        }

        public static void Set(Language language)
        {
            if (language == Current) return;

            _current = language;
            PlayerPrefs.SetInt(PrefKey, (int)language);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static string T(string key)
        {
            if (!Table.TryGetValue(key, out (string en, string pt) entry)) return key;
            return Current == Language.Portuguese ? entry.pt : entry.en;
        }

        public static string T(string key, params object[] args) => string.Format(T(key), args);

        /// <summary>Translated name for an asset-keyed entry, or <paramref name="fallback"/>.</summary>
        public static string Asset(string key, string fallback) =>
            Table.ContainsKey(key) ? T(key) : fallback;

        private static Language Initial()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-lang");
            if (flag >= 0 && flag + 1 < args.Length)
                return args[flag + 1].StartsWith("pt", StringComparison.OrdinalIgnoreCase)
                    ? Language.Portuguese
                    : Language.English;

            if (PlayerPrefs.HasKey(PrefKey)) return (Language)PlayerPrefs.GetInt(PrefKey);

            return Application.systemLanguage == SystemLanguage.Portuguese
                ? Language.Portuguese
                : Language.English;
        }

        private static readonly Dictionary<string, (string en, string pt)> Table = new()
        {
            // HUD
            ["hud.research"] = ("RESEARCH", "PESQUISA"),
            ["hud.researchCount"] = ("RESEARCH  ·  {0}", "PESQUISA  ·  {0}"),
            ["hud.yearCaption"] = ("YEAR", "ANO"),
            ["hud.yearOf"] = ("of {0}", "de {0}"),
            ["hud.nextYear"] = ("NEXT YEAR", "PRÓXIMO ANO"),
            ["hud.terrain"] = ("TERRAIN", "TERRENO"),
            ["hud.keyOpen"] = ("Open, build anything", "Livre, aceita qualquer coisa"),
            ["hud.keyWaterfall"] = ("Waterfall, hydro only", "Cachoeira, só hidrelétrica"),
            ["hud.keyCoast"] = ("Coast, sea power and wells", "Litoral, energia do mar e poços"),
            ["hud.keyMountain"] = ("Mountain, mines and wind", "Montanha, minas e eólica"),
            ["hud.hint"] = ("Click a hexagon to build.\nSpace also advances a year.",
                "Clique num hexágono para construir.\nEspaço também avança um ano."),
            ["hud.playAgain"] = ("PLAY AGAIN", "JOGAR DE NOVO"),
            ["hud.victory"] = ("VICTORY\n\nYou reached year {0}.", "VITÓRIA\n\nVocê chegou ao ano {0}."),
            ["hud.collapse"] = ("COLLAPSE\n\nThe population is gone.",
                "COLAPSO\n\nA população chegou a zero."),
            // Names the language the button switches *to*, in that language.
            ["hud.switchLanguage"] = ("PORTUGUÊS", "ENGLISH"),

            // Admin mode (F1)
            ["admin.title"] = ("ADMIN MODE  ·  F1", "MODO ADMIN  ·  F1"),
            ["admin.money"] = ("+$1000", "+$1000"),
            ["admin.research"] = ("+500 research", "+500 de pesquisa"),
            ["admin.unlockAll"] = ("Unlock all research", "Liberar toda a pesquisa"),
            ["admin.refill"] = ("Refill sustain + happy", "Encher sustent. + felic."),
            ["admin.skipYears"] = ("Skip 10 years", "Pular 10 anos"),
            ["admin.event"] = ("Draw an event card", "Sortear um evento"),
            ["admin.invincibleOff"] = ("Invincible: OFF", "Invencível: NÃO"),
            ["admin.invincibleOn"] = ("Invincible: ON", "Invencível: SIM"),
            ["admin.set"] = ("SET", "OK"),

            // Resource pills
            ["res.energy"] = ("ENERGY", "ENERGIA"),
            ["res.money"] = ("MONEY", "DINHEIRO"),
            ["res.sustain"] = ("SUSTAIN", "SUSTENT."),
            ["res.people"] = ("PEOPLE", "POPULAÇÃO"),
            ["res.happy"] = ("HAPPY", "FELICIDADE"),
            ["res.steady"] = ("steady", "estável"),
            ["res.perYear"] = ("{0} /yr", "{0} /ano"),

            // Per-year effect summary
            ["stat.energy"] = ("Energy {0}", "Energia {0}"),
            ["stat.sust"] = ("Sust {0}", "Sust {0}"),
            ["stat.money"] = ("Money {0}", "Dinheiro {0}"),
            ["stat.pop"] = ("Pop {0}", "Pop {0}"),
            ["stat.happy"] = ("Happy {0}", "Felic {0}"),
            ["stat.none"] = ("No yearly effect", "Sem efeito anual"),

            // Build panel
            ["build.noSelection"] = ("NO HEX SELECTED", "NENHUM HEXÁGONO SELECIONADO"),
            ["build.clickToInspect"] = ("Click a hexagon to inspect it", "Clique num hexágono para inspecioná-lo"),
            ["build.hex"] = ("HEX {0}", "HEX {0}"),
            ["build.descWaterfall"] = ("Waterfall. Hydro plants only.", "Cachoeira. Só hidrelétricas."),
            ["build.descMountain"] = ("Mountain. Mines, wind, geothermal.", "Montanha. Minas, eólica, geotérmica."),
            ["build.descCoast"] = ("Coast. Sea power and offshore wells.", "Litoral. Energia do mar e poços."),
            ["build.descOpen"] = ("Open ground. Build anything.", "Terreno livre. Aceita qualquer construção."),
            ["build.buildHere"] = ("BUILD HERE", "CONSTRUIR AQUI"),
            ["build.cityDetail"] = ("Grows each year, houses your people", "Cresce a cada ano, abriga sua população"),
            ["build.bonus"] = ("{0} BONUS", "BÔNUS: {0}"),
            ["build.builtCity"] = ("BUILT: {0}, LEVEL {1}", "CONSTRUÍDO: {0}, NÍVEL {1}"),
            ["build.growing"] = ("GROWING: {0}", "CRESCENDO: {0}"),
            ["build.built"] = ("BUILT: {0}", "CONSTRUÍDO: {0}"),
            ["build.currentOutput"] = ("Current output", "Produção atual"),
            ["build.upgrade"] = ("Upgrade to level {0}", "Subir para o nível {0}"),
            ["build.upgradeDetail"] = ("Instant, skips waiting for growth", "Imediato, sem esperar o crescimento"),
            ["build.clearLand"] = ("Clear the land", "Desmatar"),
            ["build.demolish"] = ("Demolish", "Demolir"),
            ["build.clearDetail"] = ("Fells the forest and frees the hex to build on",
                "Derruba a floresta e libera o hexágono"),
            ["build.demolishDetail"] = ("Clears the hex, refunds part of the cost",
                "Libera o hexágono, devolve parte do custo"),
            ["build.capped"] = ("Capped at level {0}", "Limite: nível {0}"),
            ["build.cappedDetail"] = ("Research raises the city level cap", "Pesquisas aumentam o nível máximo"),
            ["build.maturesIn"] = ("Still growing in, yields in {0} years", "Ainda crescendo, produz em {0} anos"),
            ["build.buyTerritory"] = ("Buy a coast hex", "Comprar litoral"),
            ["build.buyTerritoryDetail"] = ("A new hex on the shore, dearer each time",
                "Um hexágono novo na costa, mais caro a cada vez"),

            // Block reasons
            ["block.noTile"] = ("No tile selected", "Nada selecionado"),
            ["block.occupied"] = ("Hex occupied", "Hexágono ocupado"),
            ["block.needs"] = ("Needs {0}", "Requer {0}"),
            ["block.needsExtraction"] = ("Needs {0} extraction", "Requer extração de {0}"),
            ["block.nothingToClear"] = ("Nothing to clear", "Nada para remover"),
            ["block.noNode"] = ("No node", "Nenhum nó"),
            ["block.researched"] = ("Already researched", "Já pesquisado"),
            ["block.research"] = ("Not enough research", "Pesquisa insuficiente"),
            ["block.needsResearch"] = ("Research {0} first", "Pesquise {0} antes"),
            ["block.limit"] = ("Limit of {0} on the island", "Limite de {0} na ilha"),
            ["block.or"] = (" or ", " ou "),

            // Terrain and fuel, lower-case for use inside sentences
            ["relief.grass"] = ("grass", "campo"),
            ["relief.waterfall"] = ("waterfall", "cachoeira"),
            ["relief.mountain"] = ("mountain", "montanha"),
            ["relief.coast"] = ("coast", "litoral"),
            ["relief.any"] = ("any terrain", "qualquer terreno"),
            ["relief.or"] = (" or ", " ou "),
            ["fuel.coal"] = ("coal", "carvão"),
            ["fuel.oil"] = ("oil", "petróleo"),
            ["fuel.uranium"] = ("uranium", "urânio"),
            ["fuel.gas"] = ("gas", "gás"),
            ["fuel.none"] = ("no", "nenhum"),

            // Research panel
            ["research.title"] = ("RESEARCH", "PESQUISA"),
            ["research.points"] = ("{0} pts    +{1:0.#} / yr", "{0} pts    +{1:0.#} / ano"),
            ["research.summary"] = ("{0} of {1} researched  ·  cities generate research, more of it the larger they grow",
                "{0} de {1} pesquisados  ·  cidades geram pesquisa, e mais quanto maiores forem"),
            ["research.done"] = ("RESEARCHED", "PESQUISADO"),
            ["research.cost"] = ("{0} pts", "{0} pts"),
            ["research.free"] = ("FREE", "GRÁTIS"),
            ["research.pendingShort"] = ("not active yet", "ainda inativo"),
            ["research.effect"] = ("EFFECT", "EFEITO"),
            ["research.tradeoff"] = ("TRADEOFF", "CONTRAPARTIDA"),
            ["research.pending"] = ("NOT ACTIVE YET", "AINDA INATIVO"),
            ["research.requires"] = ("REQUIRES", "REQUER"),
            ["research.requiresAny"] = ("REQUIRES ANY ONE OF", "REQUER UM DESTES"),
            ["research.unlock"] = ("RESEARCH  ·  {0} pts", "PESQUISAR  ·  {0} pts"),
            ["research.hint"] = ("Drag to move  ·  scroll to zoom  ·  click a node for details",
                "Arraste para mover  ·  role para dar zoom  ·  clique num nó para detalhes"),
            ["research.branchCity"] = ("City", "Cidade"),
            ["research.branchScience"] = ("Science", "Ciência"),
            ["research.branchEnergy"] = ("Energy", "Energia"),
            ["research.branchIndustry"] = ("Industry", "Indústria"),

            // Event card
            ["event.caption"] = ("EVENT  ·  YEAR {0}", "EVENTO  ·  ANO {0}"),
            ["event.ok"] = ("CONTINUE", "CONTINUAR"),
            ["event.accept"] = ("ACCEPT", "ACEITAR"),
            ["event.decline"] = ("DECLINE", "RECUSAR"),
            ["event.nothing"] = ("Nothing happens", "Nada acontece"),
            ["event.territory"] = ("+{0} territory", "+{0} território"),
            ["event.discount"] = ("{0} {1}% cheaper", "{0} {1}% mais barata"),
            ["event.discountFor"] = ("{0} {1}% cheaper for {2} years", "{0} {1}% mais barata por {2} anos"),
            ["event.war"] = ("War for {0} years", "Guerra por {0} anos"),
            ["event.halved"] = ("Halved: crop rotation protects the fields", "Pela metade: a rotação de culturas protege"),
            ["event.resilient"] = ("High sustainability absorbs 30% of the damage",
                "A sustentabilidade alta absorve 30% do dano"),

            // Events, keyed by asset name
            ["event.Moinho.title"] = ("The Windmill Spreads", "O Moinho se Espalha"),
            ["event.Moinho.text"] = ("Millers teach the craft from village to village.",
                "Moleiros ensinam a técnica de vila em vila."),
            ["event.RodaDagua.title"] = ("Improved Water Wheel", "Roda d’Água Aprimorada"),
            ["event.RodaDagua.text"] = ("Wooden gears give way to precision mechanisms.",
                "Engrenagens de madeira dão lugar a mecanismos de precisão."),
            ["event.RotacaoCulturas.title"] = ("Crop Rotation", "Rotação de Culturas"),
            ["event.RotacaoCulturas.text"] = ("Fallow leaves the fields; the land never rests, but it yields more.",
                "O pousio abandona os campos; a terra nunca descansa, mas rende mais."),
            ["event.CanalEstrada.title"] = ("Canal and Road", "Canal e Estrada"),
            ["event.CanalEstrada.text"] = ("Merchants reach where once only the wind did.",
                "Mercadores chegam onde antes só chegava o vento."),
            ["event.FeiraCidade.title"] = ("Town Fair", "Feira da Cidade"),
            ["event.FeiraCidade.text"] = ("A full square, a stall for everything.", "Praça cheia, banca de tudo."),
            ["event.Hortas.title"] = ("Community Gardens", "Hortas e Pomares Comunitários"),
            ["event.Hortas.text"] = ("Every backyard becomes a pantry.", "Cada quintal vira despensa."),
            ["event.Textil.title"] = ("Textile Mill", "Fábrica Têxtil"),
            ["event.Textil.text"] = ("Investors want to open a mill. Jobs and money, at the price of long shifts and soot.",
                "Investidores querem abrir uma fábrica. Empregos e dinheiro, ao preço de jornadas longas e fuligem."),
            ["event.Bloqueio.title"] = ("Trade Blockade", "Bloqueio Comercial"),
            ["event.Bloqueio.text"] = ("The usual suppliers are cut off. Pay to import, or make do at home?",
                "Os fornecedores de sempre foram cortados. Pagar para importar, ou se virar em casa?"),
            ["event.Bloqueio.import"] = ("IMPORT", "IMPORTAR"),
            ["event.Bloqueio.local"] = ("PRODUCE LOCALLY", "PRODUZIR LOCAL"),
            ["event.Guerra.title"] = ("War Declared", "Guerra Declarada"),
            ["event.Guerra.text"] = ("Your country has gone to war.", "Seu país entrou em guerra."),
            ["event.Desastre.title"] = ("Natural Disaster", "Desastre Natural"),
            ["event.Desastre.text"] = ("Disaster strikes the country.", "Um desastre atinge o país."),
            ["event.Desastre.v0"] = ("A hurricane tears across the country.", "Um furacão atravessa o país."),
            ["event.Desastre.v1"] = ("An earthquake shakes the country.", "Um terremoto sacode o país."),
            ["event.Desastre.v2"] = ("A volcano erupts over the country.", "Um vulcão entra em erupção no país."),
            ["event.OfertaTerritorio.title"] = ("Territory Offer", "Oferta de Território"),
            ["event.OfertaTerritorio.text"] = ("A neighbour offers to sell a stretch of land.",
                "Um vizinho oferece vender um pedaço de terra."),
            ["event.OfertaTerritorio.buy"] = ("BUY  ·  $200", "COMPRAR  ·  $200"),
            ["event.TerritorioConquistado.title"] = ("Territory Conquered", "Território Conquistado"),
            ["event.TerritorioConquistado.text"] = ("You won a territory in the war.", "Você ganhou um território em guerra."),
            ["event.BaixasCivis.title"] = ("Civilian Casualties", "Baixas Civis"),
            ["event.BaixasCivis.text"] = ("The war has reached the cities.", "A guerra chegou às cidades."),
            ["event.Peste.title"] = ("Crop Blight", "Peste na Lavoura"),
            ["event.Peste.text"] = ("Fungus in the fields.", "Fungo nas plantações."),
            ["event.Insatisfacao.title"] = ("Popular Unrest", "Insatisfação Popular"),
            ["event.Insatisfacao.text"] = ("Protests in the squares.", "Protestos nas praças."),
            ["event.Insatisfacao.endure"] = ("LET IT PASS", "DEIXAR PASSAR"),
            ["event.Insatisfacao.pay"] = ("APPEASE  ·  $30", "APAZIGUAR  ·  $30"),
            ["event.Descoberta.title"] = ("Scientific Discovery", "Descoberta Científica"),
            ["event.Descoberta.text"] = ("A treatise changes what is known about energy.",
                "Um tratado muda o que se sabe sobre a energia."),
            ["event.BabyBoom.title"] = ("Baby Boom", "Baby Boom"),
            ["event.BabyBoom.text"] = ("Years of plenty, full houses.", "Anos de fartura, casas cheias."),
            ["event.Ouro.title"] = ("Gold Reserves", "Reservas de Ouro"),
            ["event.Ouro.text"] = ("Miners strike a vein.", "Mineradores encontram um veio."),

            // Tiles, keyed by asset name
            ["tile.City"] = ("City", "Cidade"),
            ["tile.Hidreletrica"] = ("Hydroelectric", "Hidrelétrica"),
            ["tile.Maremotriz"] = ("Tidal", "Maremotriz"),
            ["tile.Eolica"] = ("Wind", "Eólica"),
            ["tile.Solar"] = ("Solar", "Solar"),
            ["tile.Nuclear"] = ("Nuclear", "Nuclear"),
            ["tile.Biomassa"] = ("Biomass", "Biomassa"),
            ["tile.Carvao"] = ("Coal Plant", "Carvão"),
            ["tile.Petroleo"] = ("Oil Plant", "Petróleo"),
            ["tile.ExtracaoCarvao"] = ("Coal Mine", "Extração de Carvão"),
            ["tile.ExtracaoPetroleo"] = ("Oil Well", "Extração de Petróleo"),
            ["tile.ExtracaoGas"] = ("Gas Well", "Extração de Gás"),
            ["tile.ExtracaoUranio"] = ("Uranium Mine", "Extração de Urânio"),
            ["tile.GasNatural"] = ("Gas Plant", "Gás Natural"),
            ["tile.Ondas"] = ("Wave", "Ondas"),
            ["tile.Geotermica"] = ("Geothermal", "Geotérmica"),
            ["tile.Fusao"] = ("Fusion", "Fusão"),
            ["tile.Floresta"] = ("Forest", "Floresta"),

        };

        /// <summary>
        /// Name in the current language for text whose English lives in the asset
        /// itself. Research nodes are named in the research tree artifact, so their
        /// English must not be overridden here or a rename there would never show.
        /// </summary>
        public static string Translated(string key, string english) =>
            Current == Language.Portuguese && Portuguese.TryGetValue(key, out string pt) ? pt : english;

        /// <summary>
        /// Portuguese names for research nodes, keyed by node id. The effect and
        /// tradeoff text is English only for now: the tree is still being written
        /// in the artifact, and translating prose that changes weekly is wasted.
        /// </summary>
        private static readonly Dictionary<string, string> Portuguese = new()
        {
            ["node.center"] = "Ponto Central",
            ["node.cityDev"] = "Desenvolvimento Urbano",
            ["node.university"] = "Universidade",
            ["node.trade"] = "Comércio",
            ["node.urban"] = "Urbanização",
            ["node.cityDev2"] = "Desenvolvimento Urbano 2",
            ["node.hygiene"] = "Investimento em Saneamento",
            ["node.vertical"] = "Verticalização",
            ["node.buildingEff"] = "Eficiência Predial",
            ["node.transit"] = "Transporte Público",
            ["node.gardenCity"] = "Cidade-Jardim",
            ["node.arcology"] = "Arcologia",
            ["node.ev"] = "Veículos Elétricos",
            ["node.nmv5b6gig"] = "Entretenimento",
            ["node.research"] = "Pesquisa",
            ["node.thermo"] = "Termodinâmica",
            ["node.electromag"] = "Eletromagnetismo",
            ["node.atomic"] = "Física Atômica",
            ["node.semis"] = "Semicondutores",
            ["node.biomass"] = "Biomassa",
            ["node.sustBiomass"] = "Biomassa Sustentável",
            ["node.geothermal"] = "Geotérmica",
            ["node.nmv5b5btw"] = "Geotérmica Melhorada",
            ["node.wind"] = "Turbinas Eólicas",
            ["node.offshore"] = "Eólica Offshore",
            ["node.nuclear"] = "Fissão Nuclear",
            ["node.genIV"] = "Reatores de Geração IV",
            ["node.nmv5au1sk"] = "Segurança Nuclear",
            ["node.nmv5avema"] = "Segurança Nuclear II",
            ["node.nmv5az390"] = "Tecnologia Nuclear",
            ["node.nmv5b017x"] = "Armas Atômicas",
            ["node.repository"] = "Repositório Geológico Profundo",
            ["node.fusion"] = "Fusão",
            ["node.solar"] = "Solar",
            ["node.nmv5b3u8p"] = "Solar Melhorada",
            ["node.tandemSolar"] = "Células Solares Tandem",
            ["node.cheapSolar"] = "Solar Barata",
            ["node.rooftop"] = "Solar nos Telhados",
            ["node.computing"] = "Computação",
            ["node.forecast"] = "Previsão do Tempo",
            ["node.superconductors"] = "Supercondutores",
            ["node.storage"] = "Armazenamento de Energia",
            ["node.betterStor"] = "Armazenamento Avançado",
            ["node.pumpedHydro"] = "Hidrelétrica Reversível",
            ["node.exports"] = "Exportação de Energia",
            ["node.coops"] = "Energia Comunitária",
            ["node.hydroPow"] = "Energia Hidrelétrica",
            ["node.tidal"] = "Energia Maremotriz",
            ["node.wave"] = "Energia das Ondas",
            ["node.coalPow"] = "Energia a Carvão",
            ["node.coal2"] = "Energia a Carvão 2",
            ["node.oilDisc"] = "Descoberta do Petróleo",
            ["node.gasDisc"] = "Descoberta do Gás Natural",
            ["node.fracking"] = "Fraturamento Hidráulico",
            ["node.oilEnergy"] = "Energia do Petróleo",
            ["node.oilExt"] = "Extração de Petróleo",
            ["node.oilTrack"] = "Rastreamento de Petróleo",
            ["node.oilExport"] = "Exportação de Petróleo",
            ["node.refinery"] = "Refinaria",
            ["node.safeOil"] = "Petróleo Seguro",
            ["node.filtered"] = "Chaminés com Filtro",
            ["node.ccs"] = "Captura de Carbono",
            ["node.justTrans"] = "Transição Justa",
            ["node.smartGrid"] = "Rede Inteligente",
            ["node.greenH2"] = "Hidrogênio Verde",
            ["node.industrial"] = "Revolução Industrial",
            ["node.massProd"] = "Produção em Massa",
            ["node.mechMining"] = "Mineração Mecanizada",
            ["node.mineReclaim"] = "Recuperação de Minas",
            ["node.envScience"] = "Ciência Ambiental",
            ["node.carbonTax"] = "Imposto sobre Carbono",
            ["node.feedIn"] = "Tarifa Feed-in",
            ["node.coastal"] = "Engenharia Costeira",
            ["node.recycling"] = "Reciclagem",
            ["node.circular"] = "Economia Circular",
            ["node.reforest"] = "Reflorestamento",
            ["node.conservation"] = "Conservação",
            ["node.rewilding"] = "Renaturalização",
            ["node.ecotourism"] = "Ecoturismo",
            ["node.nmv5c081l"] = "Gestão Eficiente",
        };
    }
}
