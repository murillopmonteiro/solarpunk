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

The scene, the hex prefab, the hexagon mesh, all 10 tile assets, every material,
the city prefab, and the entire HUD are produced by scripts in `Assets/Editor/`.

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

# Tests
"$U" -batchmode -nographics -projectPath "$P" \
  -runTests -testPlatform EditMode -testResults <results.xml> -logFile <log>

# Windows player (also used by the screenshot harness)
"$U" -batchmode -nographics -projectPath "$P" \
  -executeMethod Solarpunk.EditorTools.PlayerBuilder.BuildWindowsPlayer -quit -logFile <log>
```

Equivalent menu items inside the Editor: `Solarpunk → Build Initial Scene`,
`Validate Scene`, `Build Windows Player`, `Build City Model Prefab`,
`Generate Starting Tile Definitions`, `Generate Hex Mesh`.

### Seeing the game without the Editor

`DevScreenshots` is inert unless the player is launched with `-autoshot`. Given
that flag it drives the game through its UI states and writes a PNG of each,
which can then be read directly.

```bash
"$P/Builds/Solarpunk/Solarpunk.exe" -autoshot <outdir> \
  -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile <log>
```

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
    UI/        code-built HUD                      (leaf: nothing depends on it)
    Dev/       DevScreenshots capture harness
  Data/TileDefinitions/   the 10 tile assets
  Meshes/ Prefabs/ Materials/ Scenes/Game.unity
Assets/Editor/
  SceneBootstrapper   assembles the whole scene
  TileDataFactory     generates the 10 tile assets
  CityModelBuilder    FBX -> game-ready prefab
  HexMeshFactory      generates the hexagon mesh
  PlayerBuilder       Windows player build
  SceneValidator      catches wiring errors a compile will not
ArtSource/            Blender sources, outside Assets/ on purpose
docs/                 GameDesign.md, TileStats.xlsx
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

**The city's per-turn effect is constant. It does not scale with level.** This
was a real bug: `GetEffectForLevel` multiplied by level, so auto-growth pushed
the city to level 8 by year 9 and runs ended on happiness with 92% of
sustainability unspent. If anything reintroduces scaling, `BalanceTests` fails.

Defeat when sustainability or happiness hits 0. Victory at turn 300.

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

**Unity layout groups are unpredictable.** They inflated the resource bar to 4x
its configured height. The HUD positions everything explicitly against corner
anchors via `UIFactory.Place`. Keep it that way.

**Blender to Unity FBX export** needs `bake_space_transform=True` with
`axis_forward='-Z'`, `axis_up='Y'`, or the model imports on its side. Recipe in
`ArtSource/README.md`. Assign materials Unity-side, not from the FBX.

**Blender MCP** requires Blender running with the MCP addon enabled and
"Connect to MCP server" clicked. Only one client at a time.

---

## State

Working: board with terrain restrictions, click-to-select, build and demolish,
five-resource economy with blackout penalty, turn loop, win/loss and restart,
level 1 city model, full HUD.

**Pending: PR #1** (`architecture/assemblies-and-tests`) adds six assembly
definitions and 18 EditMode tests. Not yet merged. Until it is, there are no
assembly boundaries and no test suite on `master`.

### Not built

- **Research (`pesquisa`)** — the design doc has the city generating it, but it
  sits outside the 5-resource vector and needs its own system.
- **Random yearly events** — doc says one per turn.
- **Territory expansion** — doc says via event or paid purchase.
- **Extraction gating** — `requiresExtraction` is set on Nuclear/Carvão/Petróleo
  but nothing enforces a penalty when extraction is absent. Data only.
- **Mountain wind bonus** for Eólica — in the doc, not in code.
- **City models for levels 2-10** — only level 1 exists. Suggested approach is
  swapping the prefab at thresholds, not modelling ten.

### Open design question

**City level is currently mechanically inert.** It still ticks up each year and
the paid upgrade still charges $200-$1800, but nothing about the output
changes, because the effect is constant by design. Level needs to buy
*something*. Options discussed: population capacity, visual-only progression,
or driving research output (most faithful to the doc, since research sits
outside the vector and so leaves G constant by construction).

---

## Working style that has held up

Murillo designs the balance; do not silently redesign his numbers. When his
arithmetic implies something the code contradicts, say so explicitly and show
the calculation rather than quietly picking one.

Verify before claiming. Screenshots for anything visual, `SceneValidator` for
wiring, tests for behaviour. Several confident claims in this project turned out
to be wrong when actually checked.
