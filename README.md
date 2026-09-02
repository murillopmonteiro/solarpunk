# Solarpunk

A solarpunk hex-grid city builder: place tiles on a fixed island, balance
5 global resources (Energy, Money, Sustainability, Population, Happiness),
survive 300 turns.

Full design doc: [docs/GameDesign.md](docs/GameDesign.md)
(original PDF kept alongside it for reference).

## Stack

Unity 6 (`6000.2.1f1`), C#, built-in render pipeline, uGUI.

## Run it

1. **Unity Hub** → Add → select this folder → open with `6000.2.1f1`.
2. Open **`Assets/_Game/Scenes/Game.unity`**.
3. Press **Play**.

### What you can do

- **Click a hexagon** — it lifts and brightens, and the right-hand panel
  shows its terrain plus everything buildable on it.
- **Click a build option** — a placeholder structure drops on the hex and
  the cost comes out of your money. Options you can't use are greyed out
  with the reason on them (`Needs Waterfall`, `Not enough $`).
- **Demolish** a built hex to clear it and get half the cost back. The city
  additionally offers a paid instant level-up.
- **NEXT YEAR button (or Space)** — resolves one turn. Every stat in the top
  bar shows its current value and its projected per-turn change.

## Current test scene

A hand-authored 10-hex island (rows of 3/4/3) with a deliberate terrain
spread so every relief restriction in the design doc is reachable in one
sitting:

| Terrain | Count | Unlocks |
|---|---|---|
| Waterfall | 1 | Hidrelétrica (only here) |
| Coast | 2 | Maremotriz (only here) |
| Mountain | 2 | Eólica bonus (not yet implemented) |
| Open | 5 | anything unrestricted |

Randomised board generation comes back once the systems are proven — a
fixed board makes the mechanics reproducible to test.

## Regenerating

Everything generated lives behind menu items, all idempotent and safe to
rerun after editing the generator code in `Assets/Editor/`:

- **Solarpunk → Build Initial Scene** — rebuilds mesh, prefab, tile assets
  and the whole scene.
- **Solarpunk → Generate Starting Tile Definitions** — just the 10 tile assets.
- **Solarpunk → Generate Hex Mesh** — just the hexagon mesh.
- **Solarpunk → Validate Scene** — checks for unassigned references, a
  missing font, a prefab without a collider. Also runs headless:

```bash
"C:\Program Files\Unity\Hub\Editor\6000.2.1f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -executeMethod Solarpunk.EditorTools.SceneValidator.Validate -quit -logFile validate.log
```

## Project layout

```
Assets/_Game/
  Scripts/
    Core/      ResourceVector — the per-turn 5-resource effect every tile has
    Grid/      hex coordinates, relief, cell, board generation, click selection
    Tiles/     TileDefinition data asset, build rules, city growth, placeholder art
    Managers/  ResourceManager, TurnManager, GameManager
    UI/        code-built HUD: resource bar, contextual build panel
  Data/TileDefinitions/   the 10 tile assets
  Meshes/HexPrism         generated hexagon mesh
  Prefabs/HexCell         hex prefab (mesh + collider + HexCell)
  Scenes/Game.unity       the playable scene
Assets/Editor/
  HexMeshFactory      generates the hexagon mesh
  TileDataFactory     generates the 10 tile assets
  SceneBootstrapper   assembles the entire scene from code
  SceneValidator      catches wiring errors a compile won't
docs/
  GameDesign.md / solarpunk-game-design.pdf
```

The HUD is built from code at runtime rather than authored in the scene, so
it regenerates cleanly and there's no scene wiring to break while the layout
is still churning.

## Status

Working: board, terrain restrictions, selection, build/demolish, the 5-stat
economy with blackout penalty, hybrid city growth, win/loss + restart.

Not built yet: real art, random yearly events, territory expansion, the
research currency (the design doc has the city generating "pesquisa", which
sits outside the 5-resource vector and needs its own system), extraction
actually gating fossil/nuclear operation, and the mountain bonus for wind.

Balance numbers in `TileDataFactory.cs` are invented — the design doc
specifies qualitative profiles ("alta energia, custo médio-alto"), not
numbers. Expect to retune them.
