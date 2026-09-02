# Solarpunk

A solarpunk hex-grid city builder: place tiles on a fixed island, balance
5 global resources (Energy, Money, Sustainability, Population, Happiness),
survive 300 turns.

Full design doc: [docs/GameDesign.md](docs/GameDesign.md)
(original PDF kept alongside it for reference).

## Stack

Unity 6 (`6000.2.1f1`), C#.

## Getting started

1. Open this folder with **Unity Hub** → Add → select `Solarpunk`. It'll use
   the `6000.2.1f1` editor.
2. Open **`Assets/_Game/Scenes/Game.unity`**.
3. Hit Play.

### Controls (placeholder — no UI yet)

- **Number keys 1-9, 0** — select a tile to build: `1` City, `2` Hidrelétrica,
  `3` Maremotriz, `4` Eólica, `5` Solar, `6` Nuclear, `7` Biomassa, `8` Carvão,
  `9` Petróleo, `0` Extração.
- **Left click a hex** — build the selected tile there (if the relief allows
  it and you can afford it).
- **Space** — advance one turn (one year). Console logs the 5 resources after
  each turn, plus victory/defeat.

Regenerate the board/tiles/scene at any point via the Unity menu
**Solarpunk → Build Initial Scene** (recreates the hex prefab + scene) and
**Solarpunk → Generate Starting Tile Definitions** (recreates the 10 tile
assets) — both are idempotent, safe to rerun after tweaking the generator
code in `Assets/Editor/`.

## Project layout

```
Assets/_Game/
  Scripts/
    Core/      resource types + the per-turn ResourceVector every tile uses
    Grid/      hex coordinates, relief, cell + grid generation
    Tiles/     TileDefinition data asset, city growth, build controller
    Managers/  ResourceManager, TurnManager, GameManager
    Debug/     keyboard/console stand-in for a real HUD
  Data/
    TileDefinitions/   the 10 tile assets (city, 8 power plants, extraction)
  Scenes/
    Game.unity          the playable bootstrap scene
  Prefabs/
    HexCellPrototype     placeholder colored-cube hex (swap for real art later)
Assets/Editor/
    SceneBootstrapper.cs   builds the hex prefab + scene from code
    TileDataFactory.cs     generates the 10 tile assets from code
docs/
    GameDesign.md              transcribed design doc
    solarpunk-game-design.pdf  original
```

## Status

Playable simulation loop: hex grid with rolled relief, 10 buildable tiles
with placeholder (untested) balance numbers, click-to-build, 5-resource
tracking with blackout/win/loss, hybrid city growth. No real art, UI, random
events, territory expansion, or research-currency system yet — see
`docs/GameDesign.md` for what's designed but unbuilt.

Balance numbers in `TileDataFactory.cs` are made up to get something
running — the design doc only specifies qualitative profiles ("alta
energia", "custo médio-alto"), not numbers. Tune freely.
