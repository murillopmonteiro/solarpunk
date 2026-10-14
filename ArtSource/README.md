# Art source

Editable sources for generated models. Kept outside `Assets/` on purpose: Unity
imports `.blend` files natively, which would make every asset import depend on a
working Blender install.

## lowpoly.py

The shared primitive toolkit: `box`, `taper`, `beam`, `cylinder`, `tube`, `cone`,
`tuft`, plus `finish()` and `export()`. Every generated model imports it, which
is what makes the set look like one hand rather than several.

`finish()` does two things worth knowing. It sorts faces by material index —
load-bearing, see the note below — and it asserts that the resulting first-use
order *is* slot order, so a future model cannot ship a permuted palette silently.

## Hidreletrica.blend / build_hidreletrica.py

The hydroelectric plant: a concrete dam with a spillway sheeting into a plunge
pool, a red brick powerhouse with its tailrace arch, a timber landing stage and
rock islets between green banks. 744 triangles, eleven material slots, authored ~1.04 units wide — a
dam has to span its hex, so the footprint is wider than the other tiles'.

Brick, slate, iron and glass are the power station's exact values.

This tile only ever sits on a **Waterfall** hex, which is already bright blue,
and that changes what the model should contain. Two things learned from the
build, neither visible in Blender:

- **Do not lay down water.** A raised WATER slab standing for the impounded head
  read as a dull flat panel — a second, darker blue rectangle on a blue floor
  looks like a wall, not a reservoir. The hex *is* the river. What the hex cannot
  show is movement, so the model supplies only that: the sheet on the spillway,
  the plunge pool, ripples drifting toward the spillway mouth, and wedges of foam
  fanning downstream.
- **Nature needs somewhere to stand.** Loose grass tufts floated and bare rocks
  read as grey crates dropped in the water. Planting mid-channel now sits on rock
  islets — a boulder with something growing out of it, which is what survives in
  a river — and everything else sits on the banks.
- **The structures need banks.** Standing straight on the water, the dam and the
  powerhouse read as floating, and a dam with nothing to abut loses the reason it
  exists. `Hydro_Land` is HexCell's open-ground green *exactly*, so the banks read
  as the same land as the neighbouring tiles. Each bank is one mass plus a couple
  of rotated chunks overlapping its edge: a single rectangle reads as a green card
  laid on the river, and the overlaps break the straight edge into something
  shoreline-shaped. Tufts on the banks are planted at bank height, not z=0 — at
  ground level they stand in the river bed.

Also worth knowing: the spillway's water sheet has to be offset along the
chute's **own normal**, not in world y/z. Offsetting the obvious way buries a
0.007 slab inside a 0.014 one and the spillway renders as bare concrete.

Slot order matches `Palette` in `Assets/Editor/HydroModelBuilder.cs`:

```
0 Hydro_Brick  1 Hydro_Concrete  2 Hydro_Roof   3 Hydro_Timber  4 Hydro_Iron
5 Hydro_Glass  6 Hydro_Water     7 Hydro_Foam   8 Hydro_Rock    9 Hydro_Grass
10 Hydro_Land
```

## City.blend / build_city.py

The level 1 city: brick and plaster terraces under tile and slate roofs, facing a
cobbled street, with a stone civic hall and a corner clock tower closing the far
end. 616 triangles, eight material slots, authored ~1.07 units wide.

Brick, stone, slate, iron and glass are the power station's exact values — the
town that works the pit is built of the same material as the pit. Tile is the
warm roof red the hand-modelled city already used, kept because it is what makes
the city legible against the station's grey from across the board.

Two things learned building it, both visible only in a player build:

- **Walls and roofs need to differ in value, not just hue.** Every terrace in
  brick under a tile roof turned the whole tile into one pink mass. `terrace()`
  takes a `wall` and `roof` material for exactly this reason; alternating them is
  what makes the rows read as separate buildings.
- **The tower has to stand clear of the hall, not in front of it.** Centred, it
  merged into the wall behind it at the board camera's 41-degree pitch. It sits
  at the hall's front-left corner now, which also breaks the mirror symmetry that
  a centred tower forced on the whole tile.

Slot order matches `Palette` in `Assets/Editor/CityModelBuilder.cs`:

```
0 City_Brick  1 City_Stone  2 City_Slate   3 City_Tile
4 City_Iron   5 City_Glass  6 City_Cobble  7 City_Grass
```

`CityLevel1.blend` and `CityLevel1.fbx` are the superseded hand-modelled town.
Nothing references them any more; they are kept only as the record of what the
city looked like before.

## CoalPlant.blend / build_coalplant.py

The coal thermal plant: a brick boiler house ribbed with pilasters, four pale
chimneys on corner piers, a lower glazed turbine hall, and a coal store feeding
it by conveyor. Battersea's massing, at tile scale. 1094 triangles, eight
material slots, authored ~0.98 units wide.

Stone, iron and coal reuse the mine's exact values, so the pit that digs the fuel
and the station that burns it read as the same world.

Slot order matches `Palette` in `Assets/Editor/CoalPlantModelBuilder.cs`:

```
0 Plant_Brick  1 Plant_Stone  2 Plant_Roof   3 Plant_Iron
4 Plant_Glass  5 Plant_Coal   6 Plant_Concrete  7 Plant_Grass
```

## CoalMine.blend / build_coalmine.py

The coal extraction tile: a timber headframe straddling an open pit, a stone
winding house with its drum, a spoil heap and an ore cart on a rail spur.
890 triangles, seven material slots, authored ~0.96 units wide.

`CoalMine.blend` is an **output**, not a source. The source is
`build_coalmine.py`, which assembles the whole thing from primitives. To change
the model, edit the script and re-run it in Blender:

```python
exec(open(r"...\ArtSource\build_coalmine.py").read())
```

Material slot order must match `Palette` in
`Assets/Editor/CoalMineModelBuilder.cs`:

```
0 Mine_Timber   1 Mine_Stone   2 Mine_Slate   3 Mine_Iron
4 Mine_Coal     5 Mine_Rock    6 Mine_Grass
```

The script sorts its faces by material index before building the mesh. That is
load-bearing, not tidiness: the FBX round-trip orders submeshes by first use in
the face list, so an unsorted mesh reaches Unity with its palette permuted.

Export with the same settings as the city (below), then run
**Solarpunk → Build Coal Mine Model Prefab**.

The script authors the pithead facing Blender's +Y. The board camera is fixed at
yaw 0 and the FBX axis conversion lands that facing away from it, so the prefab
root carries a `DisplayYaw` that turns the model to its intended three-quarter
view — house back-left, cart front-left, spoil heap front. Orientation lives on
the prefab alongside `DisplayScale`, not in the mesh, so the coordinates in this
script keep reading literally. Changing the board camera's yaw means revisiting
that constant.

## CityLevel1.blend

The level 1 city: a tight early-industrial town block, gable-fronted terrace
houses around a hipped civic hall. 332 triangles, six material slots, authored
about 1 unit wide so it drops straight onto a hex of circumradius 1.

Material slot order matters. It must match `Palette` in
`Assets/Editor/CityModelBuilder.cs`, which assigns the Unity materials by index:

```
0 City_Wall_Cream   1 City_Wall_Brick   2 City_Wall_Plaster
3 City_Roof_Slate   4 City_Roof_Tile    5 City_Trim
```

### Re-exporting after an edit

Select the `CityLevel1` object and export to
`Assets/_Game/Models/CityLevel1.fbx` with these settings. The axis flags plus
`bake_space_transform` are what make Blender's Z-up land as Unity's Y-up; without
them the town imports lying on its side.

```python
bpy.ops.export_scene.fbx(
    filepath=r"...\Assets\_Game\Models\CityLevel1.fbx",
    use_selection=True,
    object_types={'MESH'},
    apply_scale_options='FBX_SCALE_ALL',
    bake_space_transform=True,
    axis_forward='-Z',
    axis_up='Y',
    mesh_smooth_type='FACE',   # preserves the flat-shaded low-poly look
    add_leaf_bones=False,
)
```

Then run **Solarpunk → Build City Model Prefab** in Unity, or just
**Solarpunk → Build Initial Scene**, which rebuilds the prefab as part of its
pass. Display scale lives on the prefab root in `CityModelBuilder`, so the model
itself stays authored at true size.
