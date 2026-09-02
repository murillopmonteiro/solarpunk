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
2. Open the empty scene Unity created for you (`Assets/Scenes`), or create a
   new one, and add:
   - An empty GameObject with `HexGridManager` — assign a hex cell prefab
     (a simple hex mesh/sprite with a `HexCell` component on it).
   - An empty GameObject with `ResourceManager`.
   - An empty GameObject with `TurnManager` — wire in the grid + resource
     manager references.
   - An empty GameObject with `GameManager` — wire in the same references.
3. Create the 8 power plant + city + extraction tile assets via
   **Assets → Create → Solarpunk → Tile Definition**, one per tile type from
   the design doc's tables, and drop them in
   `Assets/_Game/Data/TileDefinitions`.
4. Hit Play — `GameManager` generates the board on `Start()`.

## Project layout

```
Assets/_Game/
  Scripts/
    Core/      resource types + the per-turn ResourceVector every tile uses
    Grid/      hex coordinates, relief, cell + grid generation
    Tiles/     TileDefinition data asset, city growth logic
    Managers/  ResourceManager, TurnManager, GameManager
  Data/
    TileDefinitions/   ScriptableObject instances (city, 8 power plants, extraction)
docs/
    GameDesign.md              transcribed design doc
    solarpunk-game-design.pdf  original
```

## Status

Core simulation skeleton only: hex grid generation with rolled relief,
5-resource tracking, turn loop, city growth stub. No scene, prefabs, art,
UI, events, or the 8 tile-definition assets yet — see `docs/GameDesign.md`
for what's designed but unbuilt (random events, territory expansion,
research tree).
