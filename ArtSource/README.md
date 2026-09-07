# Art source

Editable sources for generated models. Kept outside `Assets/` on purpose: Unity
imports `.blend` files natively, which would make every asset import depend on a
working Blender install.

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
