"""
!!  SUPERSEDED -- DO NOT RUN WITHOUT ASKING FIRST  !!

This script generated the city's first draft. Murillo has since hand-edited the
model in Blender, and City.blend is now the source of truth for what ships.
Running this would overwrite both City.blend and City.fbx and discard those
edits.

Known differences between this script and the shipped model:
  - the two side terraces were turned to run the same way as the front pair,
    so all four rows are parallel instead of forming a courtyard
  - wall and roof materials were reshuffled between the rows
  - footprint width went from 1.065 to 1.088

Nothing was added or deleted: the triangle count is unchanged at 616. To fold
the edits back in, measure them off the .blend and edit this file, then verify
the rebuild matches 616 tris and per-material counts of 84/72/36/156/70/90/24/84.

-----------------------------------------------------------------------------

Generates the level 1 city as a single flat-shaded low-poly mesh, authored ~1 hex
unit wide with its origin on the ground plane.

The same world as the pithead and the power station, one social class up: brick
terraces under tile roofs, facing a cobbled street, with a stone civic hall and
its clock tower anchoring the far end. Brick, stone, slate, iron and glass are
the power station's exact values -- the town that works the pit is built of the
same material as the pit.

Replaces the hand-modelled CityLevel1.blend. That file and its FBX are left on
disk untouched; nothing references them any more.

Run inside Blender (__file__ must be set so the script can find lowpoly.py):

    p = r"<repo>\\ArtSource\\build_city.py"
    exec(compile(open(p).read(), p, "exec"), {"__file__": p})

Material slot order is a contract with Assets/Editor/CityModelBuilder.cs, which
assigns the Unity materials by index. Do not reorder MATERIALS.
"""
import os, sys, importlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lowpoly
importlib.reload(lowpoly)          # pick up edits without restarting Blender
from lowpoly import *

BRICK, STONE, SLATE, TILE, IRON, GLASS, COBBLE, GRASS = range(8)

MATERIALS = [
    ("City_Brick",  (0.46, 0.27, 0.22)),
    ("City_Stone",  (0.90, 0.87, 0.79)),
    ("City_Slate",  (0.33, 0.36, 0.42)),
    ("City_Tile",   (0.62, 0.34, 0.27)),
    ("City_Iron",   (0.30, 0.30, 0.33)),
    ("City_Glass",  (0.34, 0.47, 0.60)),
    ("City_Cobble", (0.55, 0.54, 0.52)),
    ("City_Grass",  (0.28, 0.45, 0.22)),
]

begin(MATERIALS)


def terrace(cx, cy, half_x, house_count, wall_h, face=-1, ridge_along_x=True,
            wall=BRICK, roof=TILE):
    """A run of joined houses: one block under one roof, articulated into
    dwellings by doors, windows and chimneys rather than by modelling each house
    separately. That is what a terrace is, and it costs a fraction of the
    triangles of N detached boxes.

    `wall` alternates between brick and plaster across the town. With every row
    in brick the walls sat too close in value to the tile roofs and the whole
    tile read as one pink mass at gameplay distance; pale walls under warm roofs
    are what made the old city legible, and what keeps it distinct from the
    power station's grey.
    """
    half_y = 0.075
    box(cx, cy, 0.0, half_x, half_y, wall_h, wall)

    if ridge_along_x:
        taper(cx, cy, wall_h, (half_x + 0.008, half_y + 0.008),
              (half_x + 0.008, 0.012), 0.072, roof)
    else:
        taper(cx, cy, wall_h, (half_y + 0.008, half_x + 0.008),
              (0.012, half_x + 0.008), 0.072, roof)

    # Fronts. Each dwelling gets a door and a window beside it, then a pair
    # upstairs, which is what makes a row of windows read as houses.
    y_wall = cy + face * half_y
    span = 2 * half_x / house_count
    for i in range(house_count):
        hx0 = cx - half_x + span * (i + 0.5)
        wall_panel(hx0 - span * 0.22, y_wall, 0.052, 0.020, 0.052, IRON, face)
        wall_panel(hx0 + span * 0.20, y_wall, 0.062, 0.026, 0.030, GLASS, face)
        wall_panel(hx0 - span * 0.20, y_wall, 0.132, 0.024, 0.026, GLASS, face)
        wall_panel(hx0 + span * 0.20, y_wall, 0.132, 0.024, 0.026, GLASS, face)

    # Party-wall chimneys, on the joins between dwellings.
    for i in range(1, house_count):
        stack_x = cx - half_x + span * i
        box(stack_x, cy, wall_h + 0.030, 0.018, 0.020, 0.088, BRICK)
        for dx in (-0.008, 0.008):
            box(stack_x + dx, cy, wall_h + 0.118, 0.006, 0.007, 0.016, TILE)


# ------------------------------------------------------------- civic hall
# Stone, hipped slate roof, clock tower. The one public building, and the only
# thing tall enough to be the tile's silhouette.
HX, HY = 0.02, 0.29
box(HX, HY, 0.0, 0.155, 0.115, 0.235, STONE)
taper(HX, HY, 0.235, (0.172, 0.132), (0.046, 0.018), 0.086, SLATE)

for dx in (-0.100, -0.033, 0.033, 0.100):                 # hall glazing
    wall_panel(HX + dx, HY - 0.115, 0.120, 0.024, 0.058, GLASS)
box(HX, HY - 0.115, 0.0, 0.040, 0.006, 0.030, STONE)      # entrance steps

# The tower is the tile's silhouette, so it has to clear the hall roof outright
# rather than peer over it -- taller and narrower than looks right in isolation.
# It sits at the hall's front-left corner, not centred: dead ahead of the hall it
# merged into the wall behind it at the board camera's pitch, and centring it also
# made the whole tile mirror-symmetric, which reads as built rather than grown.
TX, TY = -0.125, 0.165
box(TX, TY, 0.0, 0.044, 0.044, 0.520, STONE)
box(TX, TY, 0.520, 0.053, 0.053, 0.022, STONE)            # cornice
taper(TX, TY, 0.542, (0.053, 0.053), (0.006, 0.006), 0.115, SLATE)   # spire
for face, wall in ((-1, TY - 0.044), (1, TY + 0.044)):
    wall_panel(TX, wall, 0.462, 0.023, 0.023, GLASS, face)   # clock face
    wall_panel(TX, wall, 0.395, 0.016, 0.028, IRON, face)    # belfry louvre

# ------------------------------------------------------------- the terraces
# Two rows facing each other across the street, and one closing each side.
terrace(-0.255, -0.150, 0.195, 3, 0.185, face=-1, wall=STONE)
terrace(0.285, -0.150, 0.150, 2, 0.185, face=-1, wall=BRICK)
terrace(-0.395, 0.150, 0.140, 2, 0.170, face=-1, ridge_along_x=False, wall=BRICK)
terrace(0.400, 0.170, 0.130, 2, 0.170, face=-1, ridge_along_x=False,
        wall=STONE, roof=SLATE)

# ------------------------------------------------------------ street + yard
box(0.06, -0.020, 0.0, 0.330, 0.085, 0.008, COBBLE)       # street
box(0.02, 0.130, 0.0, 0.150, 0.075, 0.008, COBBLE)        # square before the hall

for lx, ly in ((-0.205, -0.045), (0.215, -0.045)):        # lamp posts
    box(lx, ly, 0.0, 0.010, 0.010, 0.120, IRON)
    box(lx, ly, 0.120, 0.017, 0.017, 0.026, GLASS)
    box(lx, ly, 0.146, 0.020, 0.020, 0.008, IRON)

for cx, cy, hgt, rot in [
    (-0.455, -0.395, 0.026, 0.3), (-0.150, -0.430, 0.021, -0.8),
    (0.330, -0.400, 0.024, -0.5), (0.455, -0.170, 0.022, 1.1),
    (0.430, 0.330, 0.023, -0.2), (-0.180, 0.440, 0.020, 0.7),
    (0.300, 0.455, 0.019, 1.4),
]:
    tuft(cx, cy, hgt, rot, GRASS)

# ------------------------------------------------------------------ assemble
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
obj = finish("City")
export(obj,
       os.path.join(REPO, "ArtSource", "City.blend"),
       os.path.join(REPO, "Assets", "_Game", "Models", "City.fbx"))
