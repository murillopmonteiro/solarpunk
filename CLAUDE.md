# Solarpunk

Hex-grid city builder. You place tiles on a fixed island, balance five global
resources, and try to survive 300 turns. One turn is one in-game year.

Repo: `murillopmonteiro/solarpunk` · Unity **6000.2.1f1** · C# · Built-in render
pipeline · uGUI.

Design doc: `docs/GameDesign.md` (Portuguese, authored by Murillo). It is the
source of truth for intent; where this file and the doc disagree, the doc is
the design and this file is the implementation status.

---

## The one rule that matters

**Nothing in this project is hand-authored. Everything is generated from code.**

The scene, the hex prefab, the hexagon mesh, all 18 tile assets, every material,
the city prefab, the tree mesh, and the entire HUD are produced by scripts in
`Assets/Editor/`.

`Solarpunk → Build Initial Scene` wipes and rebuilds all of it in one pass.

So: **do not hand-edit the scene and expect it to survive.** The next rebuild
overwrites it. Anything permanent goes into the generator that produces it.

This exists because the Editor cannot be seen from a terminal session. Making
the scene an output means changes are readable C# instead of blind edits to
serialized YAML.

---

## Commands

Unity is at `C:\Program Files\Unity\Hub\Editor\6000.2.1f1\Editor\Unity.exe`.

```bash
U="/c/Program Files/Unity/Hub/Editor/6000.2.1f1/Editor/Unity.exe"
P="C:/Users/mauri/Claude/Projects/Solarpunk"

# Rebuild everything (mesh, prefabs, tile assets, materials, scene)
"$U" -batchmode -nographics -projectPath "$P" \
  -executeMethod Solarpunk.EditorTools.SceneBootstrapper.BuildInitialScene -quit -logFile <log>

# Check wiring: unassigned references, missing font, collider-less prefab
"$U" -batchmode -nographics -projectPath "$P" \
  -executeMethod Solarpunk.EditorTools.SceneValidator.Validate -quit -logFile <log>

# Unlock real research nodes and check the numbers they produce (exit 1 on failure)
"$U" -batchmode -nographics -projectPath "$P" \
  -executeMethod Solarpunk.EditorTools.ResearchSmokeCheck.Run -quit -logFile <log>

# Tests
"$U" -batchmode -nographics -projectPath "$P" \
  -runTests -testPlatform EditMode -testResults <results.xml> -logFile <log>

# Windows player (also used by the screenshot harness)
"$U" -batchmode -nographics -projectPath "$P" \
  -executeMethod Solarpunk.EditorTools.PlayerBuilder.BuildWindowsPlayer -quit -logFile <log>
```

Equivalent menu items inside the Editor: `Solarpunk → Build Initial Scene`,
`Validate Scene`, `Build Windows Player`, `Build City Model Prefab`,
`Build Tree Model Prefab`, `Generate Starting Tile Definitions`,
`Generate Research Tree`, `Generate Hex Mesh`, `Generate Sea Mesh`.

### Seeing the game without the Editor

`DevScreenshots` is inert unless the player is launched with `-autoshot`. Given
that flag it drives the game through its UI states and writes a PNG of each,
which can then be read directly.

```bash
"$P/Builds/Solarpunk/Solarpunk.exe" -autoshot <outdir> \
  -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile <log>
```

Add `-lang pt` (or `-lang en`) to capture a given language; the last shot flips
language mid-game to exercise the HUD rebuild.

This is the only way to actually look at the UI from a terminal session. Use it
before claiming anything about how the game looks. It has caught a resource bar
eating 30% of the screen, rows rendering upside down, and rounded corners that
did not exist in builds.

### When Unity is open

The Editor holds a project lock. Batch mode then exits **21** with "another
Unity instance is running". Either ask for it to be closed, or copy
`Assets/`, `Packages/`, `ProjectSettings/` to a scratch directory and build
there. The scratch copy works fine and avoids interrupting anyone.

Do not run two Unity instances against different projects at once either; it
disturbs package resolution.

---

## Layout

```
Assets/_Game/
  Scripts/
    Core/      ResourceVector, ResourceType       (depends on nothing)
    Grid/      hex coords, relief, cell, board generation, click selection
    Tiles/     TileDefinition, BuildController, CityGrowth, StructureFactory
    Managers/  ResourceManager, TurnManager, GameManager
    Research/  ResearchNode, ResearchManager       (points + tree, outside ResourceVector)
    UI/        code-built HUD                      (leaf: nothing depends on it)
    Dev/       DevScreenshots capture harness
  Data/TileDefinitions/   the 18 tile assets
  Data/ResearchNodes/     the research tree assets
  Resources/Fonts/        Fredoka + Nunito (OFL), loaded by UIFactory
  Shaders/                VertexColorUnlit, for the sea
  Meshes/ Prefabs/ Materials/ Scenes/Game.unity
Assets/Editor/
  SceneBootstrapper   assembles the whole scene
  TileDataFactory     generates the 18 tile assets
  ResearchTreeFactory generates the research nodes from docs/ResearchTree.json
  ResearchSmokeCheck  unlocks nodes headless and checks their effects
  EventDeckFactory    generates Murillo's 18 event cards (numbers + draw rules; text in Loc)
  CityModelBuilder    FBX -> game-ready prefab
  CoalMineModelBuilder  coal extraction FBX -> game-ready prefab
  CoalPlantModelBuilder coal thermal FBX -> game-ready prefab
  HydroModelBuilder   hydroelectric FBX -> game-ready prefab
  TreeModelBuilder    tree-basic.stl -> mesh asset + prefab
  HexMeshFactory      generates the hexagon mesh
  SeaMeshFactory      generates the faceted sea the island sits in
  PlayerBuilder       Windows player build
  SceneValidator      catches wiring errors a compile will not
ArtSource/            Blender + STL sources, outside Assets/ on purpose
docs/                 GameDesign.md, TileStats.xlsx, ResearchTree.json
```

### Architecture, honestly

`Core` is genuinely isolated. `UI` is genuinely a leaf. But **`Grid`, `Tiles`
and `Managers` are mutually dependent** — a cell holds a tile, build rules check
resources, the turn loop walks both. It is a knot, not a layering. Do not
describe it as layered.

`ResourceVector` is the spine: `[energy, money, sustainability, population,
happiness]` with `+` and `*` operators. Every tile effect, every turn, every
bonus is one of these. New mechanics that fit this shape are cheap; ones that
do not (research, see below) need their own system.

Tile stats are **data, not code** — ScriptableObjects in `Data/TileDefinitions/`.
`BuildController` never knows what a coal plant is; it reads a definition,
checks relief, charges cost, applies the vector. Adding an eleventh tile
requires no logic change.

Fuel works the same way: an extraction tile declares `producedFuel`, a plant
declares `requiredFuel`, and `BuildController` matches the two by enum across the
whole board. A new extraction type is a `FuelType` value plus an asset entry.

Terrain rules are data too. `allowedRelief` is a set — empty means anywhere, so
"grass or mountain" is expressible where the old single-relief field could only
say one. `TerrainBonus` multiplies one resource and the build cost on a given
relief. Anything that varies by hex goes through `TileDefinition.EffectOn` and
`BuildCostOn`; read `perTurnEffect` or `buildCost` directly and you will silently
miss the bonus.

Research is the one system deliberately *not* built on ResourceVector. Points are
a single float on `ResearchManager`, earned each year from the levels of every
city, and spent on `ResearchNode` assets whose prerequisites are other nodes.

**The tree is designed outside the repo**, in an editable artifact page Murillo
works in (https://claude.ai/artifact/7yeMJcLU5jASCLC9japdYV). Its `tree.json` is
copied to `docs/ResearchTree.json`, and `ResearchTreeFactory` generates one node
asset per entry from it: name, cost, position, prerequisites (`also` + `any` =
"any one of"), effect and tradeoff text. To change the tree: export the file
again, replace it, run `Generate Research Tree` (or Build Initial Scene).

What a node *does* is not in the JSON, because the artifact describes effects in
prose. `ResearchTreeFactory.Effects` maps node id to `ResearchEffect` data
(tile output, build cost, research income, city level cap, tile unlock, refunds);
`Pending` says why a node does nothing yet, and that note shows on its card. A
node with neither gets "No effect wired yet", so a node added in the artifact
appears in game before anyone wires it. Effects are deltas against current tile
stats, so check them when tile numbers change.

`ResearchManager` is the only reader of effects. Everything that prices a tile
goes through `TileEconomy` (base, then research adds and multipliers, then the
terrain bonus): the turn loop, `BuildController`, the build panel. Read
`EffectOn`/`BuildCostOn` directly and research silently goes missing.

Tiles named by an `UnlockTile` effect are locked until that node is researched;
the start palette is City, Coal Mine, Coal Plant and Hydro. City level is capped
at 2 until City Development (3), then 5, 7, 10. Tradeoffs that switch on above a
city level are research modifiers on top of the constant city row, not the old
level multiplier.

Player-facing text is in English and PT-BR, in one table: `Core/Loc.cs`. UI code
calls `Loc.T("key")`, never a string literal. Tiles and research nodes are keyed by
asset name (`tile.Carvao`), read through `TileDefinition.LocalizedName` /
`ResearchNode.LocalizedName`; `displayName` stays the stable identifier the
harness matches on, and is the fallback when an asset has no entry. Language is
the saved choice, else the system language; `-lang pt|en` forces it. Switching
in game rebuilds the whole HUD canvas (`GameHUD.Rebuild`), so labels need no
refresh logic of their own. PT strings run longer — check new ones in the PT
autoshot pass.

Forests are data rather than a special case: `clearCost` makes demolition charge
instead of refund, and `maturesAfter` (10 for forest) makes a planted one yield
nothing until it has grown in. The board seeds forests already grown; Reforestation
unlocks planting them from the build menu. `playerBuildable` still exists for a
future terrain feature that should never be built.

**Board rules that are not one tile's output.** `HexCell.Neighbours` is kept by
`HexGridManager` as hexes spawn (territory included). A research `TileOutput`
effect with `nextTo` applies only when a grown neighbour matches, in the same
target syntax (`Floresta`, `cat:City`, `tag:fossil`) — Garden City, Sustainable
Biomass, Wind's noise and Community Energy use it. Previews have no hex, so a
neighbour bonus shows only once built. `HexCell.age` is the hex timer: the turn
loop ages every tile a year, then fires `FellForest` (Biomass, every 20) and
`SpreadForest` (Rewilding, every 25). `maxOnBoard` caps a tile (one oil well)
until a `LiftBuildLimit` effect (Oil Tracking). `BuyTerritory` (Coastal
Engineering) puts a coast hex on sale in the build panel when nothing is
selected, $600 then ×1.5 each.

The build panel lists only what the selected hex's terrain allows and research
has unlocked, and scales itself down past ~10 rows to stay clear of the
bottom-right buttons.

---

## Balance model

Murillo's notation, used in conversation: **G = [E, M, S, P, H]** = energy,
money, sustainability, population, happiness, per year.

Starting resources: E 20 · M 2000 · **S 1000** · P 10 · H 100.

**Sustainability runs 0-1000, not 0-100.** The triads are whole numbers per
year, and 1000 is what makes the horizon arithmetic land: a coal setup drains
5/yr, so 1000 / 5 = collapse at year 200. On a 100 budget the same numbers
collapse at year 20. Happiness is still 0-100.

The reference triad, which the tests assert:

| | E | M | S | P | H |
|---|---|---|---|---|---|
| City (any level) | -8 | -2 | -1 | +5 | 0 |
| Coal Extraction | 0 | +20 | -1 | 0 | 0 |
| Coal Thermal | +10 | -3 | -3 | 0 | 0 |
| **Total** | **+2** | **+15** | **-5** | **+5** | **0** |

Founding a city also gives a one-off **+5 happiness** (`TileDefinition.onBuildBonus`).

Coal extraction is limited to **grass or mountain** — no mining the coast or the
waterfall. On a mountain it runs a **20% bonus**: $400 to open instead of $500,
and +24 money a year instead of +20. Mountains also carry no forest, so building
there skips the $200 clearing too — the mountain mine is $300 cheaper all in.

The island also starts **forested**: every open-ground hex but one carries a
forest at G = [0, 0, +3, 0, 0]. Four of them is +12 sustainability a year, which
swamps the triad's -5 — so the real clock on a run is how much forest has been
felled, not the coal alone. Clearing one costs **$200** and pays nothing back
(`TileDefinition.clearCost`).

The triad is **one** city. Cities are not capped — the island can hold as many as
it has hexes for, and each one adds its own flat row to the sum. Two cities is
E -16, P +10, and S -2 before any plant, which is a real change to the horizon
arithmetic: the 200 year coal number assumes a single city.

**The city's per-turn effect is constant. It does not scale with level.** This
was a real bug: `GetEffectForLevel` multiplied by level, so auto-growth pushed
the city to level 8 by year 9 and runs ended on happiness with 92% of
sustainability unspent. If anything reintroduces scaling, `BalanceTests` fails.

Defeat when sustainability or happiness hits 0. Victory at turn 300.

**Money can go negative: that is debt, with no limit and no interest** (Murillo's
call). Nothing checks the balance before spending — `ResourceManager.Spend` never
refuses. The one balance gate left is a design rule, the territory offer card's
"only appears with $200".

**Energy Exports** sells the whole yearly energy surplus at $1 a unit (−0.5
happiness in an exporting year), applied in `TurnManager.CalculateTurnDelta`, so
the HUD preview shows it. The tree's text says "above the storage cap"; there is
no cap yet, so the surplus is everything above consumption.

Event cards: 5% chance in an ordinary year, plus a guaranteed card every 5th year.

---

## Known gotchas

Each of these cost real debugging time.

**Asset references resolved before `EditorSceneManager.NewScene` become
destroyed "fake nulls".** Creating a scene unloads unused assets. The build
palette silently serialized as all-null for exactly this reason. Resolve asset
references *after* the new scene exists.

**`Resources.GetBuiltinResource` only works in the Editor.** `UI/Skin/*.psd`
loads fine in play mode and silently fails in a player build, rendering every
panel square. UI shapes are now generated procedurally at runtime instead.

**`Object.Destroy` throws outside play mode.** Use `StructureFactory.DestroySafely`,
which picks `Destroy` or `DestroyImmediate` by context.

**Unity's `Mathf.SmoothStep` is not GLSL smoothstep.** It interpolates between
two values. `UIFactory.Smooth01` is the real one.

**Renaming a serialized field silently drops its value.** Add
`[FormerlySerializedAs]` or scenes deserialize to zero with no error.

**The HUD look lives in `UIFactory`.** Cream panels with a timber slab under them
(a uGUI `Shadow`, so it follows resizes), cut corners, six-facet hexagon badges and
the resource glyphs are all rasterised there at runtime. Labels marked `Bold` are set
in Fredoka, the rest in Nunito; a missing font file falls back to the built-in one
silently, which `SceneValidator` catches. Unity `Text` truncates a line taller than
its rect instead of clipping it, so a big number in a tight rect simply vanishes.

**Unity layout groups are unpredictable.** They inflated the resource bar to 4x
its configured height. The HUD positions everything explicitly against corner
anchors via `UIFactory.Place`. Keep it that way.

**Unity cannot import STL at all.** It is not in the supported model formats, so
`ArtSource/tree-basic.stl` is parsed by `TreeModelBuilder` and baked into a mesh
asset instead. Two things that mesh conversion always needs: STL is **Z-up**
(swap Y and Z), and that swap mirrors handedness, so every triangle has to be
wound backwards or the model renders inside out. STL also carries no materials,
so the trunk/leaf split is inferred from the shape of each disconnected shell.

**Blender to Unity FBX export** needs `bake_space_transform=True` with
`axis_forward='-Z'`, `axis_up='Y'`, or the model imports on its side. Recipe in
`ArtSource/README.md`. Assign materials Unity-side, not from the FBX.

**The FBX round-trip orders submeshes by each material's first use in the face
list, not by material slot index.** Both model builders assign Unity materials
by index, so a mesh whose faces are not grouped by material arrives with its
whole palette permuted — timber rendered as rock, grass as stone. It fails
silently; nothing logs, and the slot *count* is still right. `build_coalmine.py`
sorts its faces by material index before `from_pydata` for exactly this reason.
To check, compare per-submesh triangle counts on both sides — they must line up
in the same order.

**Blender MCP** requires Blender running with the MCP addon enabled and
"Connect to MCP server" clicked. Only one client at a time.

---

## State

Working: board with terrain restrictions, click-to-select, build and demolish,
five-resource economy with blackout penalty, turn loop, win/loss and restart,
level 1 city, coal extraction pithead, coal thermal and hydroelectric models,
full HUD, unlimited cities,
coal plants gated on coal extraction, forests seeded on open ground and felled
for a fee, per-relief build rules with terrain bonuses, the 80-node research tree from
the artifact with 56 nodes wired to real effects (pannable, zoomable overlay;
plants locked behind research; city level capped by research), English and PT-BR text
with an in-game toggle, 18 yearly event cards on a full-screen card that blocks the
year until answered (choices, war window, territory purchase/conquest growing the
island by a hex, timed and permanent build discounts).

Models are drafted in code and finished by hand. `ArtSource/build_city.py`,
`build_coalmine.py`, `build_coalplant.py` and `build_hidreletrica.py` assemble a
model from boxes, beams, cylinders, tubes and cones, all sharing
`ArtSource/lowpoly.py` — which owns the primitives, the material-order sort and
the FBX export settings, and is what keeps the set stylistically consistent.
Start a new model by importing it.

The whole procedure — reading references, the board-camera angle to judge at,
the hexagon fit, the Unity wiring, the in-game verification — is written up as a
personal skill at `~/.claude/skills/solarpunk-tile-model/`, which also bundles
`report_model()` / `fit_scale()` / `frame_board_camera()` as Blender helpers.
It lives outside the repo, so it is not versioned with the project.

**But the script is not automatically the source of truth.** Murillo edits the
result in Blender, and once he has, the `.blend` is what ships and the script is
a header marked SUPERSEDED listing the measured deltas. That is the case for the
city and the coal plant; only the mine still round-trips through its script.
So: **read the header before running any build_*.py — running a superseded one
silently discards hand edits.** Reconciling a script back to its `.blend` means
measuring the deltas and verifying the rebuild against the recorded triangle and
per-material counts, not eyeballing it.

`CityLevel1.blend` / `CityLevel1.fbx` are the superseded hand-modelled town,
unreferenced but kept as a record. The generated city ships from `City.fbx`; the
prefab is still `CityLevel1.prefab`, since the roadmap swaps prefabs per level.

Two constants on every generated prefab's root, not baked into the mesh, so the
scripts' coordinates keep reading literally: `DisplayScale` fits the model to the
hex, and `DisplayYaw` turns it to face the fixed board camera. The FBX axis
conversion lands a model facing *away* from that camera, so `DisplayYaw` is
around -175 for both. Changing the camera's yaw means revisiting both.

**Pending: PR #1** (`architecture/assemblies-and-tests`) adds six assembly
definitions and 18 EditMode tests. Not yet merged. Until it is, there are no
assembly boundaries and no test suite on `master`.

### Not built

- **19 research nodes, plus parts of 5 more** — in the tree, marked "not active
  yet" with the reason (the 5 science gates do nothing by design). They wait on
  systems that do not exist: an energy storage cap, per-tile risk rolls (nuclear
  accidents, geothermal quakes, spills), wind/solar intermittency, research that
  changes where a tile may be built (Offshore Wind), fuel-free hydrogen plants. Node effect and tradeoff text is English only; names are
  translated.
- **Eras** — the event deck has Murillo's Era 1 cards, but no era system, so they
  stay in the deck all run alongside the general ones.
- **Extraction gating, the operating half** — coal is gated at *build* time
  (`BuildController.BlockReason`), but the doc wants fossil/nuclear plants to run
  *with a penalty* when their supply is demolished later. The turn loop does not
  look at fuel at all yet.
- **Models for the seven newer tiles** — Oil/Gas/Uranium extraction, Gas plant,
  Wave, Geothermal and Fusion render as primitive placeholders. Their numbers come
  from the research tree text where it gives them, the rest are guesses to tune.
- **Mountain wind bonus** is in: +20% energy and +20% cost, the number the
  research tree gives.
- **City models for levels 2-10** — only level 1 exists. Suggested approach is
  swapping the prefab at thresholds, not modelling ten.

### Open design question

**City level now buys research, and nothing else.** It was mechanically inert;
levels are now summed across the board each year and multiplied by
`ResearchManager.pointsPerCityLevel` to price the year's research income, which
is the doc-faithful option of the three that were on the table — research sits
outside the vector, so paying it out of level leaves G constant by construction.

Research now buys real effects, so city growth finally reaches numbers the player
feels. The open part is the budget: the tree costs far more than a run earns at
1 point per level per year, which is intended (a run buys part of it), but the
share has not been playtested.

---

## Working style that has held up

Murillo designs the balance; do not silently redesign his numbers. When his
arithmetic implies something the code contradicts, say so explicitly and show
the calculation rather than quietly picking one.

Verify before claiming. Screenshots for anything visual, `SceneValidator` for
wiring, tests for behaviour. Several confident claims in this project turned out
to be wrong when actually checked.
